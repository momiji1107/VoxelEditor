using System.Collections.Generic;
using UnityEngine;

public class VoxelWorld : MonoBehaviour
{
    [SerializeField]
    private List<VoxelBlockData> _blocks = new();

    public IReadOnlyList<VoxelBlockData> Blocks => _blocks;

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
}