using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.VFX;

public abstract class ParameterSelectorDrawer<T> : PropertyDrawer where T : Object
{
    private const float HORIZONTAL_PADDING = 5f;

    protected abstract IEnumerable<string> GetParameters(T target);

    private static string GetBackingFieldName(string autoPropertyName) => $"<{autoPropertyName}>k__BackingField";

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);
        
        SerializedProperty targetProperty = property.FindPropertyRelative(GetBackingFieldName(nameof(ParameterSelector<T>.Target)));
        SerializedProperty nameProperty = property.FindPropertyRelative(GetBackingFieldName(nameof(ParameterSelector<T>.Name)));
        SerializedProperty parameterIndexProperty = property.FindPropertyRelative(GetBackingFieldName(nameof(ParameterSelector<T>.ParameterIndex)));

        (Rect controllerRect, Rect parameterSelectorRect) = GetPropertyRects(EditorGUI.PrefixLabel(position, label));
        EditorGUI.PropertyField(controllerRect, targetProperty, GUIContent.none);
        if (targetProperty.objectReferenceValue is T target)
        {
            IEnumerable<string> parameters = GetParameters(target);
            GUIContent[] options = parameters
                .Select(p => new GUIContent(p))
                .ToArray();
            parameterIndexProperty.intValue = parameterIndexProperty.intValue == -1 ? 0 : ParameterSelectorField(options);
            nameProperty.stringValue = parameters.ElementAt(parameterIndexProperty.intValue);
        }
        else
        {
            ParameterSelectorField();
            parameterIndexProperty.intValue = -1;
            nameProperty.stringValue = "";
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

[CustomPropertyDrawer(typeof(AnimatorParameterSelector))]
public class AnimatorControllerParameterSelectorDrawer : ParameterSelectorDrawer<Animator>
{
    protected override IEnumerable<string> GetParameters(Animator target) => target.parameters.Select(p => p.name);
}

[CustomPropertyDrawer(typeof(MaterialPropertySelector))]
public class MaterialPropertySelectorDrawer : ParameterSelectorDrawer<Material>
{
    protected override IEnumerable<string> GetParameters(Material target)
    {
        Shader shader = target.shader;
        return Enumerable
            .Range(0, shader.GetPropertyCount())
            .Select(i => shader.GetPropertyName(i))
            .ToArray();
    }
}

[CustomPropertyDrawer(typeof(VisualEffectPropertySelector))]
public class VisualEffectParameterSelectorDrawer : ParameterSelectorDrawer<VisualEffect>
{
    protected override IEnumerable<string> GetParameters(VisualEffect target)
    {
        List<VFXExposedProperty> output = new();
        target.visualEffectAsset.GetExposedProperties(output);
        return output.Select(p => p.name);
    }
}
