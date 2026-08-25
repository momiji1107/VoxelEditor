using UnityEngine;

namespace VoxelEditor.Runtime
{
    public static class VoxelGridUtility
    {
        public const float CellSize = 1f;

        public static Vector3Int WorldToGrid(Vector3 worldPosition)
        {
            return new Vector3Int(
                Mathf.RoundToInt(worldPosition.x / CellSize),
                Mathf.RoundToInt(worldPosition.y / CellSize),
                Mathf.RoundToInt(worldPosition.z / CellSize)
            );
        }

        public static Vector3 GridToWorld(Vector3Int gridPosition)
        {
            return new Vector3(
                gridPosition.x * CellSize,
                gridPosition.y * CellSize,
                gridPosition.z * CellSize
            );
        }
    }
}