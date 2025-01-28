using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(ParameterSelector<Material>))]
public class MaterialPropertySelectorDrawer : ParameterSelectorDrawer<Material>
{
    protected override IEnumerable<Parameter> GetParameters(Material target)
    {
        Shader shader = target.shader;
        return Enumerable
            .Range(0, shader.GetPropertyCount())
            .Select(i => 
                {
                    string name = shader.GetPropertyName(i);
                    return new Parameter(name, Shader.PropertyToID(name));
                }
            )
            .ToArray();
    }
}
