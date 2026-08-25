using System.Collections.Generic;
using UnityEngine;

namespace VoxelEditor.Runtime
{
    [CreateAssetMenu(
        fileName = "VoxelPrefabDatabase",
        menuName = "Voxel Editor/Prefab Database"
    )]
    public class VoxelPrefabDatabase : ScriptableObject
    {
        [SerializeField] private List<GameObject> _prefabs = new();

        public IReadOnlyList<GameObject> Prefabs => _prefabs;

        public void AddPrefab(GameObject prefab)
        {
            if (prefab == null) return;
            if (_prefabs.Contains(prefab)) return;

            _prefabs.Add(prefab);
        }

        public void RemovePrefab(GameObject prefab)
        {
            if (prefab == null) return;

            _prefabs.Remove(prefab);
        }

        public bool Contains(GameObject prefab)
        {
            return prefab != null && _prefabs.Contains(prefab);
        }
    }
}