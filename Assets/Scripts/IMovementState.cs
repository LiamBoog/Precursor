using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public readonly struct KinematicSegment<T> where T : struct, IEquatable<T>, IFormattable
{
    public readonly KinematicState<T> initialState;
    public readonly T acceleration;
    public readonly T duration;

    public KinematicSegment(KinematicState<T> initialState, T acceleration, T duration)
    {
        this.initialState = initialState;
        this.acceleration = acceleration;
        this.duration = duration;
    }
}

public struct KinematicState<T> where T : struct, IEquatable<T>, IFormattable
{
    public T position;
    public T velocity;

    public KinematicState(T position, T velocity)
    {
        this.position = position;
        this.velocity = velocity;
    }
}

public interface IMovementState
{
    IMovementState Update(float t, ref KinematicState<Vector2> kinematics, IEnumerable<IInterrupt> interrupts = default);
}
