using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.VFX;

[CustomPropertyDrawer(typeof(ParameterSelector<VisualEffect>))]
public class VisualEffectParameterSelector : ParameterSelectorEditor<VisualEffect>
{
    protected override IEnumerable<Parameter> GetParameters(VisualEffect target)
    {
        List<VFXExposedProperty> output = new();
        target.visualEffectAsset.GetExposedProperties(output);
        return output.Select(p => new Parameter(p.name, Shader.PropertyToID(p.name)));
    }
}
