# Receive tracking data from the VTS iPhone app

You can request to receive tracking data from the VTube Studio iPhone app. This data includes blendshapes, head rotation, head position and more. Data is requested and sent via UDP.

## Prerequisites

You need an iPhone or iPad running VTube Studio. Make sure the `"3rd Party PC Clients"` option is on. It's at the bottom of the first settings tab. This starts a **UDP listener** on the iPhone and the app is now ready to send you data over the local network when you request it.

![Screenshot](/images/ios_screenshot_1.png)

## Requesting tracking data

You can request to get sent tracking data for up to 10 seconds. To do that, send the following string payload to the iPhone (port `21412` or whatever is displayed in the iOS app) via UDP:

```json
  {
    "messageType": "iOSTrackingDataRequest",
    "time": 2.5,
    "sentBy": "MyApp",
    "ports": [11125, 11126]
  }
```

The `time` field tells the iOS app how long to send data. Allowed values are between `0.5 and 10` seconds. Make sure to repeatedly send this request every few second to keep receiving data. When you're done, just stop sending the request.

When the iOS app receives this request, it will send UDP data packets to the IP that sent the request. 

Data will be sent to the ports you listed in the request. You have to list at least one port and can list up to 16. The `sentBy` field should contain your app name and is only used for logging. It has to be between 1 and 32 characters long.

## What data is sent?

You will receive the following data every frame (typically at 60 FPS unless there is lag in the iPhone app):

* Unix millisecond timestamp of tracking data
* Face found? (boolean)
* All 52 iOS blendshape values
* Head position
* Head rotation
* Eye left rotation
* Eye right rotation
* Any on-screen hotkey pressed? (int between 1 and 8)

Details about the exact payload can be found here: [`Payload Definition`](https://github.com/DenchiSoft/VTubeStudioBlendshapeUDPReceiverTest/blob/main/Assets/VTubeStudioBlendshapeDataReceiver/VTubeStudioRawTrackingData.cs)

A detailed explanation of all blendshapes can be found here: https://developer.apple.com/documentation/arkit/arfaceanchor/blendshapelocation


## Example app

list of files in project

where to put IP?

| File | Description |
| --- | --- |
| [`VTSARKitBlendshape.cs`](https://github.com/DenchiSoft/VTubeStudioBlendshapeUDPReceiverTest/blob/main/Assets/VTubeStudioBlendshapeDataReceiver/VTSARKitBlendshape.cs) | List all *new or modified* files |
| [`VTubeStudioBlendshapeDataReceiver.cs`](https://github.com/DenchiSoft/VTubeStudioBlendshapeUDPReceiverTest/blob/main/Assets/VTubeStudioBlendshapeDataReceiver/VTubeStudioBlendshapeDataReceiver.cs) | Show file differences that **haven't been** staged |
| [`VTubeStudioRawTrackingData.cs`](https://github.com/DenchiSoft/VTubeStudioBlendshapeUDPReceiverTest/blob/main/Assets/VTubeStudioBlendshapeDataReceiver/VTubeStudioRawTrackingData.cs) | Show file differences that **haven't been** staged |
| [`VTubeStudioUDPDataRequest.cs`](https://github.com/DenchiSoft/VTubeStudioBlendshapeUDPReceiverTest/blob/main/Assets/VTubeStudioBlendshapeDataReceiver/VTubeStudioUDPDataRequest.cs) | Show file differences that **haven't been** staged |
| [`Example/VTubeStudioReceiveDataExample.cs`](https://github.com/DenchiSoft/VTubeStudioBlendshapeUDPReceiverTest/blob/main/Assets/Example/VTubeStudioReceiveDataExample.cs) | Show file differences that **haven't been** staged |


![Screenshot](/images/unity_screenshot_1.png)
