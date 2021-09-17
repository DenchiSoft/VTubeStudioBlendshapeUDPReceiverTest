using System;

/// <summary>
/// UDP data request payload
/// </summary>
[Serializable]
public class VTubeStudioUDPDataRequest
{
    /// <summary>
    /// The request message type.
    /// </summary>
    public string messageType = "iOSTrackingDataRequest";

    /// <summary>
    /// For how many seconds should the data be sent?
    /// </summary>
    public float time;

    /// <summary>
    /// The name of the app that sent this request (for logging).
    /// </summary>
    public string sentBy;

    /// <summary>
    /// The UDP ports to send data to for the given time.
    /// </summary>
    public int[] ports;

    /// <summary>
    /// Constructor.
    /// </summary>
    public VTubeStudioUDPDataRequest(string appName, float sendTime, params int[] receiverPorts)
    {
        time = sendTime;
        sentBy = appName;
        ports = receiverPorts;
    }
}