using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.RuleTile;

namespace skner.DualGrid.Extensions
{
    public static class TilingRuleExtensions
    {
        private static readonly Dictionary<TilingRule, Dictionary<Vector3Int, int>> _neighborIndexCache = new();
        
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
