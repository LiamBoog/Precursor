using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

[CustomPropertyDrawer(typeof(AnimatorControllerParameterHash))]
public class AnimatorControllerParameterHashDrawer : PropertyDrawer
{
    private static string GetBackingFieldName(string autoPropertyName) => $"<{autoPropertyName}>k__BackingField";
    
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        SerializedProperty controllerProperty = property.FindPropertyRelative(GetBackingFieldName(nameof(AnimatorControllerParameterHash.Controller)));
        SerializedProperty hashProperty = property.FindPropertyRelative(GetBackingFieldName(nameof(AnimatorControllerParameterHash.Hash)));
        SerializedProperty parameterIndex = property.FindPropertyRelative(GetBackingFieldName(nameof(AnimatorControllerParameterHash.ParameterIndex)));

        property.isExpanded = EditorGUI.BeginFoldoutHeaderGroup(position, property.isExpanded, label);

        if (property.isExpanded)
        {
            EditorGUI.indentLevel++;
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(controllerProperty);
            if (controllerProperty.objectReferenceValue is AnimatorController controller)
            {
                AnimatorControllerParameter[] parameters = controller.parameters;
                GUIContent[] options = parameters.Select(p => new GUIContent(p.name)).ToArray();
                parameterIndex.intValue = EditorGUILayout.Popup(new GUIContent("Parameter"), parameterIndex.intValue, options);
                hashProperty.intValue = parameters[parameterIndex.intValue].nameHash;
                Debug.Log((hashProperty.intValue, Animator.StringToHash(parameters[parameterIndex.intValue].name)));
            }
            if (EditorGUI.EndChangeCheck())
            {
                Debug.Log(parameterIndex.intValue);
            }
            EditorGUI.indentLevel--;
        }
        EditorGUI.EndFoldoutHeaderGroup();
        EditorGUI.EndProperty();
    }
}
