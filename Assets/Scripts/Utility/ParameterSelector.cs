using System;
using UnityEngine;
using UnityEngine.VFX;
using Object = UnityEngine.Object;

[Serializable]
public abstract class ParameterSelector<T> where T : Object
{
    [field: SerializeField] public string Name { get; private set; }
    [field: SerializeField] public int ParameterIndex { get; private set; }
    [field: SerializeField] public T Target { get; private set; }

    public int x;
    
    public int Id => GetParameterHash(Name);

    protected abstract int GetParameterHash(string name);

    public static implicit operator int(ParameterSelector<T> parameterSelector) => parameterSelector.Id;
}

[Serializable] 
public class AnimatorParameterSelector : ParameterSelector<Animator>
{
    protected override int GetParameterHash(string name) => Animator.StringToHash(name);
}

[Serializable]
public class MaterialPropertySelector : ParameterSelector<Material>
{
    protected override int GetParameterHash(string name) => Shader.PropertyToID(name);
}

[Serializable]
public class VisualEffectPropertySelector : ParameterSelector<VisualEffect>
{
    protected override int GetParameterHash(string name) => Shader.PropertyToID(name);
}
