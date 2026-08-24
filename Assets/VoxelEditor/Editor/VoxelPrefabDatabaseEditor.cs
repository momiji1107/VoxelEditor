using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(VoxelPrefabDatabase))]
public class VoxelPrefabDatabaseEditor
    : Editor
{
    private GameObject _prefabToAdd;

    public override void OnInspectorGUI()
    {
        VoxelPrefabDatabase database =
            (VoxelPrefabDatabase)target;

        EditorGUILayout.LabelField(
            "Voxel Prefabs",
            EditorStyles.boldLabel
        );

        EditorGUILayout.Space(5);

        _prefabToAdd =
            (GameObject)EditorGUILayout.ObjectField(
                "Prefab",
                _prefabToAdd,
                typeof(GameObject),
                false
            );

        if (GUILayout.Button("Add Prefab"))
        {
            AddPrefab(database);
        }

        EditorGUILayout.Space(10);

        EditorGUILayout.LabelField(
            $"登録Prefab数 : {database.Prefabs.Count}"
        );

        EditorGUILayout.Space(5);

        for (int i = 0;
             i < database.Prefabs.Count;
             i++)
        {
            GameObject prefab =
                database.Prefabs[i];

            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.ObjectField(
                prefab,
                typeof(GameObject),
                false
            );

            if (GUILayout.Button(
                    "Remove",
                    GUILayout.Width(70)))
            {
                RemovePrefab(
                    database,
                    prefab
                );

                break;
            }

            EditorGUILayout.EndHorizontal();
        }
    }

    private void AddPrefab(
        VoxelPrefabDatabase database)
    {
        if (_prefabToAdd == null)
        {
            return;
        }

        if (database.Contains(
                _prefabToAdd))
        {
            return;
        }

        Undo.RecordObject(
            database,
            "Add Voxel Prefab"
        );

        database.AddPrefab(
            _prefabToAdd
        );

        EditorUtility.SetDirty(
            database
        );

        _prefabToAdd = null;
    }

    private void RemovePrefab(
        VoxelPrefabDatabase database,
        GameObject prefab)
    {
        Undo.RecordObject(
            database,
            "Remove Voxel Prefab"
        );

        database.RemovePrefab(
            prefab
        );

        EditorUtility.SetDirty(
            database
        );
    }
}