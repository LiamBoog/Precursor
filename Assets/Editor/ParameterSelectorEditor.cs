using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public abstract class ParameterSelectorEditor<T> : PropertyDrawer where T : Object
{
    protected struct Parameter
    {
        public string Name { get; }
        public int Hash { get; }

        public Parameter(string name, int hash)
        {
            Name = name;
            Hash = hash;
        }
    }
    
    private const float HORIZONTAL_PADDING = 5f;

    protected abstract IEnumerable<Parameter> GetParameters(T target);
    
    protected static string GetBackingFieldName(string autoPropertyName) => $"<{autoPropertyName}>k__BackingField";

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);
        
        SerializedProperty targetProperty = property.FindPropertyRelative(GetBackingFieldName(nameof(ParameterSelector<T>.Target)));
        SerializedProperty hashProperty = property.FindPropertyRelative(GetBackingFieldName(nameof(ParameterSelector<T>.Id)));
        SerializedProperty parameterIndexProperty = property.FindPropertyRelative(GetBackingFieldName(nameof(ParameterSelector<T>.ParameterIndex)));

        (Rect controllerRect, Rect parameterSelectorRect) = GetPropertyRects(EditorGUI.PrefixLabel(position, label));
        EditorGUI.PropertyField(controllerRect, targetProperty, GUIContent.none);
        if (targetProperty.objectReferenceValue is T target)
        {
            IEnumerable<Parameter> parameters = GetParameters(target);
            GUIContent[] options = parameters
                .Select(p => new GUIContent(p.Name))
                .ToArray();
            parameterIndexProperty.intValue = parameterIndexProperty.intValue == -1 ? 0 : ParameterSelectorField(options);
            hashProperty.intValue = parameters.ElementAt(parameterIndexProperty.intValue).Hash;
        }
        else
        {
            ParameterSelectorField();
            parameterIndexProperty.intValue = -1;
            hashProperty.intValue = -1;
        }
        
        EditorGUI.EndProperty();

        int ParameterSelectorField(GUIContent[] options = null)
        {
            return EditorGUI.Popup(parameterSelectorRect, GUIContent.none, parameterIndexProperty.intValue, options ?? new GUIContent[] { });
        }
    }

    private (Rect, Rect) GetPropertyRects(Rect position)
    {
        Rect controllerRect = position;
        controllerRect.width = controllerRect.width / 2f - HORIZONTAL_PADDING;
        
        Rect parameterSelectorRect = position;
        parameterSelectorRect.width /= 2f;
        parameterSelectorRect.position += position.width / 2f * Vector2.right;
        
        return (controllerRect, parameterSelectorRect);
    }
}
