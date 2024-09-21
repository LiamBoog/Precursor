using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class FallingState : MovementState
{
    private float gravity;
    
    public FallingState(MovementParameters movementParameters, IPlayerInfo playerInfo, float gravity) : base(movementParameters, playerInfo)
    {
        this.gravity = gravity;
    }
    
    protected override MovementState Update(ref float t, ref KinematicState<Vector2> kinematics, IEnumerable<IInterrupt> interrupts)
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
        
        ApplyMotionCurves(t, ref kinematics, WalkingCurve, 
            (float t, ref KinematicState<float> kinematics) => FallingCurve(t, -parameters.TerminalVelocity, gravity, ref kinematics));
        t = 0f;

        return this;
    }
}
