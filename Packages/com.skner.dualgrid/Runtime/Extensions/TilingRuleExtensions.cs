using System;
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

        public static DualGridRuleTile.DualGridTilingRule CloneToDualGridTilingRule(this TilingRule rule)
        {
            DualGridRuleTile.DualGridTilingRule output = new DualGridRuleTile.DualGridTilingRule
            {
                m_Neighbors = new List<int>(rule.m_Neighbors),
                m_NeighborPositions = new List<Vector3Int>(rule.m_NeighborPositions),
                m_RuleTransform = rule.m_RuleTransform,
                m_Sprites = new Sprite[rule.m_Sprites.Length],
                m_GameObject = rule.m_GameObject,
                m_MinAnimationSpeed = rule.m_MinAnimationSpeed,
                m_MaxAnimationSpeed = rule.m_MaxAnimationSpeed,
                m_PerlinScale = rule.m_PerlinScale,
                m_Output = rule.m_Output,
                m_ColliderType = rule.m_ColliderType,
                m_RandomTransform = rule.m_RandomTransform,
            };
            Array.Copy(rule.m_Sprites, output.m_Sprites, rule.m_Sprites.Length);
            return output;
        }
    }
}
