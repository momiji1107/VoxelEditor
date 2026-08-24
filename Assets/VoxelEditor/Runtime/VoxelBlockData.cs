using System;
using UnityEngine;

[Serializable]
public class VoxelBlockData
{
    [SerializeField]
    private Vector3Int _gridPosition;

    [SerializeField]
    private GameObject _prefab;

    [SerializeField]
    private Quaternion _rotation = Quaternion.identity;

    public Vector3Int GridPosition => _gridPosition;

    public GameObject Prefab => _prefab;

    public Quaternion Rotation => _rotation;

    public VoxelBlockData(
        Vector3Int gridPosition,
        GameObject prefab,
        Quaternion rotation)
    {
        _gridPosition = gridPosition;
        _prefab = prefab;
        _rotation = rotation;
    }
}