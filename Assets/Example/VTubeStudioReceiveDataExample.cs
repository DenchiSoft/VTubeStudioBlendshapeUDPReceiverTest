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

    // VTS data receiver.
    public VTubeStudioBlendshapeDataReceiver receiver;
    public VTubeStudioRawTrackingData currentTrackingData;

    // Current state of the receiver.
    private bool on;
    private int hotkey = -1;

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
                text += "A few blendshapes:\n";
                text += $"  {VTSARKitBlendshape.MouthSmileLeft}: <b>{blendshapes[VTSARKitBlendshape.MouthSmileLeft]}</b>\n";
                text += $"  {VTSARKitBlendshape.EyeBlinkLeft}: <b>{blendshapes[VTSARKitBlendshape.EyeBlinkLeft]}</b>\n";
                text += $"  {VTSARKitBlendshape.EyeLookInLeft}: <b>{blendshapes[VTSARKitBlendshape.EyeLookInLeft]}</b>\n";
                text += $"  {VTSARKitBlendshape.TongueOut}: <b>{blendshapes[VTSARKitBlendshape.TongueOut]}</b>\n";
                text += $"  {VTSARKitBlendshape.MouthRight}: <b>{blendshapes[VTSARKitBlendshape.MouthRight]}</b>\n";
                text += $"  {VTSARKitBlendshape.EyeLookOutRight}: <b>{blendshapes[VTSARKitBlendshape.EyeLookOutRight]}</b>\n";
                text += $"  {VTSARKitBlendshape.JawOpen}: <b>{blendshapes[VTSARKitBlendshape.JawOpen]}</b>\n";
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
            if (receiver.StartReceivingData(AppName, IPAddress.Parse(IPhoneIP), PortToReceiveDataOn))
            {
                on = true;
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
