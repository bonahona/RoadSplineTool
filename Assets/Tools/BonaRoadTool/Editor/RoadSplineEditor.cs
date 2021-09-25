using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Fyrvall.BonaRoadTool
{
    [CustomEditor(typeof(RoadSpline))]
    public class RoadSplineEditor: Editor
    {
        private readonly RaycastHit[] RaycastHits = new RaycastHit[128];
        private GUIStyle EditorTextStyle;

        private class InScenePosition
        {
            public Vector3 Position;
            public bool IsInWorld;
            public bool IsInCable;

            public static readonly InScenePosition None = new InScenePosition { Position = Vector3.zero, IsInWorld = false };
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            EditorGUILayout.Space();

            var roadSpline = target as RoadSpline;
            roadSpline.IsEditable = GUILayout.Toggle(roadSpline.IsEditable, "Edit path", "Button", GUILayout.Height(24));

            if (EditorGUI.EndChangeCheck()) {
                roadSpline.UpdateMesh();
            }
        }

        public void OnEnable()
        {
            SceneView.duringSceneGui += OnSceneView;
        }

        public void OnDisable()
        {
            SceneView.duringSceneGui -= OnSceneView;

            var roadSpline = target as RoadSpline;
            if (roadSpline == null) {
                return;
            }

            roadSpline.IsEditable = Selection.activeGameObject == roadSpline.gameObject;
        }

        public void OnSceneView(SceneView sceneView)
        {
            if (EditorTextStyle == null) {
                EditorTextStyle = CreateInSceneTextStyle();
            }

            var roadSpline = target as RoadSpline;
            if (!roadSpline.IsEditable) {
                return;
            }

            // Get lone rights to capture the input
            HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));

            ShowControlPoints(roadSpline);
            DrawCurvedLine(roadSpline);
            HandleEvent(Event.current, roadSpline);

            sceneView.Repaint();
        }

        private void HandleEvent(Event currentEvent, RoadSpline spline)
        {
            var inScenePosition = GetInScenePoint(Event.current.mousePosition, spline);

            if (currentEvent.control) {
                AddContolPoint(currentEvent, spline, inScenePosition);
            } else if (currentEvent.shift) {
                RemoveControlPoint(currentEvent, spline, inScenePosition);
            } else {
                Handles.Label(inScenePosition.Position + Vector3.down * 2, "Hold control to place point\nHold shift to remove a point\nPress space to release", EditorTextStyle);
            }

            // Space releases the editing of this cable
            if (currentEvent.type == EventType.KeyDown && currentEvent.keyCode == KeyCode.Space) {
                spline.IsEditable = false;
                currentEvent.Use();
            }
        }

        private void AddContolPoint(Event currentEvent, RoadSpline spline, InScenePosition inScenePosition)
        {
            var lastPoint = spline.ControlPoints.Last();
            var lastPointPosition = spline.transform.position + lastPoint.Position;

            if (inScenePosition.IsInCable) {
                if (currentEvent.type == EventType.MouseDown) {
                    Undo.RecordObject(spline, "Inserted control point");
                    //spline.InsertControlPoint(inScenePosition.ControlPointsPairs, inScenePosition.Position);
                    currentEvent.Use();
                }
            } else {
                Handles.DrawLine(lastPointPosition, inScenePosition.Position);

                if (currentEvent.type == EventType.MouseDown) {
                    Undo.RecordObject(spline, "Added additional control point");
                    spline.AddControlPoint(inScenePosition.Position);
                    currentEvent.Use();
                }
            }
        }

        private InScenePosition GetInScenePoint(Vector2 position, RoadSpline spline)
        {
            if (Physics.Raycast(HandleUtility.GUIPointToWorldRay(position), out RaycastHit raycastHit)) {
                if (raycastHit.collider.gameObject == spline.gameObject) {
                    //return new InScenePosition { Position = raycastHit.point, IsInWorld = true, IsInCable = true, ControlPointsPairs = GetSelectedControlPoint(raycastHit, spline) };
                    return InScenePosition.None;
                } else {
                    return new InScenePosition { Position = raycastHit.point, IsInWorld = true, IsInCable = false };
                }
            } else {
                return InScenePosition.None;
            }
        }

        //private System.Tuple<SplineControlPoint, SplineControlPoint> GetSelectedControlPoint(RaycastHit raycastHit, RoadSpline spline)
        //{
        //    // Each segment consist of a |RoundSegments| quad, for a total of RoundSegments * 2 tris or RoundSegments * 6 vertices.
        //    var segmentIndex = raycastHit.triangleIndex / (2 * spline.RoundSegments);
        //    var placedSegmentIndex = Mathf.Clamp((segmentIndex + 1) / (spline.SmoothnessLevel + 1), 0, int.MaxValue);
        //    var previousSegmentIndex = Mathf.Clamp(segmentIndex / (spline.SmoothnessLevel + 1), 0, int.MaxValue);

        //    if (previousSegmentIndex == placedSegmentIndex) {
        //        previousSegmentIndex = placedSegmentIndex + 1;
        //    }

        //    return new System.Tuple<SplineControlPoint, SplineControlPoint>(spline.ControlPoints[placedSegmentIndex], spline.ControlPoints[previousSegmentIndex]);
        //}


        private void RemoveControlPoint(Event currentEvent, RoadSpline spline, InScenePosition inScenePosition)
        {
            if (currentEvent.type == EventType.MouseDown) {
                Undo.RecordObject(spline, "Removed control point");
                //spline.RemoveControlPoint(inScenePosition.ControlPointsPairs.Item1);
                currentEvent.Use();
            }
        }

        private GUIStyle CreateInSceneTextStyle()
        {
            return new GUIStyle() {
                normal = new GUIStyleState {
                    textColor = Color.white,
                },
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold,
                fontSize = 12
            };
        }

        private void ShowControlPoints(RoadSpline spline)
        {
            foreach (var point in spline.ControlPoints) {
                ShowControlPoint(point, spline);
            }
        }

        private void ShowControlPoint(SplineControlPoint controlPoint, RoadSpline spline)
        {
            var position = spline.transform.position + controlPoint.Position;

            EditorGUI.BeginChangeCheck();
            controlPoint.Position = Handles.DoPositionHandle(position, controlPoint.Direction) - spline.transform.position;
            var rotation = Handles.Disc(controlPoint.Direction, position, new Vector3(0, 1, 0), spline.Width, false, 0);
            rotation = Quaternion.Euler(0, rotation.eulerAngles.y, 0);
            controlPoint.Direction = rotation;
            if (EditorGUI.EndChangeCheck()) {
                Undo.RecordObject(spline, "Edited Cable control point");
                spline.UpdateControlPoint(controlPoint);
                spline.UpdateMesh();
                EditorUtility.SetDirty(spline);
            }
        }

        private void DrawCurvedLine(RoadSpline spline)
        {
            foreach (var controlPoint in spline.ControlPoints) {
                if(controlPoint.NextIndex == -1) {
                    continue;
                }

                var nextPoint = spline.ControlPoints[controlPoint.NextIndex];

                var firstPosition = controlPoint.Position + spline.transform.position;
                var firstTangent = controlPoint.ForwardTangent + spline.transform.position;
                var nextTangent = nextPoint.BackwardsTangent + spline.transform.position;
                var nextPosition = nextPoint.Position + spline.transform.position;

                Handles.DrawBezier(firstPosition, nextPosition, firstTangent, nextTangent, Color.green, null, 2);
            }
        }
    }
}