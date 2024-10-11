using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
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

    public virtual MovementState ProcessInterrupts(ref KinematicState<Vector2> kinematics, IEnumerable<IInterrupt> interrupts)
    {
        if (interrupts.Any(i => i is AnchorInterrupt) && player.GrappleRaycast(out Vector2 anchor))
        {
            return new AnchoredState(parameters, player, anchor, this);
        }
        
        // Process collisions
        if (interrupts.FirstOrDefault(i => i is ICollision) is ICollision collision)
        {
            Vector2 normal = collision.Normal;
            // TODO - Might make sense to set a callback to do this in UpdateKinematics instead of here
            kinematics.velocity.y = normal.y != 0f ? 0f : kinematics.velocity.y;
            kinematics.velocity.x = normal.x != 0f ? 0f : kinematics.velocity.x;
            
            if (normal.y > 0f)
            {
                if (player.JumpBuffer.Flush())
                    return new JumpingState(parameters, player, kinematics);

                if (this is not WalkingState && this is not AnchoredState)
                    return new WalkingState(parameters, player);
            }
            
            if (collision.Normal.x != 0)
            {
                if (player.JumpBuffer.Flush())
                    return new WallJumpState(parameters, player, Math.Sign(collision.Normal.x), kinematics);
            }
        }

        return this;
    }
    
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
        while (i < x?.Length && j < y?.Length)
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
                ContractLongerSegment(x[i++], ref y[j]);
            }
            else if (x[i].duration > y[j].duration)
            {
                ContractLongerSegment(y[j++], ref x[i]);
            }
            else
            {
                i++;
                j++;
            }
        }

        return output.ToArray();

        void ContractLongerSegment(KinematicSegment<float> shorter, ref KinematicSegment<float> longer)
        {
            KinematicState<float> newInitialState = longer.initialState;
            AccelerationCurve(shorter.duration, ref newInitialState, longer.acceleration);
            longer = new(
                newInitialState,
                longer.acceleration,
                longer.duration - shorter.duration
            );
        }
    }

    protected static KinematicSegment<float> AccelerationCurve(float t, ref KinematicState<float> kinematics, float acceleration)
    {
        KinematicSegment<float> output = new(kinematics, acceleration, t);
        
        kinematics.position += kinematics.velocity * t + 0.5f * acceleration * t * t;
        kinematics.velocity += acceleration * t;
        
        return output;
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
