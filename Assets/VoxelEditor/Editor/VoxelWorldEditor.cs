using UnityEditor;
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

        private void OnEnable()
        {
            _minimumHeight = serializedObject.FindProperty("_minimumHeight");
            _cellSize = serializedObject.FindProperty("_cellSize");
            _blocks = serializedObject.FindProperty("_blocks");
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

            EditorGUILayout.PropertyField(_blocks, true);

            serializedObject.ApplyModifiedProperties();
        }

        private string VoxelWorldEditorMessage()
        {
            return "VoxelWorldにブロックが1個以上存在するため、Cell Sizeは変更できません。";
        }
    }
}