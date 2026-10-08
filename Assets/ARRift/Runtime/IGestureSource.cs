using System;
using UnityEngine;
namespace CampusRift.AR
{
    public struct GestureFrame
    {
        public int handId;public string label,handed;public float score;public float[] landmarks;public long timestampMs;public int width,height,rotation;
        public Matrix4x4 displayMatrix;public bool hasDisplayMatrix,screenCoordinates;
        public float[] worldLandmarks;public string[] categoryLabels;public float[] categoryScores;
        public bool handPresent,fullScores;public int epoch;public long frameId;
        // Pose at acquisition, copied alongside metadata; never persisted.
        public bool hasCameraPose;public Vector3 cameraPosition;public Quaternion cameraRotation;
        public double? nativeToUnityMs,clockErrorMs;
        public double sensorTimestamp,acquireMs,convertReadyMs,submitMs,consumeMs,inferStartMs,resultMs;
    }
    public interface IGestureSource {event Action<GestureFrame> Result;}
}
