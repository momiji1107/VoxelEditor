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

    // =========================================================
    // Drag Settings
    // ドラッグ設定
    // =========================================================

    [SerializeField] private bool _penDragEnabled = true;
    [SerializeField] private bool _eraserDragEnabled = true;
    private Vector3Int _dragPreviewPosition;
    private bool _hasDragPreview;

    // =========================================================
    // Drag State
    // ドラッグ状態
    // =========================================================

    private bool _hasDragStart;
    private bool _isDragging;
    private bool _hasLastPlacedPosition;
    private Vector3Int _dragStartPosition;
    private Vector3Int _lastPlacedPosition;
    private Vector3Int _dragDirection;

    // =========================================================
    // Tool Settings
    // ツール設定
    // =========================================================

    private ToolMode _toolMode = ToolMode.Pen;
    private Vector2 _prefabScrollPosition;
    private int _prefabListHash;
    private GameObject _eraseTarget;
    private Vector3Int _lastErasedPosition;
    private bool _hasLastErasedPosition;

    private const float GuiWidth = 200f;
    private const float GuiMargin = 10f;

    // =========================================================
    // Prefab Rotation
    // Prefab回転
    // =========================================================

    [SerializeField] private bool _showRotationSettings;
    private Vector3 _prefabRotation = Vector3.zero;
    private const float RotationButtonHeight = 24f;

    // =========================================================
    // Grid Settings
    // グリッド設定
    // =========================================================

    [SerializeField] private bool _gridVisible = true;
    private const float GridSizeButtonWidth = 25f;
    private const float GridLineWidth = 10f;
    private const int GridSizeMin = 10;
    private const int GridSizeMax = 100;
    private const int GridSizeStep = 10;
    private int _gridSize = 40;
    private static readonly Color GridColor = new(0.5f, 0.5f, 0.5f, 0.35f);

    // =========================================================
    // Prefab List
    // Prefab一覧
    // =========================================================

    private List<VoxelPrefabDatabase> _prefabDatabases = new();
    private VoxelPrefabDatabase _prefabDatabase;
    private int _selectedDatabaseIndex;
    private GameObject _selectedPrefab;
    private readonly Dictionary<GameObject, Texture2D> _prefabPreviews = new();
    private GUIStyle _prefabLabelStyle;

    private const float PrefabItemHeight = 80f;
    private const float PrefabItemSpacing = 5f;
    private const float PrefabPreviewPadding = 5f;
    private const float PrefabLabelHeight = 15f;
    private const float PrefabSelectionBorderWidth = 3f;
    private const float DatabaseSelectorHeight = 22f;
    private const int PrefabColumns = 2;

    public override GUIContent toolbarIcon
    {
        get { return new GUIContent("V"); }
    }

    // =========================================================
    // Initialization
    // 初期化
    // =========================================================

    private void OnEnable()
    {
        LoadPrefabDatabase();
        SceneView.RepaintAll();
    }

    public override void OnToolGUI(EditorWindow window)
    {
        _voxelWorld = FindFirstObjectByType<VoxelWorld>();
        LoadPrefabDatabase();

        Event currentEvent = Event.current;

        // =========================================================
        // Cursor Detection
        // カーソル判定
        // =========================================================

        // -------------------------
        // Detect placement or erase target
        // 配置位置または消去対象を判定
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
        // Repaint while moving the cursor
        // カーソル移動中に再描画
        // -------------------------

        if (currentEvent.type == EventType.MouseMove)
        {
            SceneView.RepaintAll();
        }

        // =========================================================
        // Draw grid and operation previews
        // グリッドと操作プレビューを描画
        // =========================================================

        DrawGrid();
        DrawPlacementPreview();
        DrawErasePreview();
        DrawGUI();

        // =========================================================
        // Drag Processing
        // ドラッグ処理
        // =========================================================

        // -------------------------
        // Process while dragging
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
        // Start dragging
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
        // End dragging
        // ドラッグ終了
        // -------------------------

        if (currentEvent.type == EventType.MouseUp &&
            currentEvent.button == 0)
        {
            EndMouseDrag();
            currentEvent.Use();
        }

    }

    // =========================================================
    // Pen Operation
    // ペン操作
    // =========================================================

    /// <summary>
    /// Gets the grid position where the block will be placed from the object pointed to by the cursor.
    /// カーソルが指している対象からブロックを配置するグリッド位置を取得します。
    /// </summary>
    /// <param name="currentEvent">Current mouse event : 現在のマウスイベント</param>
    private void UpdatePlacementPosition(Event currentEvent)
    {
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

    // -------------------------
    // Place block
    // ブロック配置
    // -------------------------

    private void PlaceBlock()
    {
        if (_voxelWorld == null)
        {
            return;
        }

        GameObject prefab = _selectedPrefab;

        if (prefab == null)
        {
            Debug.LogWarning("Voxel Editor: Please select a Prefab. / Voxel Editor: Prefabを選択してください。");
            return;
        }

        if (_voxelWorld.IsBelowMinimumHeight(_gridPosition))
        {
            Debug.LogWarning(
                $"Voxel Editor: It cannot be placed below the minimum altitude ({_voxelWorld.MinimumHeight}). / Voxel Editor: 最低高度(Minimum Height) ({_voxelWorld.MinimumHeight}) より下には配置できません。");
            return;
        }

        if (_voxelWorld.HasBlock(_gridPosition))
        {
            return;
        }

        Vector3 worldPosition = VoxelGridUtility.GridToWorld(_gridPosition);

        GameObject block = (GameObject)PrefabUtility.InstantiatePrefab(prefab);

        block.transform.SetParent(_voxelWorld.transform);
        block.transform.position = worldPosition;
        block.transform.rotation = Quaternion.Euler(_prefabRotation);

        Undo.RegisterCreatedObjectUndo(block, "Place Voxel Block");

        Undo.RecordObject(_voxelWorld, "Add Voxel Block");

        _voxelWorld.AddBlock(_gridPosition, prefab, block.transform.rotation);
    }

    /// <summary>
    /// Gets the grid position where the ray intersects the minimum height plane.
    /// レイと最低高度の平面が交差するグリッド位置を取得します。
    /// </summary>
    /// <param name="ray">Ray from the Scene View camera : シーンビューカメラからのレイ</param>
    /// <param name="gridPosition">Grid position at the minimum height : 最低高度におけるグリッド位置</param>
    /// <returns>True if the ray intersects the minimum height plane : 最低高度の平面とレイが交差した場合はtrue</returns>
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

    /// <summary>
    /// Determines whether the cursor is currently over the last placed block.
    /// カーソルが現在、最後に配置したブロック上にあるかを判定します。
    /// </summary>
    /// <param name="currentEvent">Current mouse event : 現在のマウスイベント</param>
    /// <returns>True if the cursor is over the last placed block : カーソルが最後に配置したブロック上にある場合はtrue</returns>
    private bool IsCursorOverLastPlacedBlock(Event currentEvent)
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

    // =========================================================
    // Eraser Operation
    // 消しゴム操作
    // =========================================================

    /// <summary>
    /// Gets the GameObject targeted by the eraser.
    /// 消しゴムの対象となっているGameObjectを取得します。
    /// </summary>
    /// <param name="currentEvent">Current mouse event : 現在のマウスイベント</param>
    private void UpdateEraseTarget(Event currentEvent)
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

    // -------------------------
    // Erase block
    // ブロック消去
    // -------------------------

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
    // Drag Operation
    // ドラッグ操作
    // =========================================================

    // -------------------------
    // Start mouse drag
    // マウスドラッグ開始
    // -------------------------

    private void StartMouseDrag(Event currentEvent)
    {
        _hasLastPlacedPosition = false;
        _hasDragStart = false;
        _dragDirection = Vector3Int.zero;

        // -------------------------
        // Pen mode
        // ペンモード
        // -------------------------

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
        // Eraser mode
        // 消しゴムモード
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

    // =========================================================
    // Pen Drag
    // ペンドラッグ
    // =========================================================

    // -------------------------
    // Start pen drag
    // ペンドラッグ開始
    // -------------------------

    private void StartPenDrag(Event currentEvent)
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
    /// Places blocks while dragging by using the movement direction from the previous cursor position to the current cursor position.
    /// 前回のカーソル位置から現在のカーソル位置への移動方向を使用して、ドラッグ中にブロックを配置します。
    /// </summary>
    /// <param name="currentEvent">Current mouse event : 現在のマウスイベント</param>
    private void PlaceDraggedBlocks(Event currentEvent)
    {
        if (!_hasDragStart || !_hasLastPlacedPosition)
        {
            return;
        }

        // -------------------------
        // Check the cursor position
        // カーソル位置を確認
        // -------------------------

        if (IsCursorOverLastPlacedBlock(currentEvent))
        {
            return;
        }

        // -------------------------
        // Determine the next position
        // 次の配置位置を決定
        // -------------------------

        Vector3Int nextPosition = GetNearestAdjacentPosition(currentEvent);

        if (_voxelWorld == null)
        {
            return;
        }

        // 最低高度より下には配置しない
        // Do not place below the minimum height
        if (_voxelWorld.IsBelowMinimumHeight(nextPosition))
        {
            _dragPreviewPosition = nextPosition;
            _hasDragPreview = true;
            SceneView.RepaintAll();
            return;
        }

        // 既存のブロックがある位置には配置しない
        // Do not place where a block already exists
        if (_voxelWorld.HasBlock(nextPosition))
        {
            _dragPreviewPosition = nextPosition;
            _hasDragPreview = true;
            SceneView.RepaintAll();
            return;
        }

        // -------------------------
        // Place the next block
        // 次のブロックを配置
        // -------------------------

        _gridPosition = nextPosition;
        PlaceBlock();

        _lastPlacedPosition = nextPosition;
        _dragPreviewPosition = nextPosition;
        _hasDragPreview = true;

        SceneView.RepaintAll();
    }

    // -------------------------
    // Adjacent Block Selection
    // 隣接ブロック選択
    // -------------------------

    /// <summary>
    /// Determines the nearest adjacent position to the cursor.
    /// カーソルに最も近い隣接位置を決定します。
    /// </summary>
    /// <param name="currentEvent">Current mouse event : 現在のマウスイベント</param>
    /// <returns>Nearest adjacent grid position : 最も近い隣接グリッド位置</returns>
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

            // 距離がほぼ同じ場合は優先順位を使用する
            // Use the priority when the distances are nearly equal
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
    /// Determines the priority order for block placement directions.
    /// ブロックを置く方向の優先順位を決定します。
    /// </summary>
    /// <param name="direction">Placement direction : 配置方向</param>
    /// <param name="camera">Current camera : 現在のカメラ</param>
    /// <returns>Priority index : 優先順位を表すインデックス</returns>
    private int GetAdjacentPriority(Vector3Int direction, Camera camera)
    {
        Vector3 cameraForward = camera.transform.forward;
        Vector3 cameraPosition = camera.transform.position;
        Vector3 centerWorld = VoxelGridUtility.GridToWorld(_lastPlacedPosition);

        Vector3 candidateWorld =
            centerWorld + new Vector3(direction.x, direction.y, direction.z) * VoxelGridUtility.CellSize;
        Vector3 toCandidate = candidateWorld - centerWorld;

        // カメラに近い方向を手前として扱う
        // Treat the direction closer to the camera as the front direction
        float depth = Vector3.Dot(toCandidate, cameraForward);

        if (depth < -0.1f)
        {
            return 0;
        }

        if (direction == Vector3Int.up)
        {
            return 1;
        }

        if (direction == Vector3Int.left || direction == Vector3Int.right)
        {
            return 2;
        }

        if (direction == Vector3Int.down)
        {
            return 3;
        }

        return 4;
    }

    // =========================================================
    // Eraser Drag
    // 消しゴムドラッグ
    // =========================================================

    // -------------------------
    // Start eraser drag
    // 消しゴムドラッグ開始
    // -------------------------

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

    // -------------------------
    // Erase while dragging
    // ドラッグ中の消去処理
    // -------------------------

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

        if (currentPosition == _lastErasedPosition)
        {
            return;
        }

        EraseBlock();

        _lastErasedPosition = currentPosition;

        SceneView.RepaintAll();
    }

    // -------------------------
    // End mouse drag
    // マウスドラッグ終了
    // -------------------------

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

    // -------------------------
    // Cancel drag
    // ドラッグをキャンセル
    // -------------------------

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
    // Prefab Rotation
    // Prefab回転
    // =========================================================

    /// <summary>
    /// Rotates the selected Prefab by 90 degrees around the specified axes.
    /// 選択中のPrefabを指定した軸を中心に90度回転します。
    /// </summary>
    /// <param name="rotation">Rotation axis and direction : 回転する軸と方向</param>
    private void RotatePrefab(Vector3 rotation)
    {
        _prefabRotation += rotation * 90f;

        _prefabRotation.x = NormalizeAngle(_prefabRotation.x);
        _prefabRotation.y = NormalizeAngle(_prefabRotation.y);
        _prefabRotation.z = NormalizeAngle(_prefabRotation.z);

        SceneView.RepaintAll();
    }

    /// <summary>
    /// Normalizes an angle to the range from 0 to 359 degrees.
    /// 角度を0～359度の範囲に正規化します。
    /// </summary>
    /// <param name="angle">Angle to normalize : 正規化する角度</param>
    /// <returns>Normalized angle : 正規化された角度</returns>
    private float NormalizeAngle(float angle)
    {
        angle %= 360f;
        if (angle < 0f) angle += 360f;
        return angle;
    }

    /// <summary>
    /// Draws the rotation settings for the selected Prefab.
    /// 選択中のPrefabの回転設定を表示します。
    /// </summary>
    private void DrawRotationSettings()
    {
        string rotationLabel = _showRotationSettings ? "Rotation ▲" : "Rotation ▼";

        if (GUILayout.Button(rotationLabel))
        {
            _showRotationSettings = !_showRotationSettings;
        }

        if (!_showRotationSettings) return;

        GUILayout.BeginHorizontal();

        if (GUILayout.Button("X -90", GUILayout.Height(RotationButtonHeight))) RotatePrefab(Vector3.left);
        if (GUILayout.Button("X +90", GUILayout.Height(RotationButtonHeight))) RotatePrefab(Vector3.right);

        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();

        if (GUILayout.Button("Y -90", GUILayout.Height(RotationButtonHeight))) RotatePrefab(Vector3.down);
        if (GUILayout.Button("Y +90", GUILayout.Height(RotationButtonHeight))) RotatePrefab(Vector3.up);

        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();

        if (GUILayout.Button("Z -90", GUILayout.Height(RotationButtonHeight))) RotatePrefab(Vector3.back);
        if (GUILayout.Button("Z +90", GUILayout.Height(RotationButtonHeight))) RotatePrefab(Vector3.forward);

        GUILayout.EndHorizontal();

        GUILayout.Label($"Rotation : {_prefabRotation}");
    }

    // =========================================================
    // Grid
    // グリッド
    // =========================================================

    // -------------------------
    // Draw grid
    // グリッド描画
    // -------------------------

    private void DrawGrid()
    {
        if (!_gridVisible || _voxelWorld == null) return;

        float cellSize = VoxelGridUtility.CellSize;
        if (cellSize <= 0f || _gridSize <= 0) return;

        int gridY = _voxelWorld.MinimumHeight;

        Vector3 center = VoxelGridUtility.GridToWorld(new Vector3Int(0, gridY, 0));
        center += new Vector3(cellSize * 0.5f, 0f, cellSize * 0.5f);

        float totalSize = _gridSize * cellSize;
        float halfSize = totalSize * 0.5f;

        Vector3 start = center - new Vector3(halfSize, 0f, halfSize);

        Handles.color = GridColor;

        for (int i = 0; i <= _gridSize; i++)
        {
            float offset = i * cellSize;

            Vector3 xStart = start + new Vector3(offset, 0f, 0f);
            Vector3 xEnd = xStart + new Vector3(0f, 0f, totalSize);

            Vector3 zStart = start + new Vector3(0f, 0f, offset);
            Vector3 zEnd = zStart + new Vector3(totalSize, 0f, 0f);

            Handles.DrawAAPolyLine(GridLineWidth, xStart, xEnd);
            Handles.DrawAAPolyLine(GridLineWidth, zStart, zEnd);
        }

        Handles.color = Color.white;
    }

    /// <summary>
    /// Draws the grid visibility and size settings.
    /// グリッドの表示状態とサイズ設定を表示します。
    /// </summary>
    private void DrawGridSettings()
    {
        GUILayout.BeginHorizontal();

        if (GUILayout.Button(_gridVisible ? "Grid : ON" : "Grid : OFF"))
        {
            _gridVisible = !_gridVisible;
            SceneView.RepaintAll();
        }

        GUILayout.EndHorizontal();

        if (!_gridVisible) return;

        GUILayout.BeginHorizontal();

        GUILayout.Label("Grid Size", GUILayout.Width(60f));

        if (GUILayout.Button("-", GUILayout.Width(GridSizeButtonWidth)))
        {
            _gridSize = Mathf.Max(GridSizeMin, _gridSize - GridSizeStep);
            SceneView.RepaintAll();
        }

        GUILayout.Label(_gridSize.ToString(), GUI.skin.textField, GUILayout.ExpandWidth(true));

        if (GUILayout.Button("+", GUILayout.Width(GridSizeButtonWidth)))
        {
            _gridSize = Mathf.Min(GridSizeMax, _gridSize + GridSizeStep);
            SceneView.RepaintAll();
        }

        GUILayout.EndHorizontal();
    }

    // =========================================================
    // Preview
    // プレビュー
    // =========================================================

    // -------------------------
    // Placement preview
    // 配置プレビュー
    // -------------------------

    private void DrawPlacementPreview()
    {
        if (!_hasHit) return;

        Vector3Int previewPosition = _gridPosition;
        if (_isDragging && _hasDragStart && _hasDragPreview && _dragDirection != Vector3Int.zero)
            previewPosition = _dragPreviewPosition;

        bool canPlace = _voxelWorld != null && !_voxelWorld.IsBelowMinimumHeight(previewPosition) &&
                        !_voxelWorld.HasBlock(previewPosition);

        Handles.color = canPlace ? Color.green : Color.red;

        Vector3 worldPosition = VoxelGridUtility.GridToWorld(previewPosition);
        Quaternion rotation = Quaternion.Euler(_prefabRotation);

        using (new Handles.DrawingScope(Matrix4x4.TRS(worldPosition, rotation, Vector3.one)))
        {
            Handles.DrawWireCube(Vector3.zero, Vector3.one * VoxelGridUtility.CellSize);
        }

        if (_isDragging && _hasDragStart && _hasDragPreview && _dragDirection != Vector3Int.zero)
            DrawDragPreviewLine(_dragStartPosition, previewPosition);

        Handles.color = Color.white;
    }

    // -------------------------
    // Erase preview
    // 消去プレビュー
    // -------------------------

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

    // -------------------------
    // Drag preview
    // ドラッグプレビュー
    // -------------------------

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
    // GUI
    // =========================================================

    // -------------------------
    // Draw editor GUI
    // エディタGUIを描画
    // -------------------------

    private void DrawGUI()
    {
        Handles.BeginGUI();

        SceneView sceneView = SceneView.currentDrawingSceneView;

        if (sceneView == null)
        {
            Handles.EndGUI();
            return;
        }

        float guiHeight = sceneView.position.height - GuiMargin * 5f;

        Rect guiRect = new Rect(GuiMargin, GuiMargin, GuiWidth, guiHeight);

        GUILayout.BeginArea(guiRect, "Voxel Editor", GUI.skin.window);

        GUILayout.Label("Tool");

        GUILayout.BeginHorizontal();

        if (GUILayout.Toggle(_toolMode == ToolMode.Pen, "Pen", GUI.skin.button)) _toolMode = ToolMode.Pen;
        if (GUILayout.Toggle(_toolMode == ToolMode.Eraser, "Eraser", GUI.skin.button)) _toolMode = ToolMode.Eraser;

        GUILayout.EndHorizontal();

        GUILayout.Space(5);

        if (_toolMode == ToolMode.Pen)
        {
            if (GUILayout.Button(_penDragEnabled ? "ペンドラッグ : ON" : "ペンドラッグ : OFF"))
            {
                _penDragEnabled = !_penDragEnabled;
                CancelDrag();
            }
        }
        else
        {
            if (GUILayout.Button(_eraserDragEnabled ? "消しゴムドラッグ : ON" : "消しゴムドラッグ : OFF"))
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
                GUILayout.Label("Placement Position");
                GUILayout.Label($"(X,Y,Z) = ({_gridPosition.x}, {_gridPosition.y}, {_gridPosition.z})");
            }
            else
            {
                GUILayout.Label("No Hit");
            }

            GUILayout.Space(5);
            DrawGridSettings();

            GUILayout.Space(5);
            DrawRotationSettings();

            GUILayout.Space(5);

            GUILayout.Label("Prefab", EditorStyles.boldLabel);
            DrawPrefabDatabaseSelector();
            GUILayout.Label(_selectedPrefab != null ? $"selected : {_selectedPrefab.name}" : "selected : None");

            Rect lastRect = GUILayoutUtility.GetLastRect();

            float prefabTop = lastRect.yMax + 5f;
            float prefabBottom = guiHeight - 8f;
            float prefabHeight = Mathf.Max(0f, prefabBottom - prefabTop);

            DrawPrefabSelector(prefabHeight, prefabTop);
        }
        else
        {
            GUILayout.Label("Eraser Mode");
        }

        GUILayout.EndArea();
        Handles.EndGUI();
    }

    // -------------------------
    // Prefab List GUI
    // Prefab一覧GUI
    // -------------------------
    
    /// <summary>
    /// Draws the database selection field.
    /// データベース選択欄を表示します。
    /// </summary>
    private void DrawPrefabDatabaseSelector()
    {
        if (_prefabDatabases.Count == 0)
        {
            EditorGUILayout.HelpBox("VoxelPrefabDatabase が見つかりません。", MessageType.Warning);
            return;
        }

        string[] databaseNames = new string[_prefabDatabases.Count];

        for (int i = 0; i < _prefabDatabases.Count; i++)
        {
            databaseNames[i] = _prefabDatabases[i].name;
        }

        int newIndex = EditorGUILayout.Popup(
            "Database",
            _selectedDatabaseIndex,
            databaseNames,
            GUILayout.Height(DatabaseSelectorHeight)
        );

        if (newIndex == _selectedDatabaseIndex)
        {
            return;
        }

        _selectedDatabaseIndex = newIndex;
        _prefabDatabase = _prefabDatabases[_selectedDatabaseIndex];
        _selectedPrefab = null;
        _prefabScrollPosition = Vector2.zero;
        _prefabPreviews.Clear();
        _prefabListHash = 0;

        SceneView.RepaintAll();
    }

    /// <summary>
    /// Draws the Prefab selection list with a scroll view.
    /// Prefab選択リストをスクロールビューとして描画します。
    /// </summary>
    /// <param name="availableHeight">Available height for the Prefab list : Prefab一覧に使用できる高さ</param>
    /// <param name="topOffset">Vertical position of the Prefab list : Prefab一覧の縦方向の位置</param>
    private void DrawPrefabSelector(float availableHeight, float topOffset)
    {
        if (_prefabDatabase == null)
        {
            EditorGUILayout.HelpBox("Prefab Database が選択されていません。", MessageType.Warning);
            return;
        }

        if (_prefabDatabase.Prefabs.Count == 0)
        {
            EditorGUILayout.HelpBox("Prefabが登録されていません。", MessageType.Info);
            return;
        }

        availableHeight = Mathf.Max(0f, availableHeight);

        Rect areaRect = GUILayoutUtility.GetRect(
            GUIContent.none,
            GUIStyle.none,
            GUILayout.Height(availableHeight),
            GUILayout.ExpandWidth(true)
        );

        areaRect.y = topOffset;
        areaRect.height = availableHeight;

        GUI.Box(areaRect, GUIContent.none, GUI.skin.box);

        const float border = 2f;
        Rect scrollRect = new Rect(
            areaRect.x + border,
            areaRect.y + border,
            Mathf.Max(0f, areaRect.width - border * 2f),
            Mathf.Max(0f, areaRect.height - border * 2f)
        );

        int prefabCount = _prefabDatabase.Prefabs.Count;
        int rows = Mathf.CeilToInt((float)prefabCount / PrefabColumns);
        float contentHeight = rows * PrefabItemHeight + Mathf.Max(0, rows - 1) * PrefabItemSpacing;

        float scrollbarWidth = GUI.skin.verticalScrollbar.fixedWidth;
        Rect contentRect = new Rect(
            0f,
            0f,
            Mathf.Max(scrollRect.width - scrollbarWidth),
            contentHeight
        );

        _prefabScrollPosition = GUI.BeginScrollView(
            scrollRect,
            _prefabScrollPosition,
            contentRect,
            false,
            true
        );

        float itemWidth = (contentRect.width - PrefabItemSpacing) / PrefabColumns;

        for (int i = 0; i < prefabCount; i++)
        {
            int column = i % PrefabColumns;
            int row = i / PrefabColumns;
            float x = column * (itemWidth + PrefabItemSpacing);
            float y = row * (PrefabItemHeight + PrefabItemSpacing);
            Rect itemRect = new Rect(x, y, itemWidth, PrefabItemHeight);

            DrawPrefabButton(_prefabDatabase.Prefabs[i], itemRect);
        }

        GUI.EndScrollView();
    }

    /// <summary>
    /// Draws a Prefab selection button with its preview, name, and selection border.
    /// Prefabのプレビュー、名前、選択枠を含む選択ボタンを描画します。
    /// </summary>
    /// <param name="prefab">Prefab to display : 表示するPrefab</param>
    /// <param name="buttonRect">Display rectangle of the button : ボタンを表示する領域</param>
    private void DrawPrefabButton(GameObject prefab, Rect buttonRect)
    {
        if (prefab == null) return;

        Texture2D preview = GetPrefabPreview(prefab);
        bool selected = _selectedPrefab == prefab;

        if (GUI.Button(buttonRect, GUIContent.none, GUI.skin.button))
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

            GUI.DrawTexture(previewRect, preview, ScaleMode.ScaleToFit, true);
        }

        Rect labelRect = new Rect(
            buttonRect.x + 3f,
            buttonRect.y + buttonRect.height - PrefabLabelHeight - 2f,
            buttonRect.width - 6f,
            PrefabLabelHeight
        );

        if (_prefabLabelStyle == null)
        {
            _prefabLabelStyle = new GUIStyle(EditorStyles.centeredGreyMiniLabel);
            _prefabLabelStyle.normal.textColor = Color.white;
        }

        GUI.Label(labelRect, prefab.name, _prefabLabelStyle);

        if (selected)
        {
            DrawPrefabSelectionBorder(buttonRect);
        }
    }

    /// <summary>
    /// Draws a yellow border around the selected Prefab while respecting the scroll view clipping area.
    /// 選択中のPrefabの周囲に黄色い枠線を描画し、スクロールビューによる表示範囲の制限を反映します。
    /// </summary>
    /// <param name="rect">Rectangle of the selected Prefab : 選択中Prefabの表示領域</param>
    private void DrawPrefabSelectionBorder(Rect rect)
    {
        float width = PrefabSelectionBorderWidth;
        Color previousColor = GUI.color;
        GUI.color = Color.yellow;

        GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width, width), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(rect.x, rect.yMax - width, rect.width, width), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(rect.x, rect.y, width, rect.height), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(rect.xMax - width, rect.y, width, rect.height), Texture2D.whiteTexture);

        GUI.color = previousColor;
    }

    // =========================================================
    // Prefab Database
    // Prefabデータベース
    // =========================================================

    /// <summary>
    /// Loads all VoxelPrefabDatabase assets.
    /// すべてのVoxelPrefabDatabaseアセットを読み込みます。
    /// </summary>
    private void LoadPrefabDatabase()
    {
        string[] guids = AssetDatabase.FindAssets("t:VoxelPrefabDatabase");

        List<VoxelPrefabDatabase> databases = new();

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            VoxelPrefabDatabase database = AssetDatabase.LoadAssetAtPath<VoxelPrefabDatabase>(path);

            if (database != null)
            {
                databases.Add(database);
            }
        }

        databases.Sort((a, b) => string.Compare(a.name, b.name, System.StringComparison.Ordinal));

        bool databaseChanged = _prefabDatabases.Count != databases.Count;

        if (!databaseChanged)
        {
            for (int i = 0; i < databases.Count; i++)
            {
                if (_prefabDatabases[i] != databases[i])
                {
                    databaseChanged = true;
                    break;
                }
            }
        }

        if (databaseChanged)
        {
            _prefabDatabases = databases;

            if (_prefabDatabases.Count == 0)
            {
                _prefabDatabase = null;
                _selectedDatabaseIndex = 0;
                _selectedPrefab = null;
                _prefabPreviews.Clear();
                _prefabListHash = 0;
                return;
            }

            _selectedDatabaseIndex = Mathf.Clamp(
                _selectedDatabaseIndex,
                0,
                _prefabDatabases.Count - 1
            );

            _prefabDatabase = _prefabDatabases[_selectedDatabaseIndex];
            _selectedPrefab = null;
            _prefabPreviews.Clear();
            _prefabListHash = 0;

            SceneView.RepaintAll();
        }

        if (_prefabDatabases.Count == 0)
        {
            return;
        }

        if (_selectedDatabaseIndex >= _prefabDatabases.Count)
        {
            _selectedDatabaseIndex = 0;
        }

        _prefabDatabase = _prefabDatabases[_selectedDatabaseIndex];

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
    /// Gets the preview image of the specified Prefab.
    /// 指定したPrefabのプレビュー画像を取得します。
    /// </summary>
    /// <param name="prefab">Prefab to get the preview for : プレビューを取得するPrefab</param>
    /// <returns>Prefab preview texture : Prefabのプレビュー画像</returns>
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
}