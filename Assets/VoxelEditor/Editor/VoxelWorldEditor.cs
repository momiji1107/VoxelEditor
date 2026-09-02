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

        private void OnEnable()
        {
            _minimumHeight = serializedObject.FindProperty("_minimumHeight");
            _cellSize = serializedObject.FindProperty("_cellSize");
            _blocks = serializedObject.FindProperty("_blocks");

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

            _blocksList.drawElementCallback = (rect, index, isActive, isFocused) =>
            {
                if (index < 0 || index >= _blocks.arraySize)
                {
                    return;
                }

                SerializedProperty element = _blocks.GetArrayElementAtIndex(index);

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

                SerializedProperty element = _blocks.GetArrayElementAtIndex(index);

                return EditorGUI.GetPropertyHeight(
                    element,
                    true) + 4f;
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
                    new GUIContent("Cell Size"));
            }

            if (hasBlocks)
            {
                EditorGUILayout.HelpBox(
                    VoxelWorldEditorMessage(),
                    MessageType.Info);
            }

            _blocksList.DoLayoutList();

            serializedObject.ApplyModifiedProperties();
        }

        private string VoxelWorldEditorMessage()
        {
            return "VoxelWorldにブロックが1個以上存在するため、Cell Sizeは変更できません。";
        }
    }
}