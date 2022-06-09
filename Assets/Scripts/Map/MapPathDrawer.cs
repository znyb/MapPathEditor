using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteInEditMode]
[SelectionBase]
public class MapPathDrawer : MonoBehaviour
{
    public MapPathData PathData;
    public float gizmoRadius = 0.1f;
    public bool showMeshWire = true;
    public int vertexCount;
    public int trianglesCount;

    public float TileWidth = 1;
    Mesh mesh;

    public MapPathData Data
    {
        get
        {
            return PathData;
        }
    }


    private void Start()
    { 
        mesh = new Mesh();
        mesh.name = "mesh";
        mesh.MarkDynamic();
    }

    private void OnEnable()
    {
        GenerateMesh();
    }

    private void Update()
    {
        GenerateMesh();
        if(Data != null)
            Graphics.DrawMesh(mesh, Matrix4x4.TRS(Vector3.zero,Quaternion.identity,Vector3.one), Data.PathMat,0);
        //Graphics.DrawMeshNow(mesh, Vector3.zero,Quaternion.identity);
    }

    void GenerateMesh()
    {
        //return;
        if (mesh == null)
        {
            mesh = new Mesh();
            mesh.name = "mesh";
            mesh.MarkDynamic();
        }
        mesh.Clear();

        if (Data == null)
            return;

        List<Vector3> vertexs = new List<Vector3>();
        List<Vector2> uvs = new List<Vector2>();
        vertexs.Add(Data.LeftStartPoint);
        uvs.Add(Vector2.zero);
        vertexs.Add(Data.RightStartPoint);
        uvs.Add(Vector2.up);
        Vector3 leftPoint = Data.LeftStartPoint;
        Vector3 rightPoint = Data.RightStartPoint;
        float length = 0;
        Vector3 lastPoint = leftPoint;
        foreach (var segment in Data.PathSegments)
        {
            for(int i = 1;i <= segment.Step; i++)
            {
                float t = i / (float)segment.Step;
                var p = segment.GetLeftPoint(t, leftPoint);
                length += (p - lastPoint).magnitude;
                lastPoint = p;
                float v = length / TileWidth;
                vertexs.Add(p);
                uvs.Add(Vector2.right * v);
                vertexs.Add(segment.GetRightPoint(t, rightPoint));
                uvs.Add(new Vector2(v,1));
            }
            leftPoint = segment.LeftPoint;
            rightPoint = segment.RightPoint;
        }

        mesh.SetVertices(vertexs,0,vertexs.Count);
        mesh.SetUVs(0,uvs);

        List<int> indices = new List<int>();
        for (int i = 0; i < vertexs.Count - 3; i+=2)
        {
            indices.Add(i);
            indices.Add(i + 1);
            indices.Add(i + 2);
            indices.Add(i + 2);
            indices.Add(i + 1);
            indices.Add(i + 3);
        }
        mesh.SetTriangles(indices,0);
        mesh.RecalculateNormals();

        vertexCount = vertexs.Count;
        trianglesCount = indices.Count / 3;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        GenerateMesh();
    }

    private void OnDrawGizmos()
    {
        if (Data == null)
            return;

        Vector3 leftPoint = Data.LeftStartPoint;
        Vector3 rightPoint = Data.RightStartPoint;
        DrawPointGizmo(leftPoint,"start");
        DrawPointGizmo(rightPoint);

        for (int pathIndex = 0; pathIndex < Data.PathSegments.Count; pathIndex++)
        {
            MapBezierPathSegment segment = Data.PathSegments[pathIndex];
            for (int i = 1; i <= segment.Step; i++)
            {
                float t = i / (float)segment.Step;
                var lp = segment.GetLeftPoint(t, leftPoint);

                var rp = segment.GetRightPoint(t, rightPoint);

            }

            foreach(var cp in segment.LeftControlPoints)
            {
                DrawControlPointGizmo(cp);
                DrawLine(leftPoint, cp);
                leftPoint = cp;
            }

            foreach (var cp in segment.RightControlPoints)
            {
                DrawControlPointGizmo(cp);
                DrawLine(rightPoint, cp);
                rightPoint = cp;
            }

            DrawPointGizmo(segment.LeftPoint,pathIndex.ToString());
            DrawLine(leftPoint, segment.LeftPoint);
            DrawPointGizmo(segment.RightPoint);
            DrawLine(rightPoint, segment.RightPoint);
            leftPoint = segment.LeftPoint;
            rightPoint = segment.RightPoint;
        }

        if(showMeshWire)
            Gizmos.DrawWireMesh(mesh);
    }

    void DrawPointGizmo(Vector3 pos,string label = null)
    {
        var color = Gizmos.color;
        Gizmos.color = Color.green;
        Gizmos.DrawSphere(pos, gizmoRadius);
        if(!string.IsNullOrEmpty(label))
            Handles.Label(pos + Vector3.one * gizmoRadius, label);
        Gizmos.color = color;
    }

    void DrawControlPointGizmo(Vector3 pos)
    {
        var color = Gizmos.color;
        Gizmos.color = Color.blue;
        Gizmos.DrawSphere(pos, gizmoRadius);
        Gizmos.color = color;
    }

    void DrawLine(Vector3 start,Vector3 end)
    {
        var color = Gizmos.color;
        Gizmos.color = Color.red;
        Gizmos.DrawLine(start, end);
        Gizmos.color = color;
    }
#endif
}
