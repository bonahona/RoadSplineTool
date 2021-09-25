using UnityEngine;

namespace Fyrvall.BonaRoadTool
{
    [System.Serializable]
    public class SplineControlPoint
    {
        public Vector3 Position;
        public Vector3 BackwardsTangent;
        public Vector3 ForwardTangent;
        public Quaternion Direction = Quaternion.identity;
        public int SegmentsToPrevious = 0;
        public int SegmentsToNext = 0;

        public int PreviousIndex = -1;
        public int NextIndex = -1;
    }
}