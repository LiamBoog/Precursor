using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public abstract partial class MovementState
{
    protected KinematicSegment<float>[] WalkingCurve(float t, ref KinematicState<float> kinematics, float input)
    {
        List<KinematicSegment<float>> output = new();
        float targetVelocity = parameters.TopSpeed * input;

        float decelerationTarget = kinematics.velocity * targetVelocity < 0f ? 0f : targetVelocity;
        if (Mathf.Abs(decelerationTarget) < Mathf.Abs(kinematics.velocity))
        {
            output.Add(AccelerateTowardTargetVelocity(ref t, decelerationTarget, parameters.Deceleration, ref kinematics));
        }

        output.Add(AccelerateTowardTargetVelocity(ref t, targetVelocity, parameters.Acceleration, ref kinematics));
        output.Add(LinearMotionCurve(t, ref kinematics));
        return output.ToArray();
    }
    
    protected KinematicSegment<float> AccelerateTowardTargetVelocity(ref float t, float targetVelocity, float accelerationMagnitude, ref KinematicState<float> kinematics)
    {
        float acceleration = Math.Sign(targetVelocity - kinematics.velocity) * accelerationMagnitude;
        float maxAccelerationTime = acceleration == 0f ? 0f : (targetVelocity - kinematics.velocity) / acceleration;
        float accelerationTime = Mathf.Min(t, maxAccelerationTime);

        KinematicSegment<float> output = new(kinematics, acceleration, accelerationTime);
        kinematics.position += kinematics.velocity * accelerationTime + 0.5f * acceleration * accelerationTime * accelerationTime;
        kinematics.velocity = accelerationTime < maxAccelerationTime ? kinematics.velocity + acceleration * accelerationTime : targetVelocity;
        t -= accelerationTime;
        
        return output;
    }

    protected KinematicSegment<float> LinearMotionCurve(float t, ref KinematicState<float> kinematics)
    {
        KinematicSegment<float> output = new(kinematics, 0f, t);
        kinematics.position += kinematics.velocity * t;
        return output;
    }
    
    protected KinematicSegment<float>[] FallingCurve(float t, ref KinematicState<float> kinematics, float input, Func<int> wallCheck)
    {
        List<KinematicSegment<float>> output = new();

        bool aimingAtWall = input != 0f && wallCheck() * input < 0f; // TODO - maybe replace this with a wall sliding state
        float targetVelocity = aimingAtWall ? -parameters.WallSlideVelocity : -parameters.TerminalVelocity;
        kinematics.velocity = Mathf.Max(targetVelocity, kinematics.velocity);
        
        output.Add(AccelerateTowardTargetVelocity(ref t, targetVelocity, parameters.FallGravity, ref kinematics));
        output.Add(LinearMotionCurve(t, ref kinematics));
        return output.ToArray();
    }
}

public class WalkingState : MovementState
{
    public WalkingState(MovementParameters movementParameters, IPlayerInfo playerInfo) : base(movementParameters, playerInfo) { }

    protected override MovementState Update(ref float t, ref KinematicState<Vector2> kinematics, IEnumerable<IInterrupt> interrupts)
    {
        // Process inputs
        if (interrupts.LastOrDefault(i => i is not ICollision) is { } interrupt)
        {
            switch (interrupt)
            {
                case JumpInterrupt jumpInterrupt:
                    if (jumpInterrupt.type == JumpInterrupt.Type.Cancelled || !CanJump(kinematics))
                        break;
                    Debug.Log("JUMP");
                    return new JumpingState(kinematics, parameters, player);
            }
        }
        
        // Process collisions
        if (interrupts.Any(i => i is ICollision))
        {
            Vector2 deflection = ((ICollision) interrupts.First(i => i is ICollision)).Deflection;
            if (deflection.x != 0f)
            {
                kinematics.velocity.x = 0f;
            }
            else
            {
                kinematics.velocity.y = 0f;
            }
        }

        KinematicState<float> xKinematics = new(kinematics.position.x, kinematics.velocity.x);
        KinematicState<float> yKinematics = new(kinematics.position.y, kinematics.velocity.y);
        float xInput = player.Aim.x;
        
        WalkingCurve(t, ref xKinematics, xInput);
        FallingCurve(t, ref yKinematics, xInput, player.WallCheck);
        kinematics = new(
            new(xKinematics.position, yKinematics.position), 
            new(xKinematics.velocity, yKinematics.velocity));
        t = 0f;

        return this;
    }
}
