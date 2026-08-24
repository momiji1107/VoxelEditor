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

    private Vector3Int _dragPreviewPosition;
    private bool _hasDragPreview;

    private VoxelWorld _voxelWorld;

    private Vector3Int _gridPosition;
    private bool _hasHit;

    // -------------------------
    // ドラッグ設定
    // -------------------------

    [SerializeField]
    private bool _penDragEnabled = true;

    [SerializeField]
    private bool _eraserDragEnabled = true;

    // -------------------------
    // ドラッグ状態
    // -------------------------

    private bool _hasDragStart;
    private bool _isDragging;

    private bool _hasLastPlacedPosition;

    private Vector3Int _dragStartPosition;
    private Vector3Int _lastPlacedPosition;

    private Vector3Int _dragDirection;

    // -------------------------
    // ツール
    // -------------------------

    private ToolMode _toolMode =
        ToolMode.Pen;

    private GameObject _eraseTarget;

    public override GUIContent toolbarIcon
    {
        get
        {
            return new GUIContent("V");
        }
    }

    public override void OnToolGUI(
        EditorWindow window)
    {
        _voxelWorld =
            FindFirstObjectByType<VoxelWorld>();

        Event currentEvent =
            Event.current;

        // -------------------------
        // 通常時のカーソル判定
        // -------------------------

        if (_toolMode == ToolMode.Pen)
        {
            UpdatePlacementPosition(
                currentEvent
            );
        }
        else
        {
            UpdateEraseTarget(
                currentEvent
            );
        }

        // -------------------------
        // ドラッグ中の処理
        // -------------------------

        if (currentEvent.type ==
                EventType.MouseDrag &&
            currentEvent.button == 0 &&
            _isDragging &&
            !currentEvent.alt)
        {
            if (_toolMode ==
                ToolMode.Pen)
            {
                if (_penDragEnabled)
                {
                    PlaceDraggedBlocks(
                        currentEvent
                    );
                }
            }
            else
            {
                if (_eraserDragEnabled)
                {
                    EraseBlock();
                }
            }

            currentEvent.Use();
        }

        // -------------------------
        // プレビュー
        // -------------------------

        DrawPlacementPreview();
        DrawErasePreview();
        DrawGUI();

        // -------------------------
        // ドラッグ開始
        // -------------------------

        if (currentEvent.type ==
                EventType.MouseDown &&
            currentEvent.button == 0 &&
            !currentEvent.alt)
        {
            StartMouseDrag(
                currentEvent
            );

            currentEvent.Use();
        }

        // -------------------------
        // ドラッグ終了
        // -------------------------

        if (currentEvent.type ==
                EventType.MouseUp &&
            currentEvent.button == 0)
        {
            EndMouseDrag();

            currentEvent.Use();
        }

        // -------------------------
        // カーソル移動
        // -------------------------

        if (currentEvent.type ==
            EventType.MouseMove)
        {
            SceneView.RepaintAll();
        }
    }

    // =========================================================
    // マウス操作
    // =========================================================

    private void StartMouseDrag(
        Event currentEvent)
    {
        _hasLastPlacedPosition = false;
        _hasDragStart = false;

        _dragDirection =
            Vector3Int.zero;

        if (_toolMode ==
            ToolMode.Pen)
        {
            if (!_hasHit)
            {
                _isDragging = false;

                return;
            }

            if (_penDragEnabled)
            {
                _isDragging = true;

                StartPenDrag(
                    currentEvent
                );
            }
            else
            {
                _isDragging = false;

                PlaceBlock();
            }

            return;
        }

        // -------------------------
        // Eraser
        // -------------------------

        if (_eraserDragEnabled)
        {
            _isDragging = true;
        }
        else
        {
            _isDragging = false;
        }

        EraseBlock();
    }

    private void EndMouseDrag()
    {
        _isDragging = false;

        _hasLastPlacedPosition = false;

        _hasDragStart = false;

        _dragDirection =
            Vector3Int.zero;

        _hasDragPreview = false;

        SceneView.RepaintAll();
    }

    // =========================================================
    // 配置位置
    // =========================================================

    /// <summary>
    /// カーソルの指す対象から配置位置を取得します。
    /// </summary>
    private void UpdatePlacementPosition(
        Event currentEvent)
    {
        // ドラッグ中のペンでは、
        // ブロックの面ではなくカーソル移動方向を使うため、
        // 通常の配置位置更新を行います。
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
            _gridPosition =
                minimumPosition;

            _hasHit = true;

            return;
        }

        _hasHit = false;
    }

    // =========================================================
    // 消しゴム対象
    // =========================================================

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

    // =========================================================
    // ペン
    // =========================================================

    private void StartPenDrag(
        Event currentEvent)
    {
        _dragStartPosition =
            _gridPosition;

        _lastPlacedPosition =
            _gridPosition;

        _dragDirection =
            Vector3Int.zero;

        _dragPreviewPosition =
            _gridPosition;

        _hasDragPreview = true;

        _hasDragStart = true;

        _hasLastPlacedPosition = false;

        PlaceBlock();

        _hasLastPlacedPosition = true;

        SceneView.RepaintAll();
    }

    /// <summary>
    /// ドラッグ中のブロック配置。
    /// 「開始位置からの方向」ではなく、
    /// 「前回のカーソル位置から今回のカーソル位置」
    /// の移動方向を使用します。
    /// </summary>
    private void PlaceDraggedBlocks(
        Event currentEvent)
    {
        if (!_hasDragStart ||
            !_hasLastPlacedPosition)
        {
            return;
        }

        // 最後に置いたブロックを基準に、
        // カーソルがまだそのブロック上にあるか確認する
        if (IsCursorOverLastPlacedBlock(
                currentEvent))
        {
            return;
        }

        // カーソルが最後に置いたブロックから外れたので、
        // 次に置く位置を決定する
        Vector3Int nextPosition =
            GetNearestAdjacentPosition(
                currentEvent
            );

        if (_voxelWorld == null)
        {
            return;
        }

        // 最低高度より下なら配置しない
        if (_voxelWorld.IsBelowMinimumHeight(
                nextPosition))
        {
            _dragPreviewPosition =
                nextPosition;

            _hasDragPreview = true;

            SceneView.RepaintAll();

            return;
        }

        // すでにブロックがある場合は配置しない
        if (_voxelWorld.HasBlock(
                nextPosition))
        {
            _dragPreviewPosition =
                nextPosition;

            _hasDragPreview = true;

            SceneView.RepaintAll();

            return;
        }

        // 配置位置を更新
        _gridPosition =
            nextPosition;

        PlaceBlock();

        // 今配置したブロックを
        // 次の基準ブロックにする
        _lastPlacedPosition =
            nextPosition;

        _dragPreviewPosition =
            nextPosition;

        _hasDragPreview = true;

        SceneView.RepaintAll();
    }

    /// <summary>
    /// マウスの移動方向から
    /// ブロックの配置方向を決定します。
    /// </summary>
    private Vector3Int GetMouseDragDirection(
        Vector2 mouseDelta)
    {
        float absX =
            Mathf.Abs(mouseDelta.x);

        float absY =
            Mathf.Abs(mouseDelta.y);

        // --------------------------------
        // 縦方向を優先
        // --------------------------------
        //
        // 「上にドラッグ」と
        // 「奥にドラッグ」が似ていても、
        // 画面上で上下に動いた場合は
        // Y方向として扱います。
        //

        if (absY >= absX)
        {
            if (mouseDelta.y < 0f)
            {
                // SceneViewでは上方向へ
                // マウスを動かした場合、
                // Yを上げる。
                return Vector3Int.up;
            }

            return Vector3Int.down;
        }

        // --------------------------------
        // 横方向
        // --------------------------------

        SceneView sceneView =
            SceneView.lastActiveSceneView;

        if (sceneView == null)
        {
            return mouseDelta.x > 0f
                ? Vector3Int.right
                : Vector3Int.left;
        }

        Camera sceneCamera =
            sceneView.camera;

        if (sceneCamera == null)
        {
            return mouseDelta.x > 0f
                ? Vector3Int.right
                : Vector3Int.left;
        }

        Vector3 cameraRight =
            sceneCamera.transform.right;

        cameraRight.y = 0f;

        if (cameraRight.sqrMagnitude <
            0.0001f)
        {
            return mouseDelta.x > 0f
                ? Vector3Int.right
                : Vector3Int.left;
        }

        cameraRight.Normalize();

        // カメラ右方向に近いワールド軸を選択
        if (Mathf.Abs(cameraRight.x) >=
            Mathf.Abs(cameraRight.z))
        {
            if (mouseDelta.x > 0f)
            {
                return cameraRight.x > 0f
                    ? Vector3Int.right
                    : Vector3Int.left;
            }

            return cameraRight.x > 0f
                ? Vector3Int.left
                : Vector3Int.right;
        }

        if (mouseDelta.x > 0f)
        {
            return cameraRight.z > 0f
                ? Vector3Int.forward
                : Vector3Int.back;
        }

        return cameraRight.z > 0f
            ? Vector3Int.back
            : Vector3Int.forward;
    }

    // =========================================================
    // ブロック配置
    // =========================================================

    private void PlaceBlock()
    {
        if (_voxelWorld == null)
        {
            return;
        }

        GameObject prefab =
            GetSelectedPrefab();

        if (prefab == null)
        {
            Debug.LogWarning(
                "Voxel Editor: ProjectウィンドウでPrefabを選択してください。"
            );

            return;
        }

        if (_voxelWorld.IsBelowMinimumHeight(
                _gridPosition))
        {
            Debug.LogWarning(
                $"Voxel Editor: Minimum Height ({_voxelWorld.MinimumHeight}) より下には配置できません。"
            );

            return;
        }

        if (_voxelWorld.HasBlock(
                _gridPosition))
        {
            return;
        }

        Vector3 worldPosition =
            VoxelGridUtility.GridToWorld(
                _gridPosition
            );

        GameObject block =
            (GameObject)
            PrefabUtility.InstantiatePrefab(
                prefab
            );

        block.transform.position =
            worldPosition;

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

    // =========================================================
    // 消しゴム
    // =========================================================

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

        if (!_voxelWorld.TryGetBlock(
                gridPosition,
                out VoxelBlockData blockData))
        {
            return;
        }

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

    // =========================================================
    // Prefab
    // =========================================================

    private GameObject GetSelectedPrefab()
    {
        Object selectedObject =
            Selection.activeObject;

        if (selectedObject == null)
        {
            return null;
        }

        if (selectedObject is not GameObject selectedGameObject)
        {
            return null;
        }

        if (!AssetDatabase.Contains(
                selectedGameObject))
        {
            return null;
        }

        if (PrefabUtility.GetPrefabAssetType(
                selectedGameObject) ==
            PrefabAssetType.NotAPrefab)
        {
            return null;
        }

        return selectedGameObject;
    }

    // =========================================================
    // プレビュー
    // =========================================================

    private void DrawPlacementPreview()
    {
        if (!_hasHit)
        {
            return;
        }

        Vector3Int previewPosition =
            _gridPosition;

        if (_isDragging &&
            _hasDragStart &&
            _hasDragPreview &&
            _dragDirection !=
            Vector3Int.zero)
        {
            previewPosition =
                _dragPreviewPosition;
        }

        bool canPlace =
            _voxelWorld != null &&
            !_voxelWorld.IsBelowMinimumHeight(
                previewPosition
            ) &&
            !_voxelWorld.HasBlock(
                previewPosition
            );

        Handles.color =
            canPlace
                ? Color.green
                : Color.red;

        Vector3 worldPosition =
            VoxelGridUtility.GridToWorld(
                previewPosition
            );

        Handles.DrawWireCube(
            worldPosition,
            Vector3.one *
            VoxelGridUtility.CellSize
        );

        if (_isDragging &&
            _hasDragStart &&
            _hasDragPreview &&
            _dragDirection !=
            Vector3Int.zero)
        {
            DrawDragPreviewLine(
                _dragStartPosition,
                previewPosition
            );
        }

        Handles.color =
            Color.white;
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
            bounds =
                renderers[0].bounds;

            for (int i = 1;
                 i < renderers.Length;
                 i++)
            {
                bounds.Encapsulate(
                    renderers[i].bounds
                );
            }
        }

        Handles.color =
            Color.red;

        Handles.DrawWireCube(
            bounds.center,
            bounds.size
        );

        Handles.color =
            Color.white;
    }

    private void DrawDragPreviewLine(
        Vector3Int start,
        Vector3Int end)
    {
        Vector3 startWorld =
            VoxelGridUtility.GridToWorld(
                start
            );

        Vector3 endWorld =
            VoxelGridUtility.GridToWorld(
                end
            );

        Handles.color =
            new Color(
                0.3f,
                1f,
                0.3f,
                0.8f
            );

        Handles.DrawLine(
            startWorld,
            endWorld
        );

        bool canPlace =
            _voxelWorld != null &&
            !_voxelWorld.IsBelowMinimumHeight(
                end
            ) &&
            !_voxelWorld.HasBlock(
                end
            );

        Handles.color =
            canPlace
                ? new Color(
                    0.3f,
                    1f,
                    0.3f,
                    0.35f
                )
                : new Color(
                    1f,
                    0.2f,
                    0.2f,
                    0.35f
                );

        Handles.DrawWireCube(
            endWorld,
            Vector3.one *
            VoxelGridUtility.CellSize
        );

        Handles.color =
            Color.white;
    }

    // =========================================================
    // GUI
    // =========================================================

    private void DrawGUI()
    {
        Handles.BeginGUI();

        GUILayout.BeginArea(
            new Rect(
                10,
                10,
                260,
                230
            ),
            "Voxel Editor",
            GUI.skin.window
        );

        GUILayout.Label("Tool");

        GUILayout.BeginHorizontal();

        if (GUILayout.Toggle(
                _toolMode ==
                ToolMode.Pen,
                "Pen",
                GUI.skin.button))
        {
            _toolMode =
                ToolMode.Pen;
        }

        if (GUILayout.Toggle(
                _toolMode ==
                ToolMode.Eraser,
                "Eraser",
                GUI.skin.button))
        {
            _toolMode =
                ToolMode.Eraser;
        }

        GUILayout.EndHorizontal();

        GUILayout.Space(5);

        GameObject prefab =
            GetSelectedPrefab();

        if (prefab != null)
        {
            GUILayout.Label(
                $"Prefab : {prefab.name}"
            );
        }
        else
        {
            GUILayout.Label(
                "Prefab : None"
            );
        }

        GUILayout.Space(5);

        // -------------------------
        // ペンのドラッグ
        // -------------------------

        string penDragText =
            _penDragEnabled
                ? "ペンドラッグ : ON"
                : "ペンドラッグ : OFF";

        if (GUILayout.Button(
                penDragText))
        {
            _penDragEnabled =
                !_penDragEnabled;

            CancelDrag();
        }

        // -------------------------
        // 消しゴムのドラッグ
        // -------------------------

        string eraserDragText =
            _eraserDragEnabled
                ? "消しゴムドラッグ : ON"
                : "消しゴムドラッグ : OFF";

        if (GUILayout.Button(
                eraserDragText))
        {
            _eraserDragEnabled =
                !_eraserDragEnabled;

            CancelDrag();
        }

        GUILayout.Space(5);

        if (_toolMode ==
            ToolMode.Pen)
        {
            if (_hasHit)
            {
                GUILayout.Label(
                    "Placement Position"
                );

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
                GUILayout.Label(
                    "No Hit"
                );
            }
        }
        else
        {
            GUILayout.Label(
                "Eraser Mode"
            );
        }

        GUILayout.EndArea();

        Handles.EndGUI();
    }

    private void CancelDrag()
    {
        _isDragging = false;

        _hasDragStart = false;

        _hasDragPreview = false;

        _hasLastPlacedPosition = false;

        _dragDirection =
            Vector3Int.zero;

        SceneView.RepaintAll();
    }

    // =========================================================
    // 最低高度
    // =========================================================

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
            gridPosition =
                default;

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

    // =========================================================
    // ライン
    // =========================================================

    private IEnumerable<Vector3Int>
        GetLinePositions(
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

        for (int i = 1;
             i <= steps;
             i++)
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
    
    /// <summary>
    /// カーソルが最後に置いたブロック上にあるかを判定する
    /// </summary>
    /// <param name="currentEvent"></param>
    /// <returns> カーソルが最後に置いたブロック上にある = true </returns>
    private bool IsCursorOverLastPlacedBlock(
        Event currentEvent)
    {
        Ray ray =
            HandleUtility.GUIPointToWorldRay(
                currentEvent.mousePosition
            );

        if (!Physics.Raycast(
                ray,
                out RaycastHit hit))
        {
            return false;
        }

        GameObject hitObject =
            hit.collider.gameObject;

        GameObject rootObject =
            PrefabUtility.GetOutermostPrefabInstanceRoot(
                hitObject
            );

        if (rootObject == null)
        {
            rootObject = hitObject;
        }

        Vector3Int hitGridPosition =
            VoxelGridUtility.WorldToGrid(
                rootObject.transform.position
            );

        return hitGridPosition ==
               _lastPlacedPosition;
    }
    
    /// <summary>
    /// 次にブロックを置く場所を決める
    /// </summary>
    /// <param name="currentEvent"></param>
    /// <returns>ブロックを置く座標</returns>
    private Vector3Int GetNearestAdjacentPosition(
        Event currentEvent)
    {
        Vector3Int center =
            _lastPlacedPosition;

        Vector3Int[] candidates =
        {
            center + Vector3Int.up,
            center + Vector3Int.down,
            center + Vector3Int.left,
            center + Vector3Int.right,
            center + Vector3Int.forward,
            center + Vector3Int.back
        };

        Vector2 mousePosition =
            currentEvent.mousePosition;

        SceneView sceneView =
            SceneView.lastActiveSceneView;

        if (sceneView == null ||
            sceneView.camera == null)
        {
            return candidates[0];
        }

        Camera camera =
            sceneView.camera;

        Vector3Int bestPosition =
            candidates[0];

        float bestDistance =
            float.MaxValue;

        int bestPriority =
            int.MaxValue;

        for (int i = 0;
             i < candidates.Length;
             i++)
        {
            Vector3 worldPosition =
                VoxelGridUtility.GridToWorld(
                    candidates[i]
                );

            Vector2 screenPosition =
                HandleUtility.WorldToGUIPoint(
                    worldPosition
                );

            float distance =
                (screenPosition -
                 mousePosition).sqrMagnitude;

            int priority =
                GetAdjacentPriority(
                    candidates[i] -
                    center,
                    camera
                );

            if (distance < bestDistance - 0.01f)
            {
                bestDistance =
                    distance;

                bestPosition =
                    candidates[i];

                bestPriority =
                    priority;

                continue;
            }

            // 距離がほぼ同じ場合は
            // 優先順位を使用する
            if (Mathf.Abs(
                    distance -
                    bestDistance) <= 0.01f &&
                priority < bestPriority)
            {
                bestPosition =
                    candidates[i];

                bestPriority =
                    priority;
            }
        }

        return bestPosition;
    }
    
    /// <summary>
    /// ブロックを置く方向の優先順位
    /// </summary>
    /// <param name="direction"></param>
    /// <param name="camera">現在のカメラ</param>
    /// <returns></returns>
    private int GetAdjacentPriority(
        Vector3Int direction,
        Camera camera)
    {
        Vector3 cameraForward =
            camera.transform.forward;

        Vector3 cameraPosition =
            camera.transform.position;

        Vector3 centerWorld =
            VoxelGridUtility.GridToWorld(
                _lastPlacedPosition
            );

        Vector3 candidateWorld =
            centerWorld +
            new Vector3(
                direction.x,
                direction.y,
                direction.z
            ) *
            VoxelGridUtility.CellSize;

        Vector3 toCandidate =
            candidateWorld -
            centerWorld;

        // カメラに近い方向を「手前」とする
        float depth =
            Vector3.Dot(
                toCandidate,
                cameraForward
            );

        // カメラ方向に近いものほど優先
        if (depth < -0.1f)
        {
            return 0;
        }

        // 上
        if (direction == Vector3Int.up)
        {
            return 1;
        }

        // 左右
        if (direction == Vector3Int.left ||
            direction == Vector3Int.right)
        {
            return 2;
        }

        // 下
        if (direction == Vector3Int.down)
        {
            return 3;
        }

        // 奥
        return 4;
    }
}