using System;
using UnityEngine;

namespace VoxelEditor.Runtime
{
    [Serializable]
    public class VoxelBlockData
    {
        [SerializeField] private GameObject _prefab;
        [SerializeField] private Vector3Int _gridPosition;
        [SerializeField] private Vector3Int _gridSize;
        [SerializeField] private Quaternion _rotation = Quaternion.identity;

        public GameObject Prefab => _prefab;
        public Vector3Int GridPosition => _gridPosition;
        public Vector3Int GridSize => _gridSize;
        public Quaternion Rotation => _rotation;

        public VoxelBlockData(
            GameObject prefab,
            Vector3Int gridPosition,
            Vector3Int gridSize,
            Quaternion rotation)
        {
            _prefab = prefab;
            _gridPosition = gridPosition;
            _gridSize = gridSize;
            _rotation = rotation;
        }

        /// <summary>
        /// Determines whether this block occupies the specified Grid position,
        /// taking its rotation into account.
        /// 回転を考慮して、このブロックが指定されたGrid位置を占有しているか判定します。
        /// </summary>
        public bool OccupiesPosition(Vector3Int position)
        {
            Vector3Int localPosition =
                VoxelGridUtility.RotateGridOffset(
                    position - _gridPosition,
                    Quaternion.Inverse(_rotation)
                );

            return localPosition.x >= 0 &&
                localPosition.x < _gridSize.x &&
                localPosition.y >= 0 &&
                localPosition.y < _gridSize.y &&
                localPosition.z >= 0 &&
                localPosition.z < _gridSize.z;
        }
    }
}