using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public readonly struct KinematicSegment<T> where T : struct, IEquatable<T>, IFormattable
{
    public readonly KinematicState<T> initialState;
    public readonly T acceleration;
    public readonly float duration;

    public KinematicSegment(KinematicState<T> initialState, T acceleration, float duration)
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

    public abstract MovementState ProcessInterrupts(ref KinematicState<Vector2> kinematics, IEnumerable<IInterrupt> interrupts);
    
    public abstract MovementState UpdateKinematics(ref float t, ref KinematicState<Vector2> kinematics, out KinematicSegment<Vector2>[] motion);

    protected KinematicSegment<Vector2>[] ApplyMotionCurves(float t, ref KinematicState<Vector2> kinematics, MotionCurve horizontal = null, MotionCurve vertical = null)
    {
        KinematicState<float> xKinematics = new(kinematics.position.x, kinematics.velocity.x);
        KinematicState<float> yKinematics = new(kinematics.position.y, kinematics.velocity.y);

        KinematicSegment<float>[] xMotion = horizontal?.Invoke(t, ref xKinematics);
        KinematicSegment<float>[] yMotion = vertical?.Invoke(t, ref yKinematics);
        kinematics = new(
            new(xKinematics.position, yKinematics.position), 
            new(xKinematics.velocity, yKinematics.velocity));

        return MergeKinematicSegments(xMotion, yMotion);
    }

    protected static KinematicSegment<Vector2>[] MergeKinematicSegments(KinematicSegment<float>[] x, KinematicSegment<float>[] y)
    {
        List<KinematicSegment<Vector2>> output = new();

        int i = 0, j = 0;
        while (i < x.Length && j < y.Length)
        {
            output.Add(new(
                new KinematicState<Vector2>(
                    new Vector2(
                        x[i].initialState.position,
                        y[j].initialState.position),
                    new Vector2(
                        x[i].initialState.velocity,
                        y[j].initialState.velocity)
                ),
                new(x[i].acceleration, y[j].acceleration),
                Mathf.Min(x[i].duration, y[j].duration))
            );
            
            if (x[i].duration < y[j].duration)
            {
                KinematicState<float> newInitialState = y[j].initialState;
                AccelerationCurve(x[i].duration, ref newInitialState, y[j].acceleration);
                y[j] = new(
                    newInitialState,
                    y[j].acceleration,
                    y[j].duration - x[i++].duration
                );
            }
            else if (x[i].duration > y[j].duration)
            {
                KinematicState<float> newInitialState = x[i].initialState;
                AccelerationCurve(y[j].duration, ref newInitialState, x[i].acceleration);
                x[i] = new(
                    newInitialState,
                    x[i].acceleration,
                    x[i].duration - y[j++].duration
                );
            }
            else
            {
                i++;
                j++;
            }
        }

        return output.ToArray();
    }

    protected static KinematicSegment<float> AccelerationCurve(float t, ref KinematicState<float> kinematics, float acceleration)
    {
        kinematics.position += kinematics.velocity * t + 0.5f * acceleration * t * t;
        kinematics.velocity += acceleration * t;
        
        return new(kinematics, acceleration, t);
    }
    
    protected KinematicSegment<float> AccelerateTowardTargetVelocity(ref float t, float targetVelocity, float accelerationMagnitude, ref KinematicState<float> kinematics)
    {
        float acceleration = Math.Sign(targetVelocity - kinematics.velocity) * accelerationMagnitude;
        float maxAccelerationTime = acceleration == 0f ? 0f : (targetVelocity - kinematics.velocity) / acceleration;
        float accelerationTime = Mathf.Min(t, maxAccelerationTime);

        /*KinematicSegment<float> output = new(kinematics, acceleration, accelerationTime);
        kinematics.position += kinematics.velocity * accelerationTime + 0.5f * acceleration * accelerationTime * accelerationTime;
        kinematics.velocity = accelerationTime < maxAccelerationTime ? kinematics.velocity + acceleration * accelerationTime : targetVelocity;*/
        t -= accelerationTime;
        return AccelerationCurve(accelerationTime, ref kinematics, acceleration);
        
        //return output;
    }

    protected KinematicSegment<float> LinearMotionCurve(float t, ref KinematicState<float> kinematics)
    {
        KinematicSegment<float> output = new(kinematics, 0f, t);
        kinematics.position += kinematics.velocity * t;
        return output;
    }
}
