using System;
using UnityEditor.Animations;
using UnityEngine;

[Serializable]
public class AnimatorControllerParameterHash
{
    [field: SerializeField] public int Hash { get; private set; }
    [field: SerializeField] public int ParameterIndex { get; private set; }
    [field: SerializeField] public AnimatorController Controller { get; private set; }

    public static implicit operator int(AnimatorControllerParameterHash parameterHash) => parameterHash.Hash;
}
