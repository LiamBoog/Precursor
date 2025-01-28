using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;

[CustomPropertyDrawer(typeof(ParameterSelector<AnimatorController>))]
public class AnimatorControllerParameterSelectorDrawer : ParameterSelectorDrawer<AnimatorController>
{
    protected override IEnumerable<Parameter> GetParameters(AnimatorController target) => target.parameters.Select(p => new Parameter(p.name, p.nameHash));
}