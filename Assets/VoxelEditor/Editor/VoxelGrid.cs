using System.Collections.Generic;
using UnityEngine;

public class VoxelGrid
{
    private readonly Dictionary<Vector3Int, GameObject> _blocks = new();

    public bool Contains(Vector3Int position)
    {
        return _blocks.ContainsKey(position);
    }

    public bool TryGetBlock(
        Vector3Int position,
        out GameObject block)
    {
        return _blocks.TryGetValue(position, out block);
    }

    public bool AddBlock(
        Vector3Int position,
        GameObject block)
    {
        if (Contains(position))
        {
            return false;
        }

        _blocks.Add(position, block);

        return true;
    }

    public bool RemoveBlock(
        Vector3Int position,
        out GameObject block)
    {
        if (!_blocks.TryGetValue(position, out block))
        {
            return false;
        }

        _blocks.Remove(position);

        return true;
    }

    public void Clear()
    {
        _blocks.Clear();
    }
}