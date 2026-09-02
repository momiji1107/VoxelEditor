using System.Collections.Generic;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using VoxelEditor.Runtime;

namespace VoxelEditor.Editor
{
    [CustomEditor(typeof(VoxelWorld))]
    public class VoxelWorldEditor : UnityEditor.Editor
    {
        private SerializedProperty _minimumHeight;
        private SerializedProperty _cellSize;
        private SerializedProperty _blocks;

        private ReorderableList _blocksList;

        private bool _isSynchronizing;

        private readonly Dictionary<int, BlockSnapshot> _hierarchyBlocks = new();

        private class BlockSnapshot
        {
            public Vector3 WorldPosition;
            public Quaternion Rotation;
            public GameObject Prefab;
            public Vector3Int GridPosition;
            public Vector3Int GridSize;
        }

        private void OnEnable()
        {
            _minimumHeight = serializedObject.FindProperty("_minimumHeight");
            _cellSize = serializedObject.FindProperty("_cellSize");
            _blocks = serializedObject.FindProperty("_blocks");

            CreateBlocksList();

            CacheHierarchyBlocks();
        }

        private void OnDisable()
        {
            _hierarchyBlocks.Clear();
        }

        private void CreateBlocksList()
        {
            _blocksList = new ReorderableList(
                serializedObject,
                _blocks,
                false,
                true,
                false,
                true);

            _blocksList.drawHeaderCallback = rect =>
            {
                EditorGUI.LabelField(rect, "Blocks");
            };

            _blocksList.drawElementCallback = (
                rect,
                index,
                isActive,
                isFocused) =>
            {
                if (index < 0 || index >= _blocks.arraySize)
                {
                    return;
                }

                SerializedProperty element =
                    _blocks.GetArrayElementAtIndex(index);

                rect.y += 2f;

                using (new EditorGUI.DisabledScope(true))
                {
                    EditorGUI.PropertyField(
                        rect,
                        element,
                        new GUIContent($"Element {index}"),
                        true);
                }
            };

            _blocksList.elementHeightCallback = index =>
            {
                if (index < 0 || index >= _blocks.arraySize)
                {
                    return EditorGUIUtility.singleLineHeight;
                }

                SerializedProperty element =
                    _blocks.GetArrayElementAtIndex(index);

                return EditorGUI.GetPropertyHeight(
                    element,
                    true) + 4f;
            };

            _blocksList.onRemoveCallback = list =>
            {
                RemoveBlock(list.index);
            };
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(_minimumHeight);

            bool hasBlocks = _blocks.arraySize > 0;

            using (new EditorGUI.DisabledScope(hasBlocks))
            {
                EditorGUILayout.PropertyField(
                    _cellSize,
                    new GUIContent(
                        Localize(
                            "セルサイズ", 
                            "Cell Size")
                        )
                    );
            }

            if (hasBlocks)
            {
                EditorGUILayout.HelpBox(
                    VoxelWorldEditorMessage(),
                    MessageType.Info);
            }
            
            using (new EditorGUI.DisabledScope(!hasBlocks))
            {
                if (GUILayout.Button(
                        Localize(
                            "ブロックをすべて削除", 
                            "Clear All Blocks")
                        )
                    )
                {
                    ClearBlocks();
                }
            }

            _blocksList.DoLayoutList();

            serializedObject.ApplyModifiedProperties();

            CacheHierarchyBlocks();
        }

        /// <summary>
        /// HierarchyからBlockが削除された場合に、
        /// VoxelWorldのBlocksからも対応する要素を削除します。
        /// </summary>
        private void OnHierarchyChange()
        {
            if (_isSynchronizing)
            {
                return;
            }

            VoxelWorld voxelWorld = target as VoxelWorld;

            if (voxelWorld == null)
            {
                return;
            }

            SyncRemovedHierarchyBlocks();
        }

        private void SyncRemovedHierarchyBlocks()
        {
            VoxelWorld voxelWorld = (VoxelWorld)target;

            Dictionary<int, BlockSnapshot> currentBlocks = new();

            Transform worldTransform = voxelWorld.transform;

            for (int i = 0; i < worldTransform.childCount; i++)
            {
                Transform child = worldTransform.GetChild(i);

                if (child == null)
                {
                    continue;
                }

                int instanceId = child.gameObject.GetInstanceID();

                currentBlocks[instanceId] =
                    CreateBlockSnapshot(child.gameObject);
            }

            List<BlockSnapshot> removedBlocks = new();

            foreach (KeyValuePair<int, BlockSnapshot> previous in _hierarchyBlocks)
            {
                if (!currentBlocks.ContainsKey(previous.Key))
                {
                    removedBlocks.Add(previous.Value);
                }
            }

            if (removedBlocks.Count == 0)
            {
                _hierarchyBlocks.Clear();

                foreach (KeyValuePair<int, BlockSnapshot> current in currentBlocks)
                {
                    _hierarchyBlocks[current.Key] = current.Value;
                }

                return;
            }

            _isSynchronizing = true;

            try
            {
                serializedObject.Update();

                for (int i = _blocks.arraySize - 1; i >= 0; i--)
                {
                    SerializedProperty element =
                        _blocks.GetArrayElementAtIndex(i);

                    if (MatchesAnyRemovedBlock(
                            element,
                            removedBlocks,
                            voxelWorld))
                    {
                        _blocks.DeleteArrayElementAtIndex(i);
                    }
                }

                serializedObject.ApplyModifiedProperties();

                EditorUtility.SetDirty(voxelWorld);

                _hierarchyBlocks.Clear();

                foreach (KeyValuePair<int, BlockSnapshot> current in currentBlocks)
                {
                    _hierarchyBlocks[current.Key] = current.Value;
                }
            }
            finally
            {
                _isSynchronizing = false;
            }
        }

        private bool MatchesAnyRemovedBlock(
            SerializedProperty element,
            List<BlockSnapshot> removedBlocks,
            VoxelWorld voxelWorld)
        {
            SerializedProperty gridPositionProperty =
                element.FindPropertyRelative("_gridPosition");

            SerializedProperty gridSizeProperty =
                element.FindPropertyRelative("_gridSize");

            SerializedProperty prefabProperty =
                element.FindPropertyRelative("_prefab");

            SerializedProperty rotationProperty =
                element.FindPropertyRelative("_rotation");

            Vector3Int gridPosition =
                gridPositionProperty.vector3IntValue;

            Vector3Int gridSize =
                gridSizeProperty.vector3IntValue;

            GameObject prefab =
                prefabProperty.objectReferenceValue as GameObject;

            Quaternion rotation =
                rotationProperty.quaternionValue;

            Vector3 expectedWorldPosition =
                VoxelGridUtility.GridToWorldCenter(
                    gridPosition,
                    gridSize,
                    rotation,
                    voxelWorld.CellSize);

            foreach (BlockSnapshot removedBlock in removedBlocks)
            {
                if (IsMatchingBlock(
                        removedBlock,
                        expectedWorldPosition,
                        prefab,
                        rotation))
                {
                    return true;
                }
            }

            return false;
        }

        private void RemoveBlock(int index)
        {
            if (index < 0 || index >= _blocks.arraySize)
            {
                return;
            }

            VoxelWorld voxelWorld = (VoxelWorld)target;

            SerializedProperty element =
                _blocks.GetArrayElementAtIndex(index);

            SerializedProperty gridPositionProperty =
                element.FindPropertyRelative("_gridPosition");

            SerializedProperty gridSizeProperty =
                element.FindPropertyRelative("_gridSize");

            SerializedProperty prefabProperty =
                element.FindPropertyRelative("_prefab");

            SerializedProperty rotationProperty =
                element.FindPropertyRelative("_rotation");

            Vector3Int gridPosition =
                gridPositionProperty.vector3IntValue;

            Vector3Int gridSize =
                gridSizeProperty.vector3IntValue;

            GameObject prefab =
                prefabProperty.objectReferenceValue as GameObject;

            Quaternion rotation =
                rotationProperty.quaternionValue;

            Vector3 expectedWorldPosition =
                VoxelGridUtility.GridToWorldCenter(
                    gridPosition,
                    gridSize,
                    rotation,
                    voxelWorld.CellSize);

            Undo.SetCurrentGroupName("Remove Voxel Block");

            int undoGroup =
                Undo.GetCurrentGroup();

            _isSynchronizing = true;

            try
            {
                RemoveHierarchyBlock(
                    voxelWorld,
                    expectedWorldPosition,
                    prefab,
                    rotation);

                serializedObject.Update();

                _blocks.DeleteArrayElementAtIndex(index);

                serializedObject.ApplyModifiedProperties();

                EditorUtility.SetDirty(voxelWorld);
            }
            finally
            {
                _isSynchronizing = false;
            }

            Undo.CollapseUndoOperations(undoGroup);

            CacheHierarchyBlocks();
        }

        private void RemoveHierarchyBlock(
            VoxelWorld voxelWorld,
            Vector3 expectedWorldPosition,
            GameObject prefab,
            Quaternion rotation)
        {
            Transform worldTransform =
                voxelWorld.transform;

            for (int i = worldTransform.childCount - 1; i >= 0; i--)
            {
                Transform child =
                    worldTransform.GetChild(i);

                if (!IsMatchingBlock(
                        child.gameObject,
                        expectedWorldPosition,
                        prefab,
                        rotation))
                {
                    continue;
                }

                Undo.DestroyObjectImmediate(
                    child.gameObject);

                return;
            }
        }

        /// <summary>
        /// BlocksとHierarchy上のBlockをすべて削除します。
        /// </summary>
        private void ClearBlocks()
        {
            VoxelWorld voxelWorld = (VoxelWorld)target;

            if (_blocks.arraySize == 0 &&
                voxelWorld.transform.childCount == 0)
            {
                return;
            }

            Undo.SetCurrentGroupName("Clear Voxel Blocks");

            int undoGroup =
                Undo.GetCurrentGroup();

            _isSynchronizing = true;

            try
            {
                Transform worldTransform =
                    voxelWorld.transform;

                for (int i = worldTransform.childCount - 1; i >= 0; i--)
                {
                    GameObject child =
                        worldTransform.GetChild(i).gameObject;

                    Undo.DestroyObjectImmediate(child);
                }

                serializedObject.Update();

                _blocks.ClearArray();

                serializedObject.ApplyModifiedProperties();

                EditorUtility.SetDirty(voxelWorld);

                _hierarchyBlocks.Clear();
            }
            finally
            {
                _isSynchronizing = false;
            }

            Undo.CollapseUndoOperations(undoGroup);
        }

        private void CacheHierarchyBlocks()
        {
            if (target == null)
            {
                return;
            }

            VoxelWorld voxelWorld = (VoxelWorld)target;

            _hierarchyBlocks.Clear();

            Transform worldTransform =
                voxelWorld.transform;

            for (int i = 0; i < worldTransform.childCount; i++)
            {
                Transform child =
                    worldTransform.GetChild(i);

                if (child == null)
                {
                    continue;
                }

                _hierarchyBlocks[
                    child.gameObject.GetInstanceID()] =
                    CreateBlockSnapshot(child.gameObject);
            }
        }

        private BlockSnapshot CreateBlockSnapshot(
            GameObject block)
        {
            return new BlockSnapshot
            {
                WorldPosition = block.transform.position,
                Rotation = block.transform.rotation,
                Prefab = PrefabUtility.GetCorrespondingObjectFromSource<GameObject>(
                    block)
            };
        }

        private bool IsMatchingBlock(
            BlockSnapshot block,
            Vector3 expectedWorldPosition,
            GameObject prefab,
            Quaternion rotation)
        {
            const float positionTolerance = 0.001f;
            const float rotationTolerance = 0.01f;

            if (Vector3.Distance(
                    block.WorldPosition,
                    expectedWorldPosition) >
                positionTolerance)
            {
                return false;
            }

            if (Quaternion.Angle(
                    block.Rotation,
                    rotation) >
                rotationTolerance)
            {
                return false;
            }

            if (prefab == null)
            {
                return block.Prefab == null;
            }

            return block.Prefab == prefab;
        }

        private bool IsMatchingBlock(
            GameObject block,
            Vector3 expectedWorldPosition,
            GameObject prefab,
            Quaternion rotation)
        {
            const float positionTolerance = 0.001f;
            const float rotationTolerance = 0.01f;

            if (Vector3.Distance(
                    block.transform.position,
                    expectedWorldPosition) >
                positionTolerance)
            {
                return false;
            }

            if (Quaternion.Angle(
                    block.transform.rotation,
                    rotation) >
                rotationTolerance)
            {
                return false;
            }

            if (prefab == null)
            {
                return true;
            }

            GameObject sourcePrefab =
                PrefabUtility.GetCorrespondingObjectFromSource<GameObject>(
                    block);

            return sourcePrefab == prefab;
        }

        private string VoxelWorldEditorMessage()
        {
            return Localize(
                "VoxelWorldにブロックが1個以上存在するため、Cell Sizeは変更できません。", 
                "The Cell Size cannot be changed because there is at least one block in VoxelWorld."
                );
        }
        
        public static string Localize(string japanese, string english)
        {
            return EditorPrefs.GetString("Editor.kEditorLanguage", "English") == "Japanese" ? japanese : english;
        }
    }
}