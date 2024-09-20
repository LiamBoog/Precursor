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

public abstract partial class MovementState
{
    protected delegate KinematicSegment<float>[] MotionCurve(float t, ref KinematicState<float> kinematics);
    
    protected MovementParameters parameters;
    protected IPlayerInfo player;

    protected MovementState(MovementParameters movementParameters, IPlayerInfo playerInfo)
    {
        parameters = movementParameters;
        player = playerInfo;
    }

    protected abstract MovementState Update(float t, ref KinematicState<Vector2> kinematics, IEnumerable<IInterrupt> interrupts);


    public MovementState Update(float t, ref KinematicState<Vector2> kinematics, List<IInterrupt> interrupts)
    {
        try
        {
            return Update(t, ref kinematics, (IEnumerable<IInterrupt>) interrupts);
        }
        finally
        {
            interrupts.Clear();
        }
    }

    protected void ApplyMotionCurves(float t, ref KinematicState<Vector2> kinematics, MotionCurve horizontal, MotionCurve vertical)
    {
        KinematicState<float> xKinematics = new(kinematics.position.x, kinematics.velocity.x);
        KinematicState<float> yKinematics = new(kinematics.position.y, kinematics.velocity.y);

        horizontal(t, ref xKinematics);
        vertical(t, ref yKinematics);
        kinematics = new(
            new(xKinematics.position, yKinematics.position), 
            new(xKinematics.velocity, yKinematics.velocity));
    }
}
