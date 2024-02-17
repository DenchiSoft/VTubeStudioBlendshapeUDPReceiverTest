using System.Net;
using TMPro;
using UnityEngine;

public class VTubeStudioReceiveDataExample : MonoBehaviour
{
    // Basic setup data.
    public string AppName;
    public string IPhoneIP;
    public int PortToReceiveDataOn;

    // UI elements.
    public TextMeshProUGUI StartStopButtonText;
    public TextMeshProUGUI InfoText;
    public TextMeshProUGUI HotkeyText;
    public TextMeshProUGUI DataText;

    // Used to visualize face rotation.
    public GameObject FaceRotationCube;
    public GameObject MouthCube;

    // VTS data receiver.
    public VTubeStudioBlendshapeDataReceiver receiver;

    // Current state of the receiver.
    private bool on;
    private int hotkey = -1;

    // Current tracking data.
    public VTubeStudioRawTrackingData currentTrackingData;

    void Start()
    {
        Application.targetFrameRate = 60;
        InvokeRepeating(nameof(updateStatus), 0.1f, 0.1f);
    }

    void Update()
    {
        // Alternative to using the event: Poll for newest data on every Update.
        //currentTrackingData = receiver.GetNewestTrackingData();

        // Show current tracking data.
        if (on && currentTrackingData != null)
        {
            string text = $"Time: \n  <b>{currentTrackingData.Timestamp}</b>\n\n";

            if (currentTrackingData.FaceFound)
            {
                text += $"Position:\n  X: <b>{currentTrackingData.Position.x}</b>\n  Y: <b>{currentTrackingData.Position.y}</b>\n  Z: <b>{currentTrackingData.Position.z}</b>\n\n";
                text += $"Rotation:\n  X: <b>{currentTrackingData.Rotation.x}</b>\n  Y: <b>{currentTrackingData.Rotation.y}</b>\n  Z: <b>{currentTrackingData.Rotation.z}</b>\n\n";

                // A few blendshapes.
                var blendshapes = currentTrackingData.BlendShapeDictionary;
                text += "Some blendshapes:\n";
                text += $"  {VTSARKitBlendshape.MouthSmileLeft}:\t\t<b>{blendshapes[VTSARKitBlendshape.MouthSmileLeft]:0.####}</b>\n";
                text += $"  {VTSARKitBlendshape.EyeBlinkLeft}:\t\t<b>{blendshapes[VTSARKitBlendshape.EyeBlinkLeft]:0.####}</b>\n";
                text += $"  {VTSARKitBlendshape.EyeBlinkRight}:\t\t<b>{blendshapes[VTSARKitBlendshape.EyeBlinkRight]:0.####}</b>\n";
                text += $"  {VTSARKitBlendshape.TongueOut}:\t\t<b>{blendshapes[VTSARKitBlendshape.TongueOut]:0.####}</b>\n";
                text += $"  {VTSARKitBlendshape.MouthRight}:\t\t<b>{blendshapes[VTSARKitBlendshape.MouthRight]:0.####}</b>\n";
                text += $"  {VTSARKitBlendshape.EyeLookOutRight}:\t<b>{blendshapes[VTSARKitBlendshape.EyeLookOutRight]:0.####}</b>\n";
                text += $"  {VTSARKitBlendshape.JawOpen}:\t\t\t<b>{blendshapes[VTSARKitBlendshape.JawOpen]:0.####}</b>\n";

                var cubeTransform = FaceRotationCube.transform;
                cubeTransform.localRotation = Quaternion.Euler(new Vector3(-currentTrackingData.Rotation.y, -currentTrackingData.Rotation.x, currentTrackingData.Rotation.z));
                cubeTransform.localPosition = new Vector3(0.5f, 0.5f, -5f) +
                    new Vector3(-currentTrackingData.Position.x, currentTrackingData.Position.y, currentTrackingData.Position.z) * 0.2f;
                MouthCube.transform.localScale = new Vector3(0.5f, 0.05f + blendshapes[VTSARKitBlendshape.JawOpen] * 0.3f, 1f);
            }
            else
            {
                text += "Face not found.";
            }

            DataText.text = text;
        }
    }

    /// <summary>
    /// Updates the UI status periodically.
    /// </summary>
    private void updateStatus()
    {
        if (on)
        {
            InfoText.text = $"Listening on port <b>{PortToReceiveDataOn}</b> for data from <b>{IPhoneIP}</b>\n" +
                $"Receiving packets per second: <b>{receiver.GetCurrentPacketsPerSecond()}</b>\n" +
                $"Total received tracking packets: <b>{receiver.GetTotalPacketsReceivedSinceStarted()}</b>";
            HotkeyText.text = $"Hotkey:  <b>{(hotkey == -1 ? "-" : hotkey.ToString())}</b>";
        }
        else
        {
            InfoText.text = "Receiver off.";
        }
    }

    /// <summary>
    /// Called when start/stop button was pressed by user.
    /// </summary>
    public void StartStopButtonPressed()
    {
        if (on)
        {
            if (receiver.StopReceivingData())
            {
                on = false;
            }
        }
        else
        {
            // Try to parse IP.
            IPAddress parsedIPhoneIP;
            try
            {
                parsedIPhoneIP = IPAddress.Parse(IPhoneIP); ;
            }
            catch
            {
                parsedIPhoneIP = null;
            }
            
            // Start server.
            if (parsedIPhoneIP != null)
            {
                if (receiver.StartReceivingData(AppName, parsedIPhoneIP, PortToReceiveDataOn))
                {
                    on = true;
                }
            }
            else
            {
                Debug.LogError($"Invalid IP provided: {IPhoneIP}");
            }
        }

        DataText.text = "No data.";
        StartStopButtonText.text = on ? "Stop" : "Start";
    }

    /// <summary>
    /// Called whenever new tracking data is received.
    /// </summary>
    public void TrackingDataReceivedFromVTS(VTubeStudioRawTrackingData data)
    {
        currentTrackingData = data;
    }

    /// <summary>
    /// Called whenever a on-screen hotkey is pressed in the iOS app.
    /// </summary>
    public void HotkeyPressed(int hotkeyID)
    {
        CancelInvoke(nameof(resetHotkeyDisplay));
        hotkey = hotkeyID;
        Invoke(nameof(resetHotkeyDisplay), 1);
    }

    /// <summary>
    /// Resets displayed hotkey.
    /// </summary>
    private void resetHotkeyDisplay()
    {
        hotkey = -1;
    }
}
