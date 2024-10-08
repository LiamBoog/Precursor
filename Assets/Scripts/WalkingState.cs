using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public abstract partial class MovementState
{
    protected KinematicSegment<float>[] WalkingCurve(float t, ref KinematicState<float> kinematics)
    {
        List<KinematicSegment<float>> output = new();
        float targetVelocity = parameters.TopSpeed * player.Aim.x;

        float decelerationTarget = kinematics.velocity * targetVelocity < 0f ? 0f : targetVelocity;
        if (Mathf.Abs(decelerationTarget) < Mathf.Abs(kinematics.velocity))
        {
            output.Add(AccelerateTowardTargetVelocity(ref t, decelerationTarget, parameters.Deceleration, ref kinematics));
        }

        output.Add(AccelerateTowardTargetVelocity(ref t, targetVelocity, parameters.Acceleration, ref kinematics));
        output.Add(LinearMotionCurve(t, ref kinematics));
        return output.ToArray();
    }
}

public class WalkingState : MovementState
{
    public WalkingState(MovementParameters movementParameters, IPlayerInfo playerInfo) : base(movementParameters, playerInfo) { }

    public override MovementState ProcessInterrupts(ref KinematicState<Vector2> kinematics, IEnumerable<IInterrupt> interrupts)
    {
        // Process inputs
        if (interrupts.LastOrDefault(i => i is not ICollision) is { } interrupt)
        {
            switch (interrupt)
            {
                case JumpInterrupt jumpInterrupt:
                    if (jumpInterrupt.type == JumpInterrupt.Type.Cancelled) 
                        break;
                    if (CanJump(kinematics))
                        return new JumpingState(parameters, player, kinematics);
                    break;
            }
        }
        
        // Process collisions
        if (interrupts.FirstOrDefault(i => i is ICollision) is ICollision collision)
        {
            Vector2 deflection = collision.Deflection;
            // TODO - Might make sense to set a callback to do this in UpdateKinematics instead of here
            kinematics.velocity.y = deflection.y != 0f ? 0f : kinematics.velocity.y;
            kinematics.velocity.x = deflection.x != 0f ? 0f : kinematics.velocity.x;
            
            if (deflection.y > 0f)
            {
                if (player.JumpBuffer.Flush())
                    return new JumpingState(parameters, player, kinematics);
            }
            else if (deflection.x != 0f)
            {
                if (player.JumpBuffer.Flush())
                    return new WallJumpState(parameters, player, Math.Sign(collision.Normal.x), kinematics);

                return new WallSlideState(parameters, player);
            }
        }

        return this;
    }

    public override MovementState UpdateKinematics(ref float t, ref KinematicState<Vector2> kinematics, out KinematicSegment<Vector2>[] motion)
    {
        motion = ApplyMotionCurves(t, ref kinematics, WalkingCurve, FreeFallingCurve);
        t = 0f;

        return this;
    }
    
    private bool CanJump(KinematicState<Vector2> kinematics)
    {
        float fallTime = -kinematics.velocity.y / parameters.FallGravity;
        return fallTime < parameters.CoyoteTime || player.GroundCheck();
    }
}
