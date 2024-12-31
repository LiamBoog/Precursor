using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using skner.DualGrid.Extensions;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.VirtualTexturing;

namespace skner.DualGrid.Editor
{
    [CustomEditor(typeof(DualGridRuleTile), true)]
    public class DualGridRuleTileEditor : RuleTileEditor
    {
        private SerializedProperty m_TilingRules;

        public override void OnEnable()
        {
            base.OnEnable();
            m_TilingRules = serializedObject.FindProperty(nameof(m_TilingRules));
        }

        public override BoundsInt GetRuleGUIBounds(BoundsInt bounds, RuleTile.TilingRule rule)
        {
            return new BoundsInt(-1, -1, 0, 2, 2, 0);
        }

        public override Vector2 GetMatrixSize(BoundsInt bounds)
        {
            float matrixCellSize = 27;
            return new Vector2(bounds.size.x * matrixCellSize, bounds.size.y * matrixCellSize);
        }

        public override void RuleMatrixOnGUI(RuleTile tile, Rect rect, BoundsInt bounds, RuleTile.TilingRule tilingRule)
        {
            // This code was copied from the base RuleTileEditor.RuleMatrixOnGUI, because there are no good ways to extend it.
            // The changes were marked with a comment

            Handles.color = EditorGUIUtility.isProSkin ? new Color(1f, 1f, 1f, 0.2f) : new Color(0f, 0f, 0f, 0.2f);
            float w = rect.width / bounds.size.x;
            float h = rect.height / bounds.size.y;

            for (int y = 0; y <= bounds.size.y; y++)
            {
                float top = rect.yMin + y * h;
                Handles.DrawLine(new Vector3(rect.xMin, top), new Vector3(rect.xMax, top));
            }

            for (int x = 0; x <= bounds.size.x; x++)
            {
                float left = rect.xMin + x * w;
                Handles.DrawLine(new Vector3(left, rect.yMin), new Vector3(left, rect.yMax));
            }

            Handles.color = Color.white;

            var neighbors = tilingRule.GetNeighbors();

            // Incremented for cycles by 1 to workaround new GetBounds(), while perserving corner behaviour
            for (int y = -1; y < 1; y++)
            {
                for (int x = -1; x < 1; x++)
                {
                    // Pos changed here to workaround for the new 2x2 matrix, only considering the corners, while not changing the Rect r
                    Vector3Int pos = new Vector3Int(x == 0 ? 1 : x, y == 0 ? 1 : y, 0);

                    Rect r = new Rect(rect.xMin + (x - bounds.xMin) * w, rect.yMin + (-y + bounds.yMax - 1) * h, w - 1, h - 1);
                    RuleMatrixIconOnGUI(tilingRule, neighbors, pos, r);
                }
            }
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            
            EditorGUI.BeginChangeCheck();
            InjectDualGridTilingRules();
            if (!EditorGUI.EndChangeCheck())
                return;
            
            SaveTile();
        }

        private void InjectDualGridTilingRules()
        {
            Undo.RecordObject(serializedObject.targetObject, "Inject DualGridTilingRules");
            
            FieldInfo tilingRulesField = typeof(RuleTile).GetField(nameof(m_TilingRules), BindingFlags.Public | BindingFlags.Instance);
            List<RuleTile.TilingRule> tilingRules = tilingRulesField.GetValue(serializedObject.targetObject) as List<RuleTile.TilingRule>;
            tilingRules = tilingRules.Select(rule =>
            {
                DualGridRuleTile.DualGridTilingRule output = rule.CloneToDualGridTilingRule();
                SerializedDictionary<Vector3Int, int> neighbours = new();
                foreach (var (key, value) in rule.GetNeighbors())
                {
                    neighbours.Add(key, value);
                }
                output.GetType().GetProperty(nameof(DualGridRuleTile.DualGridTilingRule.Neighbours))!.SetValue(output, neighbours);
                return (RuleTile.TilingRule) output;
            })
            .ToList();
            
            tilingRulesField.SetValue(serializedObject.targetObject, tilingRules);
        }
    }
    
    public static class SerializedPropertyExtensions
    {
        private delegate FieldInfo GetFieldInfoAndStaticTypeFromProperty(SerializedProperty aProperty, out Type aType);
        private static GetFieldInfoAndStaticTypeFromProperty m_GetFieldInfoAndStaticTypeFromProperty;

        public static FieldInfo GetFieldInfoAndStaticType(this SerializedProperty prop, out Type type)
        {
            if (m_GetFieldInfoAndStaticTypeFromProperty == null)
            {
                foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    foreach (var t in assembly.GetTypes())
                    {
                        if (t.Name == "ScriptAttributeUtility")
                        {
                            MethodInfo mi = t.GetMethod("GetFieldInfoAndStaticTypeFromProperty", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                            m_GetFieldInfoAndStaticTypeFromProperty = (GetFieldInfoAndStaticTypeFromProperty)Delegate.CreateDelegate(typeof(GetFieldInfoAndStaticTypeFromProperty), mi);
                            break;
                        }
                    }
                    if (m_GetFieldInfoAndStaticTypeFromProperty != null) break;
                }
                if (m_GetFieldInfoAndStaticTypeFromProperty == null)
                {
                    UnityEngine.Debug.LogError("GetFieldInfoAndStaticType::Reflection failed!");
                    type = null;
                    return null;
                }
            }
            return m_GetFieldInfoAndStaticTypeFromProperty(prop, out type);
        }

        public static T GetCustomAttributeFromProperty<T>(this SerializedProperty prop) where T : System.Attribute
        {
            var info = prop.GetFieldInfoAndStaticType(out _);
            return info.GetCustomAttribute<T>();
        }
        
        /// <summary>
    /// Gets the target value of a SerializedProperty.
    /// </summary>
    /// <typeparam name="T">The type of the target value.</typeparam>
    /// <param name="property">The SerializedProperty.</param>
    /// <returns>The target value.</returns>
    public static T GetTargetValue<T>(this SerializedProperty property)
    {
        object targetObject = GetTargetObjectOfProperty(property);
        return (T)targetObject;
    }

    /// <summary>
    /// Sets the target value of a SerializedProperty.
    /// </summary>
    /// <typeparam name="T">The type of the target value.</typeparam>
    /// <param name="property">The SerializedProperty.</param>
    /// <param name="value">The value to set.</param>
    public static void SetTargetValue<T>(this SerializedProperty property, T value)
    {
        object targetObject = GetTargetObjectOfProperty(property);
        if (targetObject == null) return;

        var path = property.propertyPath.Replace(".Array.data[", "[");
        var elements = path.Split('.');

        foreach (var element in elements)
        {
            if (element.Contains("["))
            {
                var elementName = element.Substring(0, element.IndexOf("["));
                var index = Convert.ToInt32(element.Substring(element.IndexOf("[")).Replace("[", "").Replace("]", ""));
                targetObject = GetValue(targetObject, elementName, index);
            }
            else
            {
                targetObject = GetValue(targetObject, element);
            }
        }

        if (targetObject == null) return;

        var fieldName = elements[^1];
        var field = targetObject.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
        if (field != null)
        {
            field.SetValue(targetObject, value);
        }
        else
        {
            var propertyInfo = targetObject.GetType().GetProperty(fieldName, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
            if (propertyInfo != null)
            {
                propertyInfo.SetValue(targetObject, value);
            }
        }
    }

    private static object GetTargetObjectOfProperty(SerializedProperty prop)
    {
        if (prop == null) return null;

        var path = prop.propertyPath.Replace(".Array.data[", "[");
        object obj = prop.serializedObject.targetObject;
        var elements = path.Split('.');

        foreach (var element in elements)
        {
            if (element.Contains("["))
            {
                var elementName = element.Substring(0, element.IndexOf("["));
                var index = Convert.ToInt32(element.Substring(element.IndexOf("[")).Replace("[", "").Replace("]", ""));
                obj = GetValue(obj, elementName, index);
            }
            else
            {
                obj = GetValue(obj, element);
            }
        }

        return obj;
    }

    private static object GetValue(object source, string name)
    {
        if (source == null)
            return null;

        var type = source.GetType();
        var field = type.GetField(name, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
        if (field == null)
        {
            var property = type.GetProperty(name, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
            if (property == null)
                return null;

            return property.GetValue(source, null);
        }

        return field.GetValue(source);
    }

    private static object GetValue(object source, string name, int index)
    {
        var enumerable = GetValue(source, name) as System.Collections.IEnumerable;
        if (enumerable == null) return null;

        var enm = enumerable.GetEnumerator();
        while (index-- >= 0)
            if (!enm.MoveNext()) return null;

        return enm.Current;
    }
    }
}

