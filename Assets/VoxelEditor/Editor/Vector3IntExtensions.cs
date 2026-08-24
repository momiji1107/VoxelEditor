using UnityEngine;

namespace VoxelEditor.Editor
{
    /// <summary>
    /// Vector3Intの拡張メソッド
    /// </summary>
    public static class Vector3IntExtensions
    {
        /// <summary>
        /// a.Dot(b)で呼び出し
        /// </summary>
        /// <returns> a・b (aとbの内積) </returns>
        public static int Dot(this Vector3Int a, Vector3Int b)
        {
            return a.x * b.x + a.y * b.y + a.z * b.z;
        }
    }


}