using UnityEditor;
using UnityEditor.EditorTools;
using UnityEngine;

[EditorTool("Voxel Editor")]
public class VoxelEditorTool : EditorTool
{
    private Vector3 _hitPosition;
    private bool _hasHit;

    public override GUIContent toolbarIcon
    {
        get
        {
            return new GUIContent("V");
        }
    }

    public override void OnToolGUI(EditorWindow window)
    {
        Event currentEvent = Event.current;

        // Scene View上のマウス位置からRayを作成
        Ray ray = HandleUtility.GUIPointToWorldRay(currentEvent.mousePosition);

        // Scene上のColliderにRayを飛ばす
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            _hitPosition = hit.point;
            _hasHit = true;
        }
        else
        {
            _hasHit = false;
        }

        // 左クリックされたか
        if (currentEvent.type == EventType.MouseDown &&
            currentEvent.button == 0 &&
            !currentEvent.alt)
        {
            if (_hasHit)
            {
                Debug.Log($"Hit Position: {_hitPosition}");
            }

            currentEvent.Use();
        }

        DrawGUI();

        // Scene Viewを再描画
        if (currentEvent.type == EventType.MouseMove)
        {
            SceneView.RepaintAll();
        }
    }

    private void DrawGUI()
    {
        Handles.BeginGUI();

        GUILayout.BeginArea(
            new Rect(10, 10, 220, 80),
            "Voxel Editor",
            GUI.skin.window
        );

        if (_hasHit)
        {
            GUILayout.Label($"X : {_hitPosition.x:F2}");
            GUILayout.Label($"Y : {_hitPosition.y:F2}");
            GUILayout.Label($"Z : {_hitPosition.z:F2}");
        }
        else
        {
            GUILayout.Label("No Hit");
        }

        GUILayout.EndArea();

        Handles.EndGUI();
    }
}