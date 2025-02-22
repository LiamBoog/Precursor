using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(ParameterSelector<Animator>))]
public class AnimatorControllerParameterSelectorDrawer : ParameterSelectorDrawer<Animator>
{
    protected override IEnumerable<Parameter> GetParameters(Animator target) => target.parameters.Select(p => new Parameter(p.name, p.nameHash));
}