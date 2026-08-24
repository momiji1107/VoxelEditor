using System.Collections.Generic;
using UnityEditor;
using UnityEditor.EditorTools;
using UnityEngine;

[EditorTool("Voxel Editor")]
public class VoxelEditorTool : EditorTool
{
    private enum ToolMode
    {
        Pen,
        Eraser
    }
    
    private VoxelWorld _voxelWorld;
    private Vector3 _hitPosition;
    private Vector3Int _gridPosition;
    private bool _hasHit;
    
    private bool _isDragging;
    private Vector3Int _lastPlacedPosition;
    private bool _hasLastPlacedPosition;
    
    private ToolMode _toolMode = ToolMode.Pen;
    private GameObject _eraseTarget;

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

        if (_toolMode == ToolMode.Pen)
        {
            UpdatePlacementPosition(currentEvent);
        }
        else
        {
            UpdateEraseTarget(currentEvent);
        }

        DrawPlacementPreview();
        DrawErasePreview();
        DrawGUI();

        if (currentEvent.type == EventType.MouseDown &&
            currentEvent.button == 0 &&
            !currentEvent.alt)
        {
            _isDragging = true;
            _hasLastPlacedPosition = false;

            if (_toolMode == ToolMode.Pen)
            {
                if (_hasHit)
                {
                    PlaceBlock();
                }
            }
            else
            {
                EraseBlock();
            }

            currentEvent.Use();
        }
        
        if (currentEvent.type == EventType.MouseDrag &&
            currentEvent.button == 0 &&
            _isDragging &&
            !currentEvent.alt)
        {
            if (_toolMode == ToolMode.Pen)
            {
                if (_hasHit)
                {
                    PlaceDraggedBlocks();
                }
            }
            else
            {
                EraseBlock();
            }

            currentEvent.Use();
        }
        
        if (currentEvent.type == EventType.MouseUp &&
            currentEvent.button == 0)
        {
            _isDragging = false;
            _hasLastPlacedPosition = false;
        }

        if (currentEvent.type == EventType.MouseMove)
        {
            SceneView.RepaintAll();
        }
    }
    
    private void UpdatePlacementPosition(
        Event currentEvent)
    {
        Ray ray =
            HandleUtility.GUIPointToWorldRay(
                currentEvent.mousePosition
            );

        if (Physics.Raycast(
                ray,
                out RaycastHit hit))
        {
            Vector3 placementPosition =
                hit.point +
                hit.normal *
                (VoxelGridUtility.CellSize * 0.5f);

            _gridPosition =
                VoxelGridUtility.WorldToGrid(
                    placementPosition
                );

            _hasHit = true;

            return;
        }

        if (_voxelWorld != null &&
            TryGetMinimumHeightPosition(
                ray,
                out Vector3Int minimumPosition))
        {
            _gridPosition = minimumPosition;

            _hasHit = true;

            return;
        }

        _hasHit = false;
    }
    
    private void UpdateEraseTarget(
        Event currentEvent)
    {
        _eraseTarget = null;

        Ray ray =
            HandleUtility.GUIPointToWorldRay(
                currentEvent.mousePosition
            );

        if (!Physics.Raycast(
                ray,
                out RaycastHit hit))
        {
            return;
        }

        GameObject hitObject =
            hit.collider.gameObject;

        GameObject rootObject =
            PrefabUtility.GetOutermostPrefabInstanceRoot(
                hitObject
            );

        _eraseTarget =
            rootObject != null
                ? rootObject
                : hitObject;
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
        
        if (_voxelWorld.IsBelowMinimumHeight(_gridPosition))
        {
            Debug.LogWarning(
                $"Voxel Editor: Minimum Height ({_voxelWorld.MinimumHeight}) より下には配置できません。"
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
    
    private void PlaceDraggedBlocks()
    {
        if (!_hasHit)
        {
            return;
        }

        Vector3Int currentPosition =
            _gridPosition;

        if (!_hasLastPlacedPosition)
        {
            PlaceBlock();

            _lastPlacedPosition =
                currentPosition;

            _hasLastPlacedPosition = true;

            return;
        }

        if (_lastPlacedPosition ==
            currentPosition)
        {
            return;
        }

        foreach (Vector3Int position in
                 GetLinePositions(
                     _lastPlacedPosition,
                     currentPosition))
        {
            _gridPosition = position;

            PlaceBlock();
        }

        _gridPosition = currentPosition;

        _lastPlacedPosition =
            currentPosition;
    }
    
    private void EraseBlock()
    {
        if (_eraseTarget == null)
        {
            return;
        }

        if (_voxelWorld == null)
        {
            return;
        }

        Vector3Int gridPosition =
            VoxelGridUtility.WorldToGrid(
                _eraseTarget.transform.position
            );

        Undo.RecordObject(
            _voxelWorld,
            "Remove Voxel Block"
        );

        _voxelWorld.RemoveBlock(
            gridPosition
        );

        Undo.DestroyObjectImmediate(
            _eraseTarget
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
        if (_toolMode != ToolMode.Pen ||
            !_hasHit)
        {
            return;
        }

        bool canPlace =
            _voxelWorld != null &&
            !_voxelWorld.IsBelowMinimumHeight(
                _gridPosition
            );

        Handles.color =
            canPlace
                ? Color.green
                : Color.red;

        Vector3 worldPosition =
            VoxelGridUtility.GridToWorld(
                _gridPosition
            );

        Handles.DrawWireCube(
            worldPosition,
            Vector3.one *
            VoxelGridUtility.CellSize
        );

        Handles.color = Color.white;
    }
    
    private void DrawErasePreview()
    {
        if (_toolMode != ToolMode.Eraser ||
            _eraseTarget == null)
        {
            return;
        }

        Bounds bounds;

        Renderer[] renderers =
            _eraseTarget.GetComponentsInChildren<Renderer>();

        if (renderers.Length == 0)
        {
            bounds =
                new Bounds(
                    _eraseTarget.transform.position,
                    Vector3.one *
                    VoxelGridUtility.CellSize
                );
        }
        else
        {
            bounds = renderers[0].bounds;

            for (int i = 1; i < renderers.Length; i++)
            {
                bounds.Encapsulate(
                    renderers[i].bounds
                );
            }
        }

        Handles.color = Color.red;

        Handles.DrawWireCube(
            bounds.center,
            bounds.size
        );

        Handles.color = Color.white;
    }

    private void DrawGUI()
    {
        Handles.BeginGUI();

        GUILayout.BeginArea(
            new Rect(10, 10, 240, 190),
            "Voxel Editor",
            GUI.skin.window
        );

        GUILayout.Label("Tool");

        GUILayout.BeginHorizontal();

        if (GUILayout.Toggle(
                _toolMode == ToolMode.Pen,
                "Pen",
                GUI.skin.button))
        {
            _toolMode = ToolMode.Pen;
        }

        if (GUILayout.Toggle(
                _toolMode == ToolMode.Eraser,
                "Eraser",
                GUI.skin.button))
        {
            _toolMode = ToolMode.Eraser;
        }

        GUILayout.EndHorizontal();

        GUILayout.Space(5);

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

        if (_toolMode == ToolMode.Pen)
        {
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
        }
        else
        {
            GUILayout.Label("Eraser Mode");
        }

        GUILayout.EndArea();

        Handles.EndGUI();
    }
    
    private bool TryGetMinimumHeightPosition(
        Ray ray,
        out Vector3Int gridPosition)
    {
        float minimumHeight =
            _voxelWorld.MinimumHeight;

        Plane minimumHeightPlane =
            new Plane(
                Vector3.up,
                new Vector3(
                    0f,
                    minimumHeight,
                    0f
                )
            );

        if (!minimumHeightPlane.Raycast(
                ray,
                out float distance) ||
            distance < 0f)
        {
            gridPosition = default;

            return false;
        }

        Vector3 hitPosition =
            ray.GetPoint(distance);

        gridPosition =
            VoxelGridUtility.WorldToGrid(
                hitPosition
            );

        gridPosition.y =
            Mathf.RoundToInt(
                minimumHeight
            );

        return true;
    }
    
    private IEnumerable<Vector3Int> GetLinePositions(
        Vector3Int start,
        Vector3Int end)
        
    {
        int deltaX =
            end.x - start.x;

        int deltaY =
            end.y - start.y;

        int deltaZ =
            end.z - start.z;

        int steps =
            Mathf.Max(
                Mathf.Abs(deltaX),
                Mathf.Abs(deltaY),
                Mathf.Abs(deltaZ)
            );

        if (steps == 0)
        {
            yield return start;
            yield break;
        }

        for (int i = 1; i <= steps; i++)
        {
            float t =
                (float)i / steps;

            int x =
                Mathf.RoundToInt(
                    Mathf.Lerp(
                        start.x,
                        end.x,
                        t
                    )
                );

            int y =
                Mathf.RoundToInt(
                    Mathf.Lerp(
                        start.y,
                        end.y,
                        t
                    )
                );

            int z =
                Mathf.RoundToInt(
                    Mathf.Lerp(
                        start.z,
                        end.z,
                        t
                    )
                );

            yield return new Vector3Int(
                x,
                y,
                z
            );
        }
    }
}