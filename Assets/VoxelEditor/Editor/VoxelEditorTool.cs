using UnityEditor;
using UnityEditor.EditorTools;
using UnityEngine;

[EditorTool("Voxel Editor")]
public class VoxelEditorTool : EditorTool
{
    private Vector3 _hitPosition;
    private Vector3Int _gridPosition;
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

        Ray ray = HandleUtility.GUIPointToWorldRay(currentEvent.mousePosition);

        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            _hitPosition = hit.point;

            // Rayが当たった面の法線方向へ1ブロック分移動
            Vector3 placementPosition =
                hit.point + hit.normal * (VoxelGridUtility.CellSize * 0.5f);

            _gridPosition =
                VoxelGridUtility.WorldToGrid(placementPosition);

            _hasHit = true;
        }
        else
        {
            _hasHit = false;
        }

        DrawPlacementPreview();
        DrawGUI();

        if (currentEvent.type == EventType.MouseDown &&
            currentEvent.button == 0 &&
            !currentEvent.alt)
        {
            if (_hasHit)
            {
                Debug.Log(
                    $"Placement Grid Position: {_gridPosition}"
                );
            }

            currentEvent.Use();
        }

        if (currentEvent.type == EventType.MouseMove)
        {
            SceneView.RepaintAll();
        }
    }

    private void DrawPlacementPreview()
    {
        if (!_hasHit)
        {
            return;
        }

        Vector3 worldPosition =
            VoxelGridUtility.GridToWorld(_gridPosition);

        Handles.color = Color.green;

        Handles.DrawWireCube(
            worldPosition,
            Vector3.one * VoxelGridUtility.CellSize
        );

        Handles.color = Color.white;
    }

    private void DrawGUI()
    {
        Handles.BeginGUI();

        GUILayout.BeginArea(
            new Rect(10, 10, 240, 120),
            "Voxel Editor",
            GUI.skin.window
        );

        if (_hasHit)
        {
            GUILayout.Label("Placement Position");

            GUILayout.Label(
                $"X : {_gridPosition.x}"
            );

            GUILayout.Label(
                $"Y : {_gridPosition.y}"
            );

            GUILayout.Label(
                $"Z : {_gridPosition.z}"
            );
        }
        else
        {
            GUILayout.Label("No Hit");
        }

        GUILayout.EndArea();

        Handles.EndGUI();
    }
}