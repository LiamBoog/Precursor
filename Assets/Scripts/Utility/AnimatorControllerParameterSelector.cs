using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Animations;
using UnityEngine;
using Object = UnityEngine.Object;

[Serializable]
public class ParameterSelector<T> where T : Object
{
    [field: SerializeField] public int Id { get; private set; }
    [field: SerializeField] public int ParameterIndex { get; private set; }
    [field: SerializeField] public T Target { get; private set; }


    public static implicit operator int(ParameterSelector<T> parameterSelector) => parameterSelector.Id;
}
