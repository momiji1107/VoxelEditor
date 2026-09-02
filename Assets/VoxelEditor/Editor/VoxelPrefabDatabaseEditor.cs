using UnityEditor;
using UnityEngine;
using VoxelEditor.Runtime;

namespace VoxelEditor.Editor
{
    [CustomEditor(typeof(VoxelPrefabDatabase))]
    public class VoxelPrefabDatabaseEditor : UnityEditor.Editor
    {
        private GameObject _prefabToAdd;

        public override void OnInspectorGUI()
        {
            VoxelPrefabDatabase database = (VoxelPrefabDatabase)target;

            EditorGUILayout.LabelField(
                "Voxel Prefabs",
                EditorStyles.boldLabel
            );

            EditorGUILayout.Space(5);

            _prefabToAdd = (GameObject)EditorGUILayout.ObjectField(
                "Prefab",
                _prefabToAdd,
                typeof(GameObject),
                false
            );

            if (GUILayout.Button(
                    VoxelEditorTool.Localize("プレハブを追加", "Add Prefab")))
            {
                AddPrefab(database);
            }

            EditorGUILayout.Space(10);

            EditorGUILayout.LabelField(
                VoxelEditorTool.Localize(
                    "登録Prefab数",
                    "Number of registered Prefabs"
                ) + $" : {database.Prefabs.Count}"
            );

            EditorGUILayout.Space(5);

            for (int i = 0; i < database.Prefabs.Count; i++)
            {
                VoxelPrefabEntry entry = database.Prefabs[i];

                if (entry == null)
                {
                    continue;
                }

                GameObject prefab = entry.Prefab;

                EditorGUILayout.BeginVertical("box");

                EditorGUILayout.BeginHorizontal();

                EditorGUILayout.ObjectField(
                    "Prefab",
                    prefab,
                    typeof(GameObject),
                    false
                );

                if (GUILayout.Button(
                        VoxelEditorTool.Localize("削除", "Remove"),
                        GUILayout.Width(70)))
                {
                    RemovePrefab(database, i);
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.EndVertical();
                    break;
                }

                EditorGUILayout.EndHorizontal();

                EditorGUILayout.Space(3);

                Vector3Int currentGridSize = entry.GridSize;

                Vector3Int newGridSize = EditorGUILayout.Vector3IntField(
                    VoxelEditorTool.Localize(
                        "Gridサイズ",
                        "Grid Size"
                    ),
                    currentGridSize
                );

                newGridSize = new Vector3Int(
                    Mathf.Max(1, newGridSize.x),
                    Mathf.Max(1, newGridSize.y),
                    Mathf.Max(1, newGridSize.z)
                );

                if (newGridSize != currentGridSize)
                {
                    Undo.RecordObject(
                        database,
                        "Change Voxel Prefab Grid Size"
                    );

                    entry.SetGridSize(newGridSize);

                    EditorUtility.SetDirty(database);
                }

                Vector3Int currentRotation = entry.Rotation;
                
                Vector3Int newRotation = EditorGUILayout.Vector3IntField(
                    VoxelEditorTool.Localize(
                        "回転",
                        "Rotation"
                    ),
                    currentRotation
                );

                newRotation = new Vector3Int(
                    newRotation.x,
                    newRotation.y,
                    newRotation.z
                );

                if (newRotation != currentRotation)
                {
                    Undo.RecordObject(
                        database,
                        "Change Voxel Prefab Rotation"
                    );

                    entry.SetRotation(newRotation);

                    EditorUtility.SetDirty(database);
                }

                EditorGUILayout.EndVertical();

                EditorGUILayout.Space(5);
            }
        }

        private void AddPrefab(VoxelPrefabDatabase database)
        {
            if (_prefabToAdd == null)
            {
                return;
            }

            if (database.Contains(_prefabToAdd))
            {
                return;
            }

            Undo.RecordObject(
                database,
                "Add Voxel Prefab"
            );

            database.AddPrefab(_prefabToAdd);

            EditorUtility.SetDirty(database);

            _prefabToAdd = null;
        }

        private void RemovePrefab(
            VoxelPrefabDatabase database,
            int index)
        {
            Undo.RecordObject(
                database,
                "Remove Voxel Prefab"
            );

            database.RemovePrefab(index);

            EditorUtility.SetDirty(database);
        }
    }
}