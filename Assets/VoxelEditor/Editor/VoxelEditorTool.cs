using UnityEditor;
using UnityEditor.EditorTools;
using UnityEngine;

[EditorTool("Voxel Editor")]
public class VoxelEditorTool : EditorTool
{
    private VoxelWorld _voxelWorld;
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
        _voxelWorld = FindFirstObjectByType<VoxelWorld>();
        
        Event currentEvent = Event.current;

        Ray ray = HandleUtility.GUIPointToWorldRay(currentEvent.mousePosition);

        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            _hitPosition = hit.point;

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
                PlaceBlock();
            }

            currentEvent.Use();
        }

        if (currentEvent.type == EventType.MouseMove)
        {
            SceneView.RepaintAll();
        }
    }

    private void PlaceBlock()
    {
        GameObject prefab = GetSelectedPrefab();

        if (prefab == null)
        {
            Debug.LogWarning(
                "Voxel Editor: ProjectウィンドウでPrefabを選択してください。"
            );

            return;
        }

        Vector3 worldPosition =
            VoxelGridUtility.GridToWorld(_gridPosition);

        GameObject block =
            (GameObject)PrefabUtility.InstantiatePrefab(prefab);

        block.transform.position = worldPosition;

        Undo.RegisterCreatedObjectUndo(
            block,
            "Place Voxel Block"
        );
        
        Undo.RecordObject(
            _voxelWorld,
            "Add Voxel Block"
        );

        _voxelWorld.AddBlock(
            _gridPosition,
            prefab,
            block.transform.rotation
        );

        Debug.Log(
            $"Placed: {prefab.name} at {_gridPosition}"
        );
    }

    private GameObject GetSelectedPrefab()
    {
        Object selectedObject = Selection.activeObject;

        if (selectedObject == null)
        {
            return null;
        }

        if (selectedObject is not GameObject selectedGameObject)
        {
            return null;
        }

        if (!AssetDatabase.Contains(selectedGameObject))
        {
            return null;
        }

        if (PrefabUtility.GetPrefabAssetType(selectedGameObject) ==
            PrefabAssetType.NotAPrefab)
        {
            return null;
        }

        return selectedGameObject;
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
            new Rect(10, 10, 240, 140),
            "Voxel Editor",
            GUI.skin.window
        );

        GameObject prefab = GetSelectedPrefab();

        if (prefab != null)
        {
            GUILayout.Label($"Prefab : {prefab.name}");
        }
        else
        {
            GUILayout.Label("Prefab : None");
        }

        GUILayout.Space(5);

        if (_hasHit)
        {
            GUILayout.Label("Placement Position");
            GUILayout.Label($"X : {_gridPosition.x}");
            GUILayout.Label($"Y : {_gridPosition.y}");
            GUILayout.Label($"Z : {_gridPosition.z}");
        }
        else
        {
            GUILayout.Label("No Hit");
        }

        GUILayout.EndArea();

        Handles.EndGUI();
    }
}