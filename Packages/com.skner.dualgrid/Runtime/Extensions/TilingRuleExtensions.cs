using System.Collections.Generic;
using System.Linq;
using skner.DualGrid.Utils;
using UnityEngine;
using static UnityEngine.RuleTile;

namespace skner.DualGrid.Extensions
{
    public static class TilingRuleExtensions
    {
        private readonly static Dictionary<TilingRule, Dictionary<Vector3Int, int>> _neighborIndexCache = new();
        
        /// <summary>
        /// Calculates the relative neighbor offset from the <paramref name="dataTileOffset"/> and returns the correct index of the neighbor.
        /// </summary>
        /// <param name="rule"></param>
        /// <param name="dataTileOffset"></param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentException"></exception>
        public static int GetNeighborIndex(this TilingRule rule, Vector3Int dataTileOffset)
        {
            Vector3Int neighborOffsetPosition = DualGridUtils.ConvertDataTileOffsetToNeighborOffset(dataTileOffset);

            var neightborIndex = rule.m_NeighborPositions.IndexOf(neighborOffsetPosition);

            if (neightborIndex == -1) throw new System.ArgumentException($"Could not find a valid neighbor for tile id {rule.m_Id} with the data tile offset of {dataTileOffset}.");
            return neightborIndex;
        }
        
        public static Dictionary<Vector3Int, int> GetNeighborsCached(this TilingRule rule)
        {
            if (_neighborIndexCache.TryGetValue(rule, out Dictionary<Vector3Int, int> output))
                return output;

            output = rule.GetNeighbors();
            _neighborIndexCache.Add(rule, output);
            
            return output;
        }
    }
}
