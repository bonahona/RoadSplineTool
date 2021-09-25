using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Fyrvall.BonaRoadTool
{
    public static class RoadToolMenuItem
    {
        [MenuItem("GameObject/3D Object/Road Spline")]
        public static void CreateRoadSpline()
        {
            var position = GetMiddleOfViewPort();
            var roadSpline = CreateRoadSplineObject(position);

            roadSpline.IsEditable = true;
            Selection.activeGameObject = roadSpline.gameObject;

        }

        private static Vector3 GetMiddleOfViewPort()
        {
            var middleOfViewRay = SceneView.lastActiveSceneView.camera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 1));
            if (Physics.Raycast(middleOfViewRay, out RaycastHit rayCasthit)) {
                return rayCasthit.point;
            } else {
                return new Vector3(0, 0, 0);
            }
        }

        private static RoadSpline CreateRoadSplineObject(Vector3 position)
        {
            var roadGameObject = new GameObject("Road");
            var roadComponen = roadGameObject.AddComponent<RoadSpline>();
            roadComponen.transform.position = position;

            return roadComponen;
        }
    }
}