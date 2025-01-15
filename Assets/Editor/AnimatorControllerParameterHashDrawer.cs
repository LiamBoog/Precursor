using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

[CustomPropertyDrawer(typeof(AnimatorControllerParameterHash))]
public class AnimatorControllerParameterHashDrawer : PropertyDrawer
{
    private const float HORIZONTAL_PADDING = 5f;
    
    private static string GetBackingFieldName(string autoPropertyName) => $"<{autoPropertyName}>k__BackingField";
    
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        SerializedProperty controllerProperty = property.FindPropertyRelative(GetBackingFieldName(nameof(AnimatorControllerParameterHash.Controller)));
        SerializedProperty hashProperty = property.FindPropertyRelative(GetBackingFieldName(nameof(AnimatorControllerParameterHash.Hash)));
        SerializedProperty parameterIndexProperty = property.FindPropertyRelative(GetBackingFieldName(nameof(AnimatorControllerParameterHash.ParameterIndex)));

        (Rect controllerRect, Rect parameterSelectorRect) = GetPropertyRects(EditorGUI.PrefixLabel(position, label));
        EditorGUI.PropertyField(controllerRect, controllerProperty, GUIContent.none);
        if (controllerProperty.objectReferenceValue is AnimatorController controller)
        {
            AnimatorControllerParameter[] parameters = controller.parameters;
            GUIContent[] options = parameters
                .Select(p => new GUIContent(p.name))
                .ToArray();
            parameterIndexProperty.intValue = parameterIndexProperty.intValue == -1 ? 0 : ParameterSelectorField(options);
            hashProperty.intValue = parameters[parameterIndexProperty.intValue].nameHash;
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
