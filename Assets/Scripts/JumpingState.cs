using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public abstract partial class MovementState
{
    protected KinematicSegment<float>[] JumpCurve(float t, ref KinematicState<float> kinematics, float gravity)
    {
        List<KinematicSegment<float>> output = new();
        output.Add(AccelerateTowardTargetVelocity(ref t, 0f, gravity, ref kinematics));
        output.AddRange(FreeFallingCurve(t, ref kinematics));
        return output.ToArray();
    }
}

public class JumpingState : MovementState
{
    protected delegate void JumpInitializer(ref KinematicState<Vector2> kinematics);
    
    private readonly float initialHeight;
    private float gravity;
    private JumpInitializer onFirstUpdate;

    public JumpingState(MovementParameters movementParameters, IPlayerInfo playerInfo, KinematicState<Vector2> initialKinematics) : base(movementParameters, playerInfo)
    {
        initialHeight = initialKinematics.position.y;
        gravity = parameters.RiseGravity;
        onFirstUpdate = (ref KinematicState<Vector2> kinematics) =>
        {
            onFirstUpdate = null;
            kinematics.velocity.y = parameters.JumpVelocity;
        };
    }

    protected override MovementState Update(ref float t, ref KinematicState<Vector2> kinematics, IEnumerable<IInterrupt> interrupts)
    {
        onFirstUpdate?.Invoke(ref kinematics);

        // Handle cancelled jump
        if (interrupts.Any(i => i is JumpInterrupt { type: JumpInterrupt.Type.Cancelled }))
        {
            gravity = GetCancelledJumpGravityMagnitude(kinematics);
        }

        if (interrupts.Any(i => i is JumpInterrupt { type: JumpInterrupt.Type.Started }) && player.WallCheck() is int normal && normal != 0)
        {
            return new WallJumpState(parameters, player, normal, kinematics);
        }
        
        // Handle collision
        if (interrupts.FirstOrDefault(i => i is ICollision) is ICollision collision)
        {
            kinematics.velocity.y = collision.Deflection.y != 0f ? 0f : kinematics.velocity.y;
            kinematics.velocity.x = collision.Deflection.x != 0f ? 0f : kinematics.velocity.x;

            if (collision.Normal.x != 0 && player.JumpBuffer.Flush())
            {
                return new WallJumpState(parameters, player, Math.Sign(collision.Normal.x), kinematics);
            }
        }
        
        ApplyMotionCurves(t, ref kinematics, WalkingCurve, 
            (float t, ref KinematicState<float> kinematics) => JumpCurve(t, ref kinematics, gravity));
        t = 0f;

        return kinematics.velocity.y <= 0f ? new FallingState(parameters, player, parameters.FallGravity) : this;
    }
    
    private float GetCancelledJumpGravityMagnitude(KinematicState<Vector2> kinematics)
    {
        float verticalDisplacement = kinematics.position.y - initialHeight;
        float maxRise = Mathf.Max(parameters.CancelledJumpRise, parameters.MinJumpHeight - verticalDisplacement);
        float remainingRise = Mathf.Min(maxRise, parameters.MaxJumpHeight - verticalDisplacement);
        
        return 0.5f * kinematics.velocity.y * kinematics.velocity.y / remainingRise;
    }
}
