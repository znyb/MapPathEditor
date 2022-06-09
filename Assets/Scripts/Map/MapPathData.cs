using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[Serializable]
public class MapBezierPathSegment
{
    public int Step = 1;
    public Vector3 LeftPoint;
    public Vector3 RightPoint;
    public List<Vector3> LeftControlPoints = new List<Vector3>();
    public List<Vector3> RightControlPoints = new List<Vector3>();

    public Vector3 GetLeftPoint(float t, Vector3 start)
    {
        t = Mathf.Clamp01(t);
        return GetPoint(t, start, LeftPoint, LeftControlPoints, -1, LeftControlPoints.Count);
    }

    public Vector3 GetRightPoint(float t, Vector3 start)
    {
        t = Mathf.Clamp01(t);
        return GetPoint(t, start, RightPoint, RightControlPoints, -1, RightControlPoints.Count);
    }

    Vector3 GetPoint(float t, Vector3 start, Vector3 end, List<Vector3> controlPoints, int index1, int index2)
    {
        if (index2 - index1 <= 1)
        {
            Vector3 p1 = Vector3.zero;
            Vector3 p2 = Vector3.zero;
            if (index1 < -1)
            {
                Debug.LogError("bezier index1 out of range " + index1);
            }
            else if (index1 == -1)
            {
                p1 = start;
            }
            else
            {
                p1 = controlPoints[index1];
            }

            if (index2 > controlPoints.Count)
            {
                Debug.LogError("bezier index2 out of range " + index1);
            }
            else if (index2 == controlPoints.Count)
            {
                p2 = end;
            }
            else
            {
                p2 = controlPoints[index2];
            }

            return (1 - t) * p1 + t * p2;
        }
        return (1 - t) * GetPoint(t, start, end, controlPoints, index1, index2 - 1) + t * GetPoint(t, start, end, controlPoints, index1 + 1, index2);
    }
}


[CreateAssetMenu(menuName = "MapPathData")]
public class MapPathData : ScriptableObject
{
    public Material PathMat;
    public Vector3 LeftStartPoint;
    public Vector3 RightStartPoint;
    public List<MapBezierPathSegment> PathSegments = new List<MapBezierPathSegment>();
}
