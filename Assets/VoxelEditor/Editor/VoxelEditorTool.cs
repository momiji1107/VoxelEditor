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

    private Vector3Int _gridPosition;
    private bool _hasHit;

    // -------------------------
    // ドラッグ設定
    // -------------------------

    [SerializeField] private bool _penDragEnabled = true;
    [SerializeField] private bool _eraserDragEnabled = true;
    
    private Vector3Int _dragPreviewPosition;
    private bool _hasDragPreview;

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

    private ToolMode _toolMode = ToolMode.Pen;
    private Vector2 _prefabScrollPosition;
    private int _prefabListHash;

    private GameObject _eraseTarget;
    private Vector3Int _lastErasedPosition;
    private bool _hasLastErasedPosition;
    
    // -------------------------
    // プレファブ
    // -------------------------
    private VoxelPrefabDatabase _prefabDatabase;
    private GameObject _selectedPrefab;
    private readonly Dictionary<GameObject, Texture2D> _prefabPreviews = new();
    
    private const float PrefabAreaHeight = 220f;
    private const float PrefabItemHeight = 80f;
    private const float PrefabItemSpacing = 5f;
    private const float PrefabPreviewPadding = 5f;
    private const float PrefabLabelHeight = 15f;
    private const int PrefabColumns = 2;
    
    /// <summary>
    /// ツールバーの表示
    /// </summary>
    public override GUIContent toolbarIcon
    {
        get
        {
            return new GUIContent("V");
        }
    }
    
    private void OnEnable()
    {
        LoadPrefabDatabase();
        SceneView.RepaintAll();
    }

    public override void OnToolGUI(
        EditorWindow window)
    {
        _voxelWorld = FindFirstObjectByType<VoxelWorld>();
        LoadPrefabDatabase();

        Event currentEvent = Event.current;

        // -------------------------
        // 通常時のカーソル判定
        // -------------------------

        if (_toolMode == ToolMode.Pen)
        {
            UpdatePlacementPosition(currentEvent);
        }
        else
        {
            UpdateEraseTarget(currentEvent);
        }

        // -------------------------
        // ドラッグ中の処理
        // -------------------------

        if (currentEvent.type == EventType.MouseDrag && 
            currentEvent.button == 0 &&
            _isDragging &&
            !currentEvent.alt)
        {
            if (_toolMode == ToolMode.Pen)
            {
                if (_penDragEnabled)
                {
                    PlaceDraggedBlocks(currentEvent);
                }
            }
            else
            {
                if (_eraserDragEnabled)
                {
                    EraseDraggedBlocks(currentEvent);
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

        if (currentEvent.type == EventType.MouseDown &&
            currentEvent.button == 0 &&
            !currentEvent.alt)
        {
            StartMouseDrag(currentEvent);
            currentEvent.Use();
        }

        // -------------------------
        // ドラッグ終了
        // -------------------------

        if (currentEvent.type == EventType.MouseUp &&
            currentEvent.button == 0)
        {
            EndMouseDrag();
            currentEvent.Use();
        }

        // -------------------------
        // カーソル移動
        // -------------------------

        if (currentEvent.type == EventType.MouseMove)
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

        _dragDirection = Vector3Int.zero;

        if (_toolMode == ToolMode.Pen)
        {
            if (!_hasHit)
            {
                _isDragging = false;
                return;
            }

            if (_penDragEnabled)
            {
                _isDragging = true;
                StartPenDrag(currentEvent);
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
            StartEraserDrag();
        }
        else
        {
            _isDragging = false;
            EraseBlock();
        }
    }

    private void EndMouseDrag()
    {
        _isDragging = false;
        _hasLastPlacedPosition = false;
        _hasLastErasedPosition = false;
        _hasDragStart = false;
        _hasDragPreview = false;

        _dragDirection = Vector3Int.zero;

        SceneView.RepaintAll();
    }

    // =========================================================
    // 配置位置
    // =========================================================

    /// <summary>
    /// カーソルの指す対象から配置位置を取得します。
    /// </summary>
    private void UpdatePlacementPosition(Event currentEvent)
    {
        // ドラッグ中のペンでは、
        // ブロックの面ではなくカーソル移動方向を使うため、
        // 通常の配置位置更新を行います。
        Ray ray = HandleUtility.GUIPointToWorldRay(currentEvent.mousePosition);

        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            Vector3 placementPosition =
                hit.point +
                hit.normal *
                (VoxelGridUtility.CellSize * 0.5f);

            _gridPosition = VoxelGridUtility.WorldToGrid(placementPosition);

            _hasHit = true;

            return;
        }

        if (_voxelWorld != null &&
            TryGetMinimumHeightPosition(ray, out Vector3Int minimumPosition))
        {
            _gridPosition = minimumPosition;

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

        Ray ray = HandleUtility.GUIPointToWorldRay(currentEvent.mousePosition);

        if (!Physics.Raycast(ray, out RaycastHit hit))
        {
            return;
        }

        GameObject hitObject = hit.collider.gameObject;

        GameObject rootObject = PrefabUtility.GetOutermostPrefabInstanceRoot(hitObject);

        _eraseTarget = rootObject != null 
                ? rootObject
                : hitObject;
    }

    // =========================================================
    // ペン
    // =========================================================

    private void StartPenDrag(
        Event currentEvent)
    {
        _dragStartPosition = _gridPosition;
        _lastPlacedPosition = _gridPosition;
        _dragPreviewPosition = _gridPosition;
        _dragDirection = Vector3Int.zero;
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
    private void PlaceDraggedBlocks(Event currentEvent)
    {
        if (!_hasDragStart || !_hasLastPlacedPosition)
        {
            return;
        }

        // 最後に置いたブロックを基準に、
        // カーソルがまだそのブロック上にあるか確認する
        if (IsCursorOverLastPlacedBlock(currentEvent))
        {
            return;
        }

        // カーソルが最後に置いたブロックから外れたので、
        // 次に置く位置を決定する
        Vector3Int nextPosition = GetNearestAdjacentPosition(currentEvent);

        if (_voxelWorld == null)
        {
            return;
        }

        // 最低高度より下なら配置しない
        if (_voxelWorld.IsBelowMinimumHeight(nextPosition))
        {
            _dragPreviewPosition = nextPosition;

            _hasDragPreview = true;

            SceneView.RepaintAll();

            return;
        }

        // すでにブロックがある場合は配置しない
        if (_voxelWorld.HasBlock(nextPosition))
        {
            _dragPreviewPosition = nextPosition;
            _hasDragPreview = true;

            SceneView.RepaintAll();

            return;
        }

        // 配置位置を更新
        _gridPosition = nextPosition;

        PlaceBlock();

        // 今配置したブロックを
        // 次の基準ブロックにする
        _lastPlacedPosition = nextPosition;
        _dragPreviewPosition = nextPosition;

        _hasDragPreview = true;

        SceneView.RepaintAll();
    }
    
    // =========================================================
    // 消しゴム
    // =========================================================
    
    private void StartEraserDrag()
    {
        if (_eraseTarget == null)
        {
            _isDragging = false;

            return;
        }

        if (_voxelWorld == null)
        {
            _isDragging = false;

            return;
        }

        Vector3Int gridPosition = VoxelGridUtility.WorldToGrid(_eraseTarget.transform.position);

        _lastErasedPosition = gridPosition;
        _hasLastErasedPosition = true;

        EraseBlock();

        SceneView.RepaintAll();
    }
    
    private void EraseDraggedBlocks(Event currentEvent)
    {
        if (!_hasLastErasedPosition)
        {
            return;
        }

        if (_eraseTarget == null)
        {
            return;
        }

        Vector3Int currentPosition = VoxelGridUtility.WorldToGrid(_eraseTarget.transform.position);

        // まだ同じブロック上にいる
        if (currentPosition == _lastErasedPosition)
        {
            return;
        }

        // 新しくカーソルが乗ったブロックを削除
        EraseBlock();

        _lastErasedPosition = currentPosition;

        SceneView.RepaintAll();
    }

    /// <summary>
    /// マウスの移動方向から
    /// ブロックの配置方向を決定します。
    /// </summary>
    private Vector3Int GetMouseDragDirection(Vector2 mouseDelta)
    {
        float absX = Mathf.Abs(mouseDelta.x);
        float absY = Mathf.Abs(mouseDelta.y);

        // --------------------------------
        // 上方向を優先
        // --------------------------------

        if (absY >= absX)
        {
            if (mouseDelta.y < 0f)
            {
                // SceneViewでは上方向へマウスを動かした場合、Yを上げる。
                return Vector3Int.up;
            }

            return Vector3Int.down;
        }

        // --------------------------------
        // 横方向
        // --------------------------------

        SceneView sceneView = SceneView.lastActiveSceneView;

        if (sceneView == null)
        {
            return mouseDelta.x > 0f
                ? Vector3Int.right
                : Vector3Int.left;
        }

        Camera sceneCamera = sceneView.camera;

        if (sceneCamera == null)
        {
            return mouseDelta.x > 0f
                ? Vector3Int.right
                : Vector3Int.left;
        }

        Vector3 cameraRight = sceneCamera.transform.right;

        cameraRight.y = 0f;

        if (cameraRight.sqrMagnitude < 0.0001f)
        {
            return mouseDelta.x > 0f
                ? Vector3Int.right
                : Vector3Int.left;
        }

        cameraRight.Normalize();

        // カメラ右方向に近いワールド軸を選択
        if (Mathf.Abs(cameraRight.x) >= Mathf.Abs(cameraRight.z))
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

        if (_voxelWorld.HasBlock(_gridPosition))
        {
            return;
        }

        Vector3 worldPosition = VoxelGridUtility.GridToWorld(_gridPosition);

        GameObject block = (GameObject)PrefabUtility.InstantiatePrefab(prefab);

        block.transform.position = worldPosition;

        Undo.RegisterCreatedObjectUndo(
            block,
            "Place Voxel Block"
        );

        Undo.RecordObject(
            _voxelWorld,
            "Add Voxel Block"
        );

        _voxelWorld.AddBlock(_gridPosition, prefab, block.transform.rotation);

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

        Vector3Int gridPosition = VoxelGridUtility.WorldToGrid(_eraseTarget.transform.position);

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

        _voxelWorld.RemoveBlock(gridPosition);

        Undo.DestroyObjectImmediate(_eraseTarget);
    }

    // =========================================================
    // 選択中のPrefabを取得
    // =========================================================

    private GameObject GetSelectedPrefab()
    {
        return _selectedPrefab;
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

        Vector3Int previewPosition = _gridPosition;

        if (_isDragging &&
            _hasDragStart &&
            _hasDragPreview &&
            _dragDirection !=
            Vector3Int.zero)
        {
            previewPosition = _dragPreviewPosition;
        }

        bool canPlace =
            _voxelWorld != null &&
            !_voxelWorld.IsBelowMinimumHeight(previewPosition) &&
            !_voxelWorld.HasBlock(previewPosition);

        Handles.color = canPlace
                ? Color.green
                : Color.red;

        Vector3 worldPosition = VoxelGridUtility.GridToWorld(previewPosition);

        Handles.DrawWireCube(worldPosition, Vector3.one * VoxelGridUtility.CellSize);

        if (_isDragging &&
            _hasDragStart &&
            _hasDragPreview &&
            _dragDirection !=
            Vector3Int.zero)
        {
            DrawDragPreviewLine(_dragStartPosition, previewPosition);
        }

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

        Renderer[] renderers = _eraseTarget.GetComponentsInChildren<Renderer>();

        if (renderers.Length == 0)
        {
            bounds = new Bounds(_eraseTarget.transform.position, Vector3.one * VoxelGridUtility.CellSize);
        }
        else
        {
            bounds = renderers[0].bounds;

            for (int i = 1; i < renderers.Length; i++)
            {
                bounds.Encapsulate(renderers[i].bounds);
            }
        }

        Handles.color = Color.red;

        Handles.DrawWireCube(bounds.center, bounds.size);

        Handles.color = Color.white;
    }

    private void DrawDragPreviewLine(Vector3Int start, Vector3Int end)
    {
        Vector3 startWorld = VoxelGridUtility.GridToWorld(start);

        Vector3 endWorld = VoxelGridUtility.GridToWorld(end);

        Handles.color = new Color(0.3f, 1f, 0.3f, 0.8f);

        Handles.DrawLine(startWorld, endWorld);

        bool canPlace = _voxelWorld != null &&
                        !_voxelWorld.IsBelowMinimumHeight(end) &&
                        !_voxelWorld.HasBlock(end);

        Handles.color = canPlace
                ? new Color(0.3f, 1f, 0.3f, 0.35f)
                : new Color(1f, 0.2f, 0.2f, 0.35f);

        Handles.DrawWireCube(endWorld, Vector3.one * VoxelGridUtility.CellSize);

        Handles.color = Color.white;
    }

    // =========================================================
    // GUI
    // =========================================================

    private void DrawGUI()
    {
        Handles.BeginGUI();

        GUILayout.BeginArea(
            new Rect(10, 10, 200, 400),
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

        // -------------------------
        // ペンのドラッグ
        // -------------------------

        if (_toolMode == ToolMode.Pen)
        {
            string penDragText = _penDragEnabled 
                                ? "ペンドラッグ : ON"
                                : "ペンドラッグ : OFF";

            if (GUILayout.Button(penDragText))
            {
                _penDragEnabled = !_penDragEnabled;
                CancelDrag();
            }
        }

        // -------------------------
        // 消しゴムのドラッグ
        // -------------------------

        if (_toolMode == ToolMode.Eraser)
        {
            string eraserDragText = _eraserDragEnabled
                                    ? "消しゴムドラッグ : ON"
                                    : "消しゴムドラッグ : OFF";

            if (GUILayout.Button(eraserDragText))
            {
                _eraserDragEnabled = !_eraserDragEnabled;
                CancelDrag();
            }
        }
        
        GUILayout.Space(5);

        if (_toolMode == ToolMode.Pen)
        {
            if (_hasHit)
            {
                GUILayout.Label(
                    "Placement Position"
                );

                GUILayout.Label(
                    $"(X,Y,Z) = ({_gridPosition.x}, {_gridPosition.y}, {_gridPosition.z})"
                );
            }
            else
            {
                GUILayout.Label(
                    "No Hit"
                );
            }
        }
        else if(_toolMode == ToolMode.Eraser)
        {
            GUILayout.Label(
                "Eraser Mode"
            );
        }
        
        //Prefabの選択肢
        if (_toolMode == ToolMode.Pen)
        {
            DrawPrefabSelector();
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
        _dragDirection = Vector3Int.zero;

        SceneView.RepaintAll();
    }

    // =========================================================
    // 最低高度
    // =========================================================

    private bool TryGetMinimumHeightPosition(Ray ray, out Vector3Int gridPosition)
    {
        float minimumHeight = _voxelWorld.MinimumHeight;

        Plane minimumHeightPlane = new Plane(Vector3.up, new Vector3(0f, minimumHeight, 0f));

        if (!minimumHeightPlane.Raycast(ray, out float distance) || distance < 0f)
        {
            gridPosition = default;
            return false;
        }

        Vector3 hitPosition = ray.GetPoint(distance);

        gridPosition = VoxelGridUtility.WorldToGrid(hitPosition);
        gridPosition.y = Mathf.RoundToInt(minimumHeight);

        return true;
    }

    // =========================================================
    // ライン
    // =========================================================

    private IEnumerable<Vector3Int> GetLinePositions(Vector3Int start, Vector3Int end)
    {
        int deltaX = end.x - start.x;
        int deltaY = end.y - start.y;
        int deltaZ = end.z - start.z;

        int steps = Mathf.Max(Mathf.Abs(deltaX), Mathf.Abs(deltaY), Mathf.Abs(deltaZ));

        if (steps == 0)
        {
            yield return start;
            yield break;
        }

        for (int i = 1; i <= steps; i++)
        {
            float t = (float)i / steps;

            int x = Mathf.RoundToInt(Mathf.Lerp(start.x, end.x, t));
            int y = Mathf.RoundToInt(Mathf.Lerp(start.y, end.y, t));
            int z = Mathf.RoundToInt(Mathf.Lerp(start.z, end.z, t));

            yield return new Vector3Int(x, y, z);
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
        Ray ray = HandleUtility.GUIPointToWorldRay(currentEvent.mousePosition);

        if (!Physics.Raycast(ray, out RaycastHit hit))
        {
            return false;
        }

        GameObject hitObject = hit.collider.gameObject;

        GameObject rootObject = PrefabUtility.GetOutermostPrefabInstanceRoot(hitObject);

        if (rootObject == null)
        {
            rootObject = hitObject;
        }

        Vector3Int hitGridPosition = VoxelGridUtility.WorldToGrid(rootObject.transform.position);

        return hitGridPosition == _lastPlacedPosition;
    }
    
    /// <summary>
    /// 次にブロックを置く場所を決める
    /// </summary>
    /// <param name="currentEvent"></param>
    /// <returns>ブロックを置く座標</returns>
    private Vector3Int GetNearestAdjacentPosition(Event currentEvent)
    {
        Vector3Int center = _lastPlacedPosition;

        Vector3Int[] candidates =
        {
            center + Vector3Int.up,
            center + Vector3Int.down,
            center + Vector3Int.left,
            center + Vector3Int.right,
            center + Vector3Int.forward,
            center + Vector3Int.back
        };

        Vector2 mousePosition = currentEvent.mousePosition;

        SceneView sceneView = SceneView.lastActiveSceneView;

        if (sceneView == null || sceneView.camera == null)
        {
            return candidates[0];
        }

        Camera camera = sceneView.camera;

        Vector3Int bestPosition = candidates[0];

        float bestDistance = float.MaxValue;

        int bestPriority = int.MaxValue;

        for (int i = 0; i < candidates.Length; i++)
        {
            Vector3 worldPosition = VoxelGridUtility.GridToWorld(candidates[i]);
            Vector2 screenPosition = HandleUtility.WorldToGUIPoint(worldPosition);
            float distance = (screenPosition - mousePosition).sqrMagnitude;

            int priority = GetAdjacentPriority(candidates[i] - center, camera);

            if (distance < bestDistance - 0.01f)
            {
                bestDistance = distance;
                bestPosition = candidates[i];
                bestPriority = priority;

                continue;
            }

            // 距離がほぼ同じ場合は
            // 優先順位を使用する
            if (Mathf.Abs(distance - bestDistance) <= 0.01f &&
                priority < bestPriority)
            {
                bestPosition = candidates[i];
                bestPriority = priority;
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
    private int GetAdjacentPriority(Vector3Int direction, Camera camera)
    {
        Vector3 cameraForward = camera.transform.forward;
        Vector3 cameraPosition = camera.transform.position;
        Vector3 centerWorld = VoxelGridUtility.GridToWorld(_lastPlacedPosition);

        Vector3 candidateWorld = centerWorld + new Vector3(direction.x, direction.y, direction.z) * VoxelGridUtility.CellSize;

        Vector3 toCandidate = candidateWorld - centerWorld;

        // カメラに近い方向を「手前」とする
        float depth = Vector3.Dot(toCandidate, cameraForward);

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
        if (direction == Vector3Int.left || direction == Vector3Int.right)
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
    
    /// <summary>
    /// Prefab Databaseを取得する
    /// </summary>
    private void LoadPrefabDatabase()
    {
        string[] guids = AssetDatabase.FindAssets("t:VoxelPrefabDatabase");

        if (guids.Length == 0)
        {
            _prefabDatabase = null;
            _prefabPreviews.Clear();
            _prefabListHash = 0;
            return;
        }

        string path = AssetDatabase.GUIDToAssetPath(guids[0]);
        VoxelPrefabDatabase database = AssetDatabase.LoadAssetAtPath<VoxelPrefabDatabase>(path);

        if (_prefabDatabase != database)
        {
            _prefabDatabase = database;
            _prefabPreviews.Clear();
            _prefabListHash = 0;
        }

        if (_prefabDatabase == null)
        {
            return;
        }

        int listHash = 17;

        foreach (GameObject prefab in _prefabDatabase.Prefabs)
        {
            listHash = listHash * 31 + (prefab != null ? prefab.GetInstanceID() : 0);
        }

        if (_prefabListHash != listHash)
        {
            _prefabListHash = listHash;
            _prefabPreviews.Clear();
            SceneView.RepaintAll();
        }
    }
    
    /// <summary>
    /// Prefabのプレビュー画像を取得
    /// </summary>
    /// <param name="prefab"></param>
    /// <returns></returns>
    private Texture2D GetPrefabPreview(GameObject prefab)
    {
        if (prefab == null)
        {
            return null;
        }

        if (_prefabPreviews.TryGetValue(prefab, out Texture2D cachedPreview) && cachedPreview != null)
        {
            return cachedPreview;
        }

        Texture2D preview = AssetPreview.GetAssetPreview(prefab);

        if (preview != null)
        {
            _prefabPreviews[prefab] = preview;
            return preview;
        }

        if (AssetPreview.IsLoadingAssetPreview(prefab.GetInstanceID()))
        {
            EditorApplication.QueuePlayerLoopUpdate();
            SceneView.RepaintAll();
        }

        return AssetPreview.GetMiniThumbnail(prefab) as Texture2D;
    }

    /// <summary>
    /// Prefabの選択肢を表示する
    /// </summary>
    private void DrawPrefabSelector()
    {
        GUILayout.Label("Prefab", EditorStyles.boldLabel);

        GUILayout.Label(
            _selectedPrefab != null
                ? $"選択中 : {_selectedPrefab.name}"
                : "選択中 : None"
        );

        if (_prefabDatabase == null)
        {
            EditorGUILayout.HelpBox(
                "VoxelPrefabDatabase が見つかりません。",
                MessageType.Warning
            );
            return;
        }

        if (_prefabDatabase.Prefabs.Count == 0)
        {
            EditorGUILayout.HelpBox(
                "Prefabが登録されていません。",
                MessageType.Info
            );
            return;
        }

        Rect areaRect = GUILayoutUtility.GetRect(
            GUIContent.none,
            GUIStyle.none,
            GUILayout.Height(PrefabAreaHeight)
        );

        GUI.Box(areaRect, GUIContent.none, GUI.skin.box);

        Rect scrollRect = new Rect(
            areaRect.x + 2f,
            areaRect.y + 2f,
            areaRect.width - 4f,
            areaRect.height - 4f
        );

        float contentHeight = GetPrefabContentHeight();

        Rect contentRect = new Rect(
            0f,
            0f,
            scrollRect.width - GUI.skin.verticalScrollbar.fixedWidth,
            contentHeight
        );

        _prefabScrollPosition = GUI.BeginScrollView(
            scrollRect,
            _prefabScrollPosition,
            contentRect,
            false,
            true
        );

        float itemWidth = GetPrefabItemWidth(contentRect.width);

        for (int i = 0; i < _prefabDatabase.Prefabs.Count; i++)
        {
            int column = i % PrefabColumns;
            int row = i / PrefabColumns;

            float x = column * (itemWidth + PrefabItemSpacing);
            float y = row * (PrefabItemHeight + PrefabItemSpacing);

            Rect itemRect = new Rect(
                x,
                y,
                itemWidth,
                PrefabItemHeight
            );

            DrawPrefabButton(
                _prefabDatabase.Prefabs[i],
                itemRect
            );
        }

        GUI.EndScrollView();
    }

    private float GetPrefabItemWidth(float availableWidth)
    {
        return (availableWidth - PrefabItemSpacing) / PrefabColumns;
    }

    private float GetPrefabContentHeight()
    {
        int prefabCount = _prefabDatabase.Prefabs.Count;
        int rows = Mathf.CeilToInt((float)prefabCount / PrefabColumns);

        return rows * PrefabItemHeight + Mathf.Max(0, rows - 1) * PrefabItemSpacing;
    }
    
    private void DrawPrefabButton(
        GameObject prefab,
        Rect buttonRect)
    {
        if (prefab == null)
        {
            return;
        }

        Texture2D preview = GetPrefabPreview(prefab);
        bool selected = _selectedPrefab == prefab;

        if (GUI.Button(
                buttonRect,
                GUIContent.none,
                GUI.skin.button))
        {
            _selectedPrefab = prefab;
            SceneView.RepaintAll();
        }

        if (preview != null)
        {
            Rect previewRect = new Rect(
                buttonRect.x + PrefabPreviewPadding,
                buttonRect.y + PrefabPreviewPadding,
                buttonRect.width - PrefabPreviewPadding * 2f,
                buttonRect.height - PrefabLabelHeight - PrefabPreviewPadding * 2f
            );

            GUI.DrawTexture(
                previewRect,
                preview,
                ScaleMode.ScaleToFit,
                true
            );
        }

        Rect labelRect = new Rect(
            buttonRect.x + 3f,
            buttonRect.y + buttonRect.height - PrefabLabelHeight - 2f,
            buttonRect.width - 6f,
            PrefabLabelHeight
        );

        GUI.Label(
            labelRect,
            prefab.name,
            EditorStyles.centeredGreyMiniLabel
        );

        if (selected)
        {
            Color previousColor = Handles.color;
            Handles.color = Color.yellow;

            Handles.DrawAAPolyLine(
                3f,
                new Vector3(buttonRect.x, buttonRect.y),
                new Vector3(buttonRect.xMax, buttonRect.y),
                new Vector3(buttonRect.xMax, buttonRect.yMax),
                new Vector3(buttonRect.x, buttonRect.yMax),
                new Vector3(buttonRect.x, buttonRect.y)
            );

            Handles.color = previousColor;
        }
    }
}