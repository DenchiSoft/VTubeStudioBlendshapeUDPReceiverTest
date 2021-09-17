using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Represents the raw blendshape tracking data to be sent to VSeeFace via UDP.
/// </summary>
public class VTubeStudioRawTrackingData
{
    [Serializable]
    public class VTSTrackingDataEntry
    {
        public string k;
        public float v;

        public VTSTrackingDataEntry(string key, float value)
        {
            k = key;
            v = value;
        }
    }

    /// <summary>
    /// Current UNIX millisecond timestamp.
    /// </summary>
    public long Timestamp = 0;

    /// <summary>
    /// Last pressed on-screen hotkey.
    /// </summary>
    public int Hotkey = -1;

    /// <summary>
    /// Whether or not face has been found
    /// </summary>
    public bool FaceFound = false;

    /// <summary>
    /// Current face rotation.
    /// </summary>
    public Vector3 Rotation;

    /// <summary>
    /// Current face position.
    /// </summary>
    public Vector3 Position;

    /// <summary>
    /// Current iOS blendshapes.
    /// </summary>
    public List<VTSTrackingDataEntry> BlendShapes = new List<VTSTrackingDataEntry>();

    /// <summary>
    /// Current iOS blendshapes in dictionary for easy access.
    /// </summary>
    public Dictionary<VTSARKitBlendshape, float> BlendShapeDictionary = new Dictionary<VTSARKitBlendshape, float>();
}