using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Fyrvall.BonaRoadTool
{
    [RequireComponent(typeof(MeshFilter))]
    [RequireComponent(typeof(MeshRenderer))]
    public class RoadSpline : MonoBehaviour
    {
        public const float DistanceStep = 3f;
        private class MeshData
        {
            public List<Vector3> Vertices = new List<Vector3>();
            public List<Vector3> Normals = new List<Vector3>();
            public List<Vector2> Uvs = new List<Vector2>();
            public List<int> Triangles = new List<int>();

            public int CurrentIndex = 0;
            public float CurrentUvOffset = 0;
        }

        [Tooltip("Width (In units) each segment will be")]
        public float Width = 1f;
        [Tooltip("How the UV scale matches the length of the road")]
        public float UvScaling = 1f;
        [Tooltip("How many edges should exists per unit in unity")]
        public float Smoothness = 1f;

        public Material Material;

        [HideInInspector]
        public bool IsEditable = false;

        [HideInInspector]
        public List<SplineControlPoint> ControlPoints = new List<SplineControlPoint>();

        public void Reset()
        {
            ControlPoints = new List<SplineControlPoint> {
                new SplineControlPoint {
                    Position = new Vector3(0f, 0f, 0f), Direction = Quaternion.LookRotation(Vector3.right)
                }
            };
        }

        public void AddControlPoint(Vector3 position)
        {
            var lastControlPoint = ControlPoints.Last();
            var directionOffset = position - (transform.position + lastControlPoint.Position);
            var direction = Quaternion.LookRotation(directionOffset);

            var targetPosition = position - transform.position;
            var controlPoint = new SplineControlPoint { Position = targetPosition, Direction = direction, SegmentsToNext = 0 };
            ControlPoints.Add(controlPoint);
            lastControlPoint.NextIndex = ControlPoints.IndexOf(controlPoint);
            controlPoint.PreviousIndex = ControlPoints.IndexOf(lastControlPoint);
            UpdateControlPoint(controlPoint);
            UpdateMesh();
        }


        public void InsertControlPoint(System.Tuple<SplineControlPoint, SplineControlPoint> controlPoints, Vector3 position)
        {

            var targetPosition = position - transform.position;
            var direction = Quaternion.Slerp(controlPoints.Item1.Direction, controlPoints.Item2.Direction, 0.5f);

            var controlPoint = new SplineControlPoint { Position = targetPosition, Direction = direction };

            var insertIndex = Mathf.Max(ControlPoints.IndexOf(controlPoints.Item1), ControlPoints.IndexOf(controlPoints.Item2));
            ControlPoints.Insert(insertIndex, controlPoint);
            UpdateMesh();
        }

        public void RemoveControlPoint(SplineControlPoint controlPoint)
        {
            ControlPoints.Remove(controlPoint);
            UpdateMesh();
        }

        public void UpdateMesh()
        {
            var meshFilter = GetComponent<MeshFilter>();
            var meshRender = GetComponent<MeshRenderer>();

            var roundedControlPoints = GenerateControlPoints(ControlPoints, Smoothness);

            var meshData = GenerateMeshData(roundedControlPoints);
            var mesh = new Mesh {
                vertices = meshData.Vertices.ToArray(),
                normals = meshData.Normals.ToArray(),
                uv = meshData.Uvs.ToArray(),
                triangles = meshData.Triangles.ToArray()
            };

            meshFilter.mesh = mesh;
            meshRender.material = Material;
        }

        public void UpdateControlPoint(SplineControlPoint controlPoint)
        {
            if (controlPoint.PreviousIndex != -1) {
                var previousPoint = ControlPoints[controlPoint.PreviousIndex];

                var halfDistance = (previousPoint.Position - controlPoint.Position).magnitude / DistanceStep;
                previousPoint.ForwardTangent = previousPoint.Position + previousPoint.Direction * Vector3.forward * halfDistance;
                controlPoint.BackwardsTangent = controlPoint.Position + controlPoint.Direction * Vector3.back * halfDistance;

                var halfEstimatedCurveDistance = BezierCurve.BezierCurveLength(previousPoint.Position, previousPoint.ForwardTangent, controlPoint.BackwardsTangent, controlPoint.Position, 100) / 2;
                var controlPointDirection = Quaternion.LookRotation(controlPoint.Position - previousPoint.Position).normalized;

                var previousRotationFactor = (Quaternion.Dot(previousPoint.Direction, controlPointDirection) + 1f) / 2;
                previousPoint.SegmentsToNext = Mathf.Max(1, Mathf.FloorToInt((halfEstimatedCurveDistance / Smoothness) * previousRotationFactor));

                var rotationFactor = (Quaternion.Dot(controlPoint.Direction, controlPointDirection) + 1f) / 2;
                controlPoint.SegmentsToPrevious = Mathf.Max(1, Mathf.FloorToInt((halfEstimatedCurveDistance / Smoothness) * rotationFactor));
            }

            if (controlPoint.NextIndex != -1) {
                var nextPoint = ControlPoints[controlPoint.NextIndex];

                var halfDistance = (controlPoint.Position - nextPoint.Position).magnitude / DistanceStep;
                controlPoint.ForwardTangent = controlPoint.Position + controlPoint.Direction * Vector3.forward * halfDistance;
                nextPoint.BackwardsTangent = nextPoint.Position + nextPoint.Direction * Vector3.back * halfDistance;

                var halfEstimatedCurveDistance = BezierCurve.BezierCurveLength(controlPoint.Position, controlPoint.ForwardTangent, nextPoint.BackwardsTangent, nextPoint.Position, 100) / 2;
                var controlPointDirection = Quaternion.LookRotation(controlPoint.Position - nextPoint.Position).normalized;

                var nextRotationFactor = (Quaternion.Dot(nextPoint.Direction, controlPointDirection) + 1f) / 2;
                nextPoint.SegmentsToNext = Mathf.Max(1, Mathf.FloorToInt((halfEstimatedCurveDistance * Smoothness) * nextRotationFactor));

                var rotationFactor = (Quaternion.Dot(controlPointDirection, controlPoint.Direction) + 1f) / 2;
                controlPoint.SegmentsToPrevious = Mathf.Max(1, Mathf.FloorToInt((halfEstimatedCurveDistance * Smoothness) * rotationFactor));
            }
        }

        private List<SplineControlPoint> GenerateControlPoints(List<SplineControlPoint> controlPoints, float smoothness)
        {
            // Just add the first point. It will still be first
            var result = new List<SplineControlPoint> {
                controlPoints[0]
            };

            // Can't have a mesh with a single point
            if (controlPoints.Count < 2) {
                return result;
            }

            foreach (var controlPoint in controlPoints) {

                if(controlPoint.NextIndex == -1) {
                    continue;
                }

                var nextPoint = ControlPoints[controlPoint.NextIndex];

                var firstSteps = controlPoint.SegmentsToNext + nextPoint.SegmentsToPrevious;
                var firstStepDistance = 1f / firstSteps;

                // Insert N extra control points along the curve for the first half.
                for (int i = 0; i <= firstSteps; i++) {
                    var distanceFactor = i * firstStepDistance;
                    var position = BezierCurve.CubicCurve(controlPoint.Position, controlPoint.ForwardTangent, nextPoint.BackwardsTangent, nextPoint.Position, distanceFactor);
                    var tangent = BezierCurve.CubicCurveDerivative(controlPoint.Position, controlPoint.ForwardTangent, nextPoint.BackwardsTangent, nextPoint.Position, distanceFactor).normalized;

                    var firstRotation = Quaternion.Euler(0, 0, controlPoint.Direction.eulerAngles.z);
                    var secondRotation = Quaternion.Euler(0, 0, nextPoint.Direction.eulerAngles.z);
                    var rotation = Quaternion.Lerp(firstRotation, secondRotation, firstSteps * firstStepDistance);

                    result.Add(new SplineControlPoint { Position = position, Direction = Quaternion.LookRotation(tangent) * rotation });
                }
            }

            return result;
        }

        private MeshData GenerateMeshData(List<SplineControlPoint> controlPoints)
        {
            var result = new MeshData();
            AddControlPointToMesh(controlPoints[0], null, result);
            for (int i = 1; i < controlPoints.Count; i++) {
                AddControlPointToMesh(controlPoints[i], controlPoints[i - 1], result);
            }

            return result;
        }

        private float GetUvOffset(SplineControlPoint controlPoint, SplineControlPoint lastControlPoint)
        {
            if (lastControlPoint == null) {
                return 0;
            }

            return (lastControlPoint.Position - controlPoint.Position).magnitude;
        }

        private Vector3 GetVectorPosition(Vector3 position, Quaternion rotation, Vector3 direction, float width)
        {
            return position + rotation * direction * width;
        }

        private void AddControlPointToMesh(SplineControlPoint controlPoint, SplineControlPoint lastControlPoint, MeshData meshData)
        {
            meshData.CurrentUvOffset += GetUvOffset(controlPoint, lastControlPoint);

            meshData.Vertices.Add(GetVectorPosition(controlPoint.Position, controlPoint.Direction, Vector3.left, Width));
            meshData.Vertices.Add(GetVectorPosition(controlPoint.Position, controlPoint.Direction, Vector3.right, Width));

            // All river segments points up regardless of actual orientation
            meshData.Normals.Add(Vector3.up);
            meshData.Normals.Add(Vector3.up);

            // Make the X axis a continous point along the edge and the y axis continous along the middle. Makes it looks smoother
            meshData.Uvs.Add(new Vector2(0, meshData.CurrentUvOffset * UvScaling));
            meshData.Uvs.Add(new Vector2(1, meshData.CurrentUvOffset * UvScaling));

            if (lastControlPoint != null) {
                meshData.Triangles.AddRange(new int[] { meshData.CurrentIndex - 2, meshData.CurrentIndex, meshData.CurrentIndex - 1, meshData.CurrentIndex, meshData.CurrentIndex + 1, meshData.CurrentIndex - 1 });
            }

            meshData.CurrentIndex += 2;
        }
    }
}