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

    protected bool TryWallJump(KinematicState<Vector2> kinematics, IEnumerable<IInterrupt> interrupts, out MovementState newState)
    {
        if (interrupts.Any(i => i is JumpInterrupt { type: JumpInterrupt.Type.Started }) && player.WallCheck() is int normal && normal != 0)
        {
            newState = new WallJumpState(parameters, player, normal, kinematics);
            return true;
        }

        newState = this;
        return false;
    }

    protected bool TryWallSlide(IEnumerable<IInterrupt> interrupts, out MovementState newState)
    {
        if (interrupts.FirstOrDefault(i => i is ICollision) is ICollision collision && collision.Normal.x != 0f && player.WallCheck() * player.Aim.x < 0f)
        {
            newState = new WallSlideState(parameters, player);
            return true;
        }

        newState = this;
        return false;
    }
}

public class FallingState : MovementState
{
    public float Gravity { get; }
    
    public FallingState(MovementParameters movementParameters, IPlayerInfo playerInfo, float gravity) : base(movementParameters, playerInfo)
    {
        Gravity = gravity;
    }

    public override MovementState ProcessInterrupts(ref KinematicState<Vector2> kinematics, IEnumerable<IInterrupt> interrupts)
    {
        if (TryWallSlide(interrupts, out MovementState wallSlideState))
            return wallSlideState;
        
        // Process Jump
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
            
        if (TryWallJump(kinematics, interrupts, out MovementState wallJumpState))
            return wallJumpState;

        return base.ProcessInterrupts(ref kinematics, interrupts);
    }

    public override MovementState UpdateKinematics(ref float t, ref KinematicState<Vector2> kinematics, out KinematicSegment<Vector2>[] motion)
    {
        motion = ApplyMotionCurves(t, ref kinematics, WalkingCurve, 
            (float t, ref KinematicState<float> kinematics) => FallingCurve(t, -parameters.TerminalVelocity, Gravity, ref kinematics));
        t = 0f;

        return this;
    }
    
    private bool CanJump(KinematicState<Vector2> kinematics)
    {
        float fallTime = -kinematics.velocity.y / parameters.FallGravity;
        return fallTime < parameters.CoyoteTime || player.GroundCheck();
    }
}
