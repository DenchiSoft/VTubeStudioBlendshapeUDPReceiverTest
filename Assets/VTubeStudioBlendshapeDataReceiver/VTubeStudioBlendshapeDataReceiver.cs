using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// This class handles requesting data from the VTube Studio iPhone app.
/// Sender/Receiver each have their own threads. Data exchange between threads is implemented using a ConcurrentQueue.
/// </summary>
public class VTubeStudioBlendshapeDataReceiver : MonoBehaviour
{
    #region Events

    /// <summary>
    /// Event definition for event that gets triggered when tracking data is received.
    /// </summary>
    [Serializable]
    public class TrackingDataReceivedEvent : UnityEvent<VTubeStudioRawTrackingData> { }

    /// <summary>
    /// Event definition for event that gets triggered when a on-screen hotkey is pressed.
    /// </summary>
    [Serializable]
    public class HotkeyReceivedEvent : UnityEvent<int> { }

    /// <summary>
    /// Event that gets triggered when tracking data is received.
    /// </summary>
    public TrackingDataReceivedEvent TrackingDataReceived;

    /// <summary>
    /// Event that gets triggered when a on-screen hotkey is pressed.
    /// </summary>
    public HotkeyReceivedEvent HotkeyReceived;

    #endregion

    #region Constants

    /// <summary>
    /// Debug log tag.
    /// </summary>
    private const string TAG = "[VTS Blendshapes]";

    /// <summary>
    /// If true, the received data will be printed.
    /// </summary>
    private const bool printDebugBlendshapes = false;

    /// <summary>
    /// The listen port for the UDP server.
    /// </summary>
    private const int vTubeStudioListenPortUDP = 21412;

    /// <summary>
    /// IP to start the local UDP listener on.
    /// </summary>
    private const string localUDPListenerIP = "0.0.0.0";

    /// <summary>
    /// Number of keepalive packets (data requests) to send per second.
    /// </summary>
    private const int keepalivesPerSecond = 5;

    /// <summary>
    /// Number of seconds to request data for when requesting data.
    /// </summary>
    private const int requestDataForSeconds = 1;

    #endregion

    #region Public members

    #endregion

    #region Private members

    /// <summary>
    /// Message queue for receiver thread.
    /// </summary>
    private ConcurrentQueue<VTubeStudioRawTrackingData> receiverQueue = new ConcurrentQueue<VTubeStudioRawTrackingData>();

    /// <summary>
    /// Whether or not the threads should stop.
    /// </summary>
    private volatile bool shutdownThread;

    /// <summary>
    /// The UDP blendshape sender thread.
    /// </summary>
    private Thread udpSenderThread;

    /// <summary>
    /// The UDP command listener thread.
    /// </summary>
    private Thread udpListenerThread;

    /// <summary>
    /// Listener buffer.
    /// </summary>
    private byte[] listenBuffer;

    /// <summary>
    /// The listener socket.
    /// </summary>
    private Socket listenSocket;

    /// <summary>
    /// UDP client for sending UDP data.
    /// </summary>
    private UdpClient udpSenderClient;

    /// <summary>
    /// Whether or not the UDP blendshape server has been initialized.
    /// </summary>
    private bool initialized;

    /// <summary>
    /// The current packets per second.
    /// </summary>
    private float currentPPS;

    /// <summary>
    /// The number of data packets the UDP server has received within the last second. Used to calculate FPS.
    /// </summary>
    private int dataPacketsReceivedInLastSecond;

    /// <summary>
    /// The total number of data packets received since the server was started.
    /// </summary>
    private int dataPacketsReceivedTotal;

    /// <summary>
    /// The most recent received tracking data.
    /// </summary>
    private VTubeStudioRawTrackingData newestTrackingData;

    #endregion

    #region Unity

    /// <summary>
    /// Called by Unity.
    /// </summary>
    void Update()
    {
        // Process received data from receiver thread.
        while (!receiverQueue.IsEmpty)
        {
            VTubeStudioRawTrackingData receivedTrackingData;

            if (receiverQueue.TryDequeue(out receivedTrackingData) && receivedTrackingData != null)
            {
                dataPacketsReceivedTotal++;
                dataPacketsReceivedInLastSecond++;

                if (printDebugBlendshapes)
                {
                    Debug.Log("a");
                }

                newestTrackingData = receivedTrackingData;

                // Notify other components that tracking data was received.
                TrackingDataReceived?.Invoke(receivedTrackingData);

                if (receivedTrackingData.Hotkey != -1)
                {
                    HotkeyReceived?.Invoke(receivedTrackingData.Hotkey);
                }
            }
        }
    }

    /// <summary>
    /// Called by Unity.
    /// </summary>
    void OnApplicationQuit()
    {
        EndSenderAndReceiver();
    }

    /// <summary>
    /// Called by Unity.
    /// </summary>
    void OnDestroy()
    {
        EndSenderAndReceiver();
    }

    #endregion

    #region Public methods

    /// <summary>
    /// Start receiving UDP blendshape data from VTube Studio.
    /// </summary>
    /// <param name="appName">The name of the requesting app. Will be used for logging.</param>
    /// <param name="vTubeStudioIPhoneIP">The IP of the iPhone or iPad to request data from.</param>
    /// <param name="requestToReceiveDataOnPort">The port to receive data on. Use any port that's free on this device.</param>
    /// <returns>True if data receiver was started, false otherwise.</returns>
    public bool StartReceivingData(string appName, IPAddress vTubeStudioIPhoneIP, int requestToReceiveDataOnPort)
    {
        if (initialized)
        {
            Log($"Already started. Stop first before starting again with different parameters.");
            return false;
        }
        else if (appName == null || appName.Trim().Equals(string.Empty) || appName.Length > 32)
        {
            Log($"App name cannot be empty or longer than 32 characters.");
            return false;
        }
        else if (vTubeStudioIPhoneIP == null || requestToReceiveDataOnPort < 1 || requestToReceiveDataOnPort > 65535)
        {
            Log($"Invalid IP for iOS device or invalid receiver port.");
            return false;
        }

        // Try to parse local UDP listener IP.
        IPAddress ip;
        if (!IPAddress.TryParse(localUDPListenerIP, out ip))
        {
            Log($"Invalid IP for local UDP listener.");
            return false;
        }

        // Try to initialize socket.
        udpSenderClient = new UdpClient();
        listenSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        listenSocket.Bind(new IPEndPoint(ip, requestToReceiveDataOnPort));
        listenSocket.ReceiveTimeout = 15;
        listenBuffer = new byte[65535];

        // Reset PPS counter.
        dataPacketsReceivedTotal = 0;
        dataPacketsReceivedInLastSecond = 0;
        currentPPS = 0;

        // If listener socket is set up, start listener/sender threads.
        if (listenSocket.IsBound)
        {
            shutdownThread = false;

            Log($"Starting VTube Studio UDP blendshape request thread. Will attempt to request data from VTube Studio " +
                $"running on {vTubeStudioIPhoneIP}:{vTubeStudioListenPortUDP}");
            udpSenderThread = new Thread(() => sendUDPThread(appName, vTubeStudioIPhoneIP, requestToReceiveDataOnPort));
            udpSenderThread.Start();

            Log($"Starting VTube Studio UDP blendshape receiver thread. Will listen for data on port {requestToReceiveDataOnPort}.");
            udpListenerThread = new Thread(() => listenUDPThread());
            udpListenerThread.Start();
        }
        else
        {
            Log($"Failed to initialize.");
            return false;
        }

        // Restart packets-per-second calculator.
        CancelInvoke(nameof(calculatePPS));
        InvokeRepeating(nameof(calculatePPS), 1, 1);

        // Initialization done.
        initialized = true;
        return true;
    }

    /// <summary>
    /// Stop receiving data from VTube Studio.
    /// </summary>
    /// <returns>True if data receiver was stopped, false otherwise.</returns>
    public bool StopReceivingData()
    {
        if (!initialized)
        {
            Log($"Already stopped. Start first.");
            return false;
        }

        Log($"Stopping sender/receiver threads.");
        EndSenderAndReceiver();

        dataPacketsReceivedTotal = 0;
        dataPacketsReceivedInLastSecond = 0;
        currentPPS = 0;
        initialized = false;

        return true;
    }

    /// <summary>
    /// Returns number of tracking packets received within the last second.
    /// Returns 0 if receiver is off.
    /// </summary>
    public float GetCurrentPacketsPerSecond()
    {
        return currentPPS;
    }

    /// <summary>
    /// Returns total number of tracking packets received since receiver thread was last started.
    /// Returns 0 if receiver is off.
    /// </summary>
    public float GetTotalPacketsReceivedSinceStarted()
    {
        return dataPacketsReceivedTotal;
    }

    /// <summary>
    /// Returns the most recent tracking data.
    /// If none has been received so far, this returns null.
    /// </summary>
    public VTubeStudioRawTrackingData GetNewestTrackingData()
    {
        return newestTrackingData;
    }

    #endregion

    #region Private methods

    /// <summary>
    /// UDP listener thread.
    /// Listens for data sent by VTube Studio.
    /// </summary>
    private void listenUDPThread()
    {
        // Listen for data on specified port until requested to shut down.
        while (!shutdownThread)
        {
            try
            {
                EndPoint senderRemote = new IPEndPoint(IPAddress.Any, 0);

                try
                {
                    // Receive data.
                    int receivedBytes = listenSocket.ReceiveFrom(listenBuffer, SocketFlags.None, ref senderRemote);
                    string receivedString = Encoding.ASCII.GetString(listenBuffer, 0, receivedBytes);             
                    var trackingData = JsonUtility.FromJson<VTubeStudioRawTrackingData>(receivedString);

                    // Parse blendshapes into dictionary for easy access.
                    foreach (var blendshapeKeyValue in trackingData.BlendShapes)
                    {
                        VTSARKitBlendshape blendshape;
                        if (Enum.TryParse(blendshapeKeyValue.k, out blendshape))
                        {
                            trackingData.BlendShapeDictionary.Add(blendshape, blendshapeKeyValue.v);
                        }
                    }

                    // Put received data into concurrent queue to pass it to main thread.
                    receiverQueue.Enqueue(trackingData);
                }
                catch { }

                Thread.Sleep(16);
            }
            catch
            {
                Thread.Sleep(100);
            }
        }
    }

    /// <summary>
    /// UDP sender thread.
    /// Sends data request packets to keep the data stream alive.
    /// </summary>
    private void sendUDPThread(string appName, IPAddress vTubeStudioIPhoneIP, int requestToReceiveDataOnPort)
    {
        // Prepare data request packet.
        VTubeStudioUDPDataRequest dataRequest = new VTubeStudioUDPDataRequest(appName, requestDataForSeconds, requestToReceiveDataOnPort);
        string serializedDataRequest = JsonUtility.ToJson(dataRequest);
        byte[] serializedDataRequestBytes = Encoding.ASCII.GetBytes(serializedDataRequest);

        // Calculate time between data requests.
        int senderThreadSleepTimeMillis = Mathf.Clamp(1000 / keepalivesPerSecond, 10, 5000);

        // Keep sending data request packets until requested to shut down.
        IPEndPoint vtsIPhoneEndpoint = new IPEndPoint(vTubeStudioIPhoneIP, vTubeStudioListenPortUDP);
        while (!shutdownThread)
        {
            try
            {
                udpSenderClient.Send(serializedDataRequestBytes, serializedDataRequestBytes.Length, vtsIPhoneEndpoint);
            }
            catch { }

            Thread.Sleep(senderThreadSleepTimeMillis);
        }
    }

    /// <summary>
    /// Gets executed every second to calculate packets per second.
    /// </summary>
    private void calculatePPS()
    {
        currentPPS = dataPacketsReceivedInLastSecond;
        dataPacketsReceivedInLastSecond = 0;
    }

    /// <summary>
    /// Stops the threads.
    /// </summary>
    void EndSenderAndReceiver()
    {
        shutdownThread = true;

        if (udpSenderThread != null)
        {
            udpSenderThread.Join();
        }

        if (udpListenerThread != null)
        {
            udpListenerThread.Join();
        }

        if (listenSocket != null)
        {
            listenSocket.Close();
        }
    }

    /// <summary>
    /// Prints the given string to logs with a tag.
    /// </summary>
    private static void Log(string log)
    {
        Debug.Log($"{TAG} {log}");
    }

    #endregion
}