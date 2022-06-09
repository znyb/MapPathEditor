using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.EditorTools;
using System.Linq;

[EditorTool("路径编辑", typeof(MapPathDrawer))]
public class MapPathEditorTool : UnityEditor.EditorTools.EditorTool
{
    class ClickInfo
    {
        public Vector3 Position;
        public int PathIndex;
        public int ControlIndex;
    }

    bool lastSceneViewOrtho;
    Quaternion lastSceneCameraRotation;
    Event lastEvent;
    GUIContent icon;
    public override GUIContent toolbarIcon
    {
        get
        {
            if (icon == null)
            {
                icon = new GUIContent(EditorGUIUtility.ObjectContent(target, typeof(MapPathDrawer)));
                icon.tooltip = "路径编辑";
            }
            return icon;
        }
    }

    public override void OnActivated()
    {
        base.OnActivated();
        HandleUtility.pickGameObjectCustomPasses += PickSelf;

        var sceneView = SceneView.lastActiveSceneView;
        lastSceneViewOrtho = sceneView.orthographic;
        lastSceneCameraRotation = sceneView.rotation;
        SceneView.beforeSceneGui += SceneView_beforeSceneGui;
        var drawer = target as MapPathDrawer;
        var cameraCenter = drawer.transform.position;
        var ray = sceneView.camera.ViewportPointToRay(new Vector3(0.5f, 0.5f));
        Plane plane = new Plane(Vector3.up, 0);
        if(plane.Raycast(ray,out float dis))
        {
            cameraCenter = ray.GetPoint(dis);
        }
        sceneView.LookAt(cameraCenter, Quaternion.AngleAxis(90,Vector3.right), sceneView.size, true, false);
        sceneView.isRotationLocked = true;
    }

    private void SceneView_beforeSceneGui(SceneView obj)
    {
        var e = Event.current;
        if (e.type == EventType.MouseDrag && e.button == 0)
        {
            lastEvent = new Event(e);
            e.Use();
            return;
        }
        lastEvent = null;
    }

    public override void OnWillBeDeactivated()
    {
        base.OnWillBeDeactivated();
        var sceneView = SceneView.lastActiveSceneView;
        SceneView.beforeSceneGui -= SceneView_beforeSceneGui;
        HandleUtility.pickGameObjectCustomPasses -= PickSelf;
        sceneView.isRotationLocked = false;
        var drawer = target as MapPathDrawer;
        if (drawer == null)
            return;

        var cameraCenter = drawer.transform.position;
        var ray = sceneView.camera.ViewportPointToRay(new Vector3(0.5f, 0.5f));
        Plane plane = new Plane(Vector3.up, 0);
        if (plane.Raycast(ray, out float dis))
        {
            cameraCenter = ray.GetPoint(dis);
        }
        sceneView.LookAt(cameraCenter, lastSceneCameraRotation, sceneView.size, lastSceneViewOrtho, false);
    }

    private GameObject PickSelf(Camera cam, int layers, Vector2 position, GameObject[] ignore, GameObject[] filter, out int materialIndex)
    {
        materialIndex = 0;
        return (target as MapPathDrawer).gameObject;
    }

    public override void OnToolGUI(EditorWindow window)
    {
        base.OnToolGUI(window);

        Event e = Event.current;

        if(e.type == EventType.MouseUp)
        {
            if(e.button == 1)
            {
                Tools.viewTool = ViewTool.None;
                OnRightClick();
            }
        }

        if(lastEvent != null)
            Event.current = lastEvent;

        var drawer = target as MapPathDrawer;
        if (drawer == null)
            return;

        var data = drawer.Data;
        if (data == null)
        {
            Debug.Log("null");
            return;
        }

        var leftPoint = Handles.PositionHandle(data.LeftStartPoint, Quaternion.identity);
        if (leftPoint != data.LeftStartPoint)
        {
            Record("Line Change");
            data.LeftStartPoint = leftPoint;
        }

        var rightPoint = Handles.PositionHandle(data.RightStartPoint, Quaternion.identity);
        if (rightPoint != data.RightStartPoint)
        {
            Record("Line Change");
            data.RightStartPoint = rightPoint;
        }

        for(int i = 0; i < data.PathSegments.Count; i++)
        {
            var segment = data.PathSegments[i];
            leftPoint = Handles.PositionHandle(segment.LeftPoint, Quaternion.identity);
            if (leftPoint != segment.LeftPoint)
            {
                Record("Line Change");
                segment.LeftPoint = leftPoint;
            }

            rightPoint = Handles.PositionHandle(segment.RightPoint, Quaternion.identity);
            if (rightPoint != segment.RightPoint)
            {
                Record("Line Change");
                segment.RightPoint = rightPoint;
            }

            for(int j = 0; j < segment.LeftControlPoints.Count; j++)
            {
                leftPoint = Handles.PositionHandle(segment.LeftControlPoints[j], Quaternion.identity);
                if (leftPoint != segment.LeftControlPoints[j])
                {
                    Record("Line Change");
                    segment.LeftControlPoints[j] = leftPoint;
                }
            }

            for (int j = 0; j < segment.RightControlPoints.Count; j++)
            {
                rightPoint = Handles.PositionHandle(segment.RightControlPoints[j], Quaternion.identity);
                if (rightPoint != segment.RightControlPoints[j])
                {
                    Record("Line Change");
                    segment.RightControlPoints[j] = rightPoint;
                }
            }
        }
    }

    void Record(string opt)
    {
        var data = (target as MapPathDrawer).Data;
        if (data == null)
            return;

        Undo.RecordObject(data, opt);
        EditorUtility.SetDirty(data);
    }

    void OnRightClick()
    {
        var ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
        Plane plane = new Plane(Vector3.up, 0);
        if(plane.Raycast(ray,out float dis))
        {
            Vector3 position = ray.GetPoint(dis);
            var drawer = target as MapPathDrawer;
            var data = drawer.Data;
            if (data == null)
                return;

            var leftPoint = new Vector2(data.LeftStartPoint.x, data.LeftStartPoint.z);
            var rightPoint = new Vector2(data.RightStartPoint.x, data.RightStartPoint.z);
            List<Vector2> vertexs = new List<Vector2>();
            for (int i = 0; i < data.PathSegments.Count; i++)
            {
                var segment = data.PathSegments[i];
                int count = Mathf.Max(segment.LeftControlPoints.Count, segment.RightControlPoints.Count);
                for (int j = 0; j <= count; j++)
                {
                    vertexs.Clear();
                    vertexs.Add(leftPoint);
                    vertexs.Add(rightPoint);

                    if(j < segment.RightControlPoints.Count)
                    {
                        rightPoint = new Vector2(segment.RightControlPoints[j].x, segment.RightControlPoints[j].z);
                        vertexs.Add(rightPoint);
                    }
                    else if(j == segment.RightControlPoints.Count)
                    {
                        rightPoint = new Vector2(segment.RightPoint.x, segment.RightPoint.z);
                        vertexs.Add(rightPoint);
                    }

                    if(j < segment.LeftControlPoints.Count)
                    {
                        leftPoint = new Vector2(segment.LeftControlPoints[j].x, segment.LeftControlPoints[j].z);
                        vertexs.Add(leftPoint);
                    }
                    else if (j == segment.LeftControlPoints.Count)
                    {
                        leftPoint = new Vector2(segment.LeftPoint.x, segment.LeftPoint.z);
                        vertexs.Add(leftPoint);
                    }

                    if(IsPointInPolygon(new Vector2(position.x, position.z), vertexs))
                    {
                        var info = new ClickInfo()
                        {
                            Position = position,
                            PathIndex = i, 
                            ControlIndex = j
                        };
                        GenericMenu insertMenu = new GenericMenu();
                        insertMenu.AddItem(new GUIContent("插入路径点"), false, InsertPoint, info);
                        insertMenu.AddItem(new GUIContent("插入路径控制点"), false, InsertControlPoint, info);
                        insertMenu.ShowAsContext();

                        return;
                    }
                }
            }

            GenericMenu addMenu = new GenericMenu();
            addMenu.AddItem(new GUIContent("添加路径起点"), false, AddStartPoint, position);
            addMenu.AddItem(new GUIContent("添加路径终点"), false, AddEndPoint, position);
            addMenu.ShowAsContext();
        }

    }

    void InsertPoint(object obj)
    {
        Record("Add Point");
        var info = obj as ClickInfo;
        var drawer = target as MapPathDrawer;
        var data = drawer.Data;
        if (data == null)
            return;

        if(info.PathIndex >= data.PathSegments.Count)
        {
            Debug.LogError($"路径点插入位置错误,pathIndex:{info.PathIndex},path segment count:{data.PathSegments.Count}");
            return;
        }

        var segment = data.PathSegments[info.PathIndex];
        if (info.ControlIndex > segment.LeftControlPoints.Count && info.ControlIndex > segment.RightControlPoints.Count)
        {
            Debug.LogError($"路径点插入位置错误,ControlIndex:{info.ControlIndex}," +
                $"segment LeftControlPoints count:{segment.LeftControlPoints.Count}" +
                $"segment RightControlPoints count:{segment.RightControlPoints.Count}");
            return;
        }

        var offset = (segment.LeftPoint - segment.RightPoint) / 2;
        var newSegment = new MapBezierPathSegment();
        newSegment.LeftPoint = info.Position + offset;
        newSegment.RightPoint = info.Position - offset;
        newSegment.LeftControlPoints = segment.LeftControlPoints.GetRange(0, info.ControlIndex);
        newSegment.RightControlPoints = segment.RightControlPoints.GetRange(0, info.ControlIndex);
        data.PathSegments.Insert(info.PathIndex, newSegment);

        segment.LeftControlPoints.RemoveRange(0, info.ControlIndex);
        segment.RightControlPoints.RemoveRange(0, info.ControlIndex);

    }

    void InsertControlPoint(object obj)
    {
        Record("Add Control Point");
        var info = obj as ClickInfo;
        var drawer = target as MapPathDrawer;
        var data = drawer.Data;
        if (data == null)
            return;

        if (info.PathIndex >= data.PathSegments.Count)
        {
            Debug.LogError($"控制点插入位置错误,pathIndex:{info.PathIndex},path segment count:{data.PathSegments.Count}");
            return;
        }

        var segment = data.PathSegments[info.PathIndex];
        if(info.ControlIndex > segment.LeftControlPoints.Count && info.ControlIndex > segment.RightControlPoints.Count)
        {
            Debug.LogError($"控制点插入位置错误,ControlIndex:{info.ControlIndex}," +
                $"segment LeftControlPoints count:{segment.LeftControlPoints.Count}" +
                $"segment RightControlPoints count:{segment.RightControlPoints.Count}");
            return;
        }

        var offset = (segment.LeftPoint - segment.RightPoint) / 2;
        if(info.ControlIndex >= segment.LeftControlPoints.Count)
            segment.LeftControlPoints.Add(info.Position + offset);
        else
            segment.LeftControlPoints.Insert(info.ControlIndex, info.Position + offset);

        if(info.ControlIndex >= segment.RightControlPoints.Count)
            segment.RightControlPoints.Add(info.Position - offset);
        else
            segment.RightControlPoints.Insert(info.ControlIndex, info.Position - offset);

    }

    void AddStartPoint(object obj)
    {
        Record("Add Start Point");
        Vector3 position = (Vector3)obj;
        var drawer = target as MapPathDrawer;
        var data = drawer.Data;
        if (data == null)
            return;

        var offset = (data.LeftStartPoint - data.RightStartPoint) / 2;
        var newSegment = new MapBezierPathSegment();
        newSegment.LeftPoint = data.LeftStartPoint;
        newSegment.RightPoint = data.RightStartPoint;
        data.PathSegments.Insert(0, newSegment);

        data.LeftStartPoint = position + offset;
        data.RightStartPoint = position - offset;
    }

    void AddEndPoint(object obj)
    {
        Record("Add End Point");
        Vector3 position = (Vector3)obj;
        var drawer = target as MapPathDrawer;
        var data = drawer.Data;
        if (data == null)
            return;

        var leftPoint = data.LeftStartPoint;
        var rightPoint = data.RightStartPoint;
        if(data.PathSegments.Count > 0)
        {
            var segment = data.PathSegments.Last();
            leftPoint = segment.LeftPoint;
            rightPoint = segment.RightPoint;
        }

        var offset = (leftPoint - rightPoint) / 2;
        var newSegment = new MapBezierPathSegment();
        newSegment.LeftPoint = position + offset;
        newSegment.RightPoint = position - offset;
        data.PathSegments.Add(newSegment);
    }

    public static bool IsPointInPolygon(Vector2 p, List<Vector2> vertexs)
    {
        int crossNum = 0;
        int vertexCount = vertexs.Count;

        for (int i = 0; i < vertexCount; i++)
        {
            Vector2 v1 = vertexs[i];
            Vector2 v2 = vertexs[(i + 1) % vertexCount];

            if (((v1.y <= p.y) && (v2.y > p.y))
                || ((v1.y > p.y) && (v2.y <= p.y)))
            {
                if (p.x < v1.x + (p.y - v1.y) / (v2.y - v1.y) * (v2.x - v1.x))
                {
                    crossNum += 1;
                }
            }
        }

        if (crossNum % 2 == 0)
        {
            return false;
        }
        else
        {
            return true;
        }
    }
}
