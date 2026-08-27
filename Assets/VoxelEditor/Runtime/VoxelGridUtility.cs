using UnityEngine;

namespace VoxelEditor.Runtime
{
    public static class VoxelGridUtility
    {
        /// <summary>
        /// Converts a world position to a grid position.
        /// ワールド座標をGrid座標へ変換します。
        /// </summary>
        public static Vector3Int WorldToGrid(
            Vector3 worldPosition,
            float cellSize)
        {
            return new Vector3Int(
                Mathf.RoundToInt(worldPosition.x / cellSize),
                Mathf.RoundToInt(worldPosition.y / cellSize),
                Mathf.RoundToInt(worldPosition.z / cellSize)
            );
        }

        /// <summary>
        /// Converts a grid position to a world position.
        /// Grid座標をワールド座標へ変換します。
        /// </summary>
        public static Vector3 GridToWorld(
            Vector3Int gridPosition,
            float cellSize)
        {
            return new Vector3(
                gridPosition.x * cellSize,
                gridPosition.y * cellSize,
                gridPosition.z * cellSize
            );
        }

        /// <summary>
        /// Rotates a local grid offset around GridPosition.
        /// GridPositionを回転中心としてローカルGrid座標を回転します。
        /// </summary>
        public static Vector3Int RotateGridOffset(
            Vector3Int localPosition,
            Quaternion rotation)
        {
            Vector3 rotatedPosition = rotation * localPosition;

            return Vector3Int.RoundToInt(rotatedPosition);
        }

        /// <summary>
        /// Gets the occupied Grid position of a local cell after rotation.
        /// 回転後のローカルセルが実際に占有するGrid座標を取得します。
        /// </summary>
        public static Vector3Int GetRotatedGridPosition(
            Vector3Int gridPosition,
            Vector3Int localPosition,
            Quaternion rotation)
        {
            return gridPosition +
                   RotateGridOffset(localPosition, rotation);
        }

        /// <summary>
        /// Gets the minimum and maximum Grid coordinates occupied after rotation.
        /// 回転後に占有するGrid座標の最小値と最大値を取得します。
        /// </summary>
        public static void GetRotatedGridBounds(
            Vector3Int gridSize,
            Quaternion rotation,
            out Vector3Int minPosition,
            out Vector3Int maxPosition)
        {
            Vector3Int maxLocalPosition = gridSize - Vector3Int.one;

            Vector3Int[] corners =
            {
                new Vector3Int(0, 0, 0),
                new Vector3Int(maxLocalPosition.x, 0, 0),
                new Vector3Int(0, maxLocalPosition.y, 0),
                new Vector3Int(0, 0, maxLocalPosition.z),

                new Vector3Int(maxLocalPosition.x, maxLocalPosition.y, 0),
                new Vector3Int(maxLocalPosition.x, 0, maxLocalPosition.z),
                new Vector3Int(0, maxLocalPosition.y, maxLocalPosition.z),
                new Vector3Int(
                    maxLocalPosition.x,
                    maxLocalPosition.y,
                    maxLocalPosition.z)
            };

            minPosition = Vector3Int.one * int.MaxValue;
            maxPosition = Vector3Int.one * int.MinValue;

            foreach (Vector3Int corner in corners)
            {
                Vector3Int rotatedCorner =
                    RotateGridOffset(corner, rotation);

                minPosition = Vector3Int.Min(minPosition, rotatedCorner);
                maxPosition = Vector3Int.Max(maxPosition, rotatedCorner);
            }
        }

        /// <summary>
        /// Gets the GridSize after rotation.
        /// 回転後のGridSizeを取得します。
        /// </summary>
        public static Vector3Int GetRotatedGridSize(
            Vector3Int gridSize,
            Quaternion rotation)
        {
            GetRotatedGridBounds(
                gridSize,
                rotation,
                out Vector3Int minPosition,
                out Vector3Int maxPosition);

            return maxPosition - minPosition + Vector3Int.one;
        }

        /// <summary>
        /// Gets the world position of the Prefab pivot.
        /// GridPositionを固定した状態でPrefabのTransform位置を取得します。
        ///
        /// PrefabのTransform原点は、元のGridSizeの中心にあることを前提とします。
        /// </summary>
        public static Vector3 GridToWorldCenter(
            Vector3Int gridPosition,
            Vector3Int originalGridSize,
            Quaternion rotation,
            float cellSize)
        {
            Vector3 gridWorldPosition =
                GridToWorld(gridPosition, cellSize);

            Vector3 localCenterOffset =
                new Vector3(
                    (originalGridSize.x - 1) * 0.5f,
                    (originalGridSize.y - 1) * 0.5f,
                    (originalGridSize.z - 1) * 0.5f
                );

            return gridWorldPosition +
                   rotation * localCenterOffset * cellSize;
        }
    }
}