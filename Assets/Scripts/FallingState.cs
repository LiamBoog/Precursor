using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public abstract partial class MovementState
{
    protected KinematicSegment<float>[] FallingCurve(float t, float targetVelocity, float gravity, ref KinematicState<float> kinematics)
    {
        List<KinematicSegment<float>> output = new();
        
        kinematics.velocity = Mathf.Max(targetVelocity, kinematics.velocity);
        
        output.Add(AccelerateTowardTargetVelocity(ref t, targetVelocity, gravity, ref kinematics));
        output.Add(LinearMotionCurve(t, ref kinematics));
        return output.ToArray();
    }

    protected KinematicSegment<float>[] FreeFallingCurve(float t, ref KinematicState<float> kinematics)
    {
        return FallingCurve(t, -parameters.TerminalVelocity, parameters.FallGravity, ref kinematics);
    }
}

public class FallingState : MovementState
{
    private float gravity;
    
    public FallingState(MovementParameters movementParameters, IPlayerInfo playerInfo, float gravity) : base(movementParameters, playerInfo)
    {
        this.gravity = gravity;
    }

    public override MovementState ProcessInterrupts(ref KinematicState<Vector2> kinematics, IEnumerable<IInterrupt> interrupts)
    {
        if (interrupts.FirstOrDefault(i => i is ICollision) is ICollision collision)
        {
            kinematics.velocity.y = collision.Deflection.y != 0f ? 0f : kinematics.velocity.y;
            kinematics.velocity.x = collision.Deflection.x != 0f ? 0f : kinematics.velocity.x;
            
            if (Vector2.Dot(collision.Normal, Vector2.up) > 0.5f) // Collision with ground
            {
                if (player.JumpBuffer.Flush())
                    return new JumpingState(parameters, player, kinematics);
                
                return new WalkingState(parameters, player);
            }
            
            if (collision.Normal.x != 0)
            {
                if (player.JumpBuffer.Flush())
                    return new WallJumpState(parameters, player, Math.Sign(collision.Normal.x), kinematics);

                if (player.WallCheck() * player.Aim.x < 0f)
                    return new WallSlideState(parameters, player);
            }
        }
        
        if (interrupts.Any(i => i is JumpInterrupt { type: JumpInterrupt.Type.Started }) && player.WallCheck() is int normal && normal != 0)
        {
            return new WallJumpState(parameters, player, normal, kinematics);
        }

        return this;
    }

    public override MovementState UpdateKinematics(ref float t, ref KinematicState<Vector2> kinematics, out KinematicSegment<Vector2>[] motion)
    {
        motion = ApplyMotionCurves(t, ref kinematics, WalkingCurve, 
            (float t, ref KinematicState<float> kinematics) => FallingCurve(t, -parameters.TerminalVelocity, gravity, ref kinematics));
        t = 0f;

        return this;
    }
}
