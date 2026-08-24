using System.Collections.Generic;
using UnityEngine;

public class VoxelWorld : MonoBehaviour
{
    [Header("Grid Settings")]
    [SerializeField]
    private int _minimumHeight = 0;

    [SerializeField]
    private List<VoxelBlockData> _blocks = new();

    public int MinimumHeight => _minimumHeight;

    public IReadOnlyList<VoxelBlockData> Blocks => _blocks;

    public bool IsBelowMinimumHeight(
        Vector3Int gridPosition)
    {
        return gridPosition.y < _minimumHeight;
    }

    public void AddBlock(
        Vector3Int gridPosition,
        GameObject prefab,
        Quaternion rotation)
    {
        VoxelBlockData blockData =
            new VoxelBlockData(
                gridPosition,
                prefab,
                rotation
            );

        _blocks.Add(blockData);
    }

    public void RemoveBlock(
        Vector3Int gridPosition)
    {
        for (int i = _blocks.Count - 1; i >= 0; i--)
        {
            if (_blocks[i].GridPosition == gridPosition)
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
            if (block.GridPosition == gridPosition)
            {
                blockData = block;

                return true;
            }
        }

        blockData = null;

        return false;
    }
    
    public bool HasBlock(
        Vector3Int gridPosition)
    {
        return TryGetBlock(
            gridPosition,
            out _
        );
    }
}