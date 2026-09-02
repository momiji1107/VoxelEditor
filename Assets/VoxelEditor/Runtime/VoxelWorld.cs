using System.Collections.Generic;
using UnityEngine;

namespace VoxelEditor.Runtime
{
    public class VoxelWorld : MonoBehaviour
    {
        [Header("Grid Settings")]
        [SerializeField] private int _minimumHeight = 0;

        [SerializeField, Min(0.01f)]
        private float _cellSize = 1f;

        [SerializeField]
        private List<VoxelBlockData> _blocks = new();

        private float _lastValidCellSize;

        public int MinimumHeight => _minimumHeight;
        public float CellSize => _cellSize;

        public IReadOnlyList<VoxelBlockData> Blocks => _blocks;

        private void Awake()
        {
            _lastValidCellSize = _cellSize;
        }

        private void OnValidate()
        {
            _cellSize = Mathf.Max(0.1f, _cellSize);

            if (_lastValidCellSize <= 0f)
            {
                _lastValidCellSize = _cellSize;
            }

            if (_blocks != null && _blocks.Count > 0)
            {
                _cellSize = _lastValidCellSize;
                return;
            }

            _lastValidCellSize = _cellSize;
        }
        
        public bool IsBelowMinimumHeight(Vector3Int gridPosition)
        {
            return gridPosition.y < _minimumHeight;
        }

        public void AddBlock(
            GameObject prefab,
            Vector3Int gridPosition,
            Vector3Int gridSize,
            Quaternion rotation)
        {
            VoxelBlockData blockData =
                new VoxelBlockData(
                    prefab,
                    gridPosition,
                    gridSize,
                    rotation
                );

            _blocks.Add(blockData);
        }

        public void RemoveBlock(Vector3Int gridPosition)
        {
            for (int i = _blocks.Count - 1; i >= 0; i--)
            {
                if (_blocks[i].OccupiesPosition(gridPosition))
                {
                    _blocks.RemoveAt(i);
                    return;
                }
            }
        }

        public bool TryGetBlock(
            Vector3Int gridPosition,
            out VoxelBlockData blockData)
        {
            foreach (VoxelBlockData block in _blocks)
            {
                if (block.OccupiesPosition(gridPosition))
                {
                    blockData = block;
                    return true;
                }
            }

            blockData = null;
            return false;
        }

        public bool IsPositionOccupied(Vector3Int position)
        {
            foreach (VoxelBlockData block in _blocks)
            {
                if (block.OccupiesPosition(position))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Determines whether the entire rotated Prefab can be placed.
        /// 回転後のPrefab全体を配置できるか判定します。
        /// </summary>
        public bool CanPlaceBlock(
            Vector3Int gridPosition,
            Vector3Int gridSize,
            Quaternion rotation)
        {
            for (int x = 0; x < gridSize.x; x++)
            {
                for (int y = 0; y < gridSize.y; y++)
                {
                    for (int z = 0; z < gridSize.z; z++)
                    {
                        Vector3Int localPosition =
                            new Vector3Int(x, y, z);

                        Vector3Int occupiedPosition =
                            VoxelGridUtility.GetRotatedGridPosition(
                                gridPosition,
                                localPosition,
                                rotation
                            );

                        if (IsBelowMinimumHeight(occupiedPosition))
                        {
                            return false;
                        }

                        if (IsPositionOccupied(occupiedPosition))
                        {
                            return false;
                        }
                    }
                }
            }

            return true;
        }
    }
}