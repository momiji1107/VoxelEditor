using System.Collections.Generic;
using UnityEngine;

namespace VoxelEditor.Runtime
{
    [System.Serializable]
    public class VoxelPrefabEntry
    {
        [SerializeField] private GameObject _prefab;
        [SerializeField] private Vector3Int _gridSize = Vector3Int.one;
        [SerializeField] private Vector3Int _rotation = Vector3Int.one;

        public GameObject Prefab => _prefab;
        public Vector3Int GridSize => _gridSize;
        public Vector3Int Rotation => _rotation;

        public VoxelPrefabEntry(GameObject prefab)
        {
            _prefab = prefab;
            
            Vector3 scale = prefab.transform.localScale;
            SetGridSize(new Vector3Int(
                Mathf.CeilToInt(scale.x), 
                Mathf.CeilToInt(scale.y), 
                Mathf.CeilToInt(scale.z))
            );
            
            _rotation = Vector3Int.one;
        }

        public void SetGridSize(Vector3Int gridSize)
        {
            _gridSize = new Vector3Int(
                Mathf.Max(1, gridSize.x),
                Mathf.Max(1, gridSize.y),
                Mathf.Max(1, gridSize.z)
            );
        }
        
        public void SetRotation(Vector3Int rotation)
        {
            _rotation = rotation;
        }
    }

    [CreateAssetMenu(
        fileName = "VoxelPrefabDatabase",
        menuName = "Voxel Editor/Prefab Database"
    )]
    public class VoxelPrefabDatabase : ScriptableObject
    {
        [SerializeField] private List<VoxelPrefabEntry> _prefabs = new();

        public IReadOnlyList<VoxelPrefabEntry> Prefabs => _prefabs;

        public void AddPrefab(GameObject prefab)
        {
            if (prefab == null)
            {
                return;
            }

            if (Contains(prefab))
            {
                return;
            }

            _prefabs.Add(new VoxelPrefabEntry(prefab));
        }
        
        public void RemovePrefab(int index)
        {
            if (index < 0 || index >= _prefabs.Count)
            {
                return;
            }

            _prefabs.RemoveAt(index);
        }

        public bool Contains(GameObject prefab)
        {
            if (prefab == null)
            {
                return false;
            }

            foreach (VoxelPrefabEntry entry in _prefabs)
            {
                if (entry != null && entry.Prefab == prefab)
                {
                    return true;
                }
            }

            return false;
        }
    }
}