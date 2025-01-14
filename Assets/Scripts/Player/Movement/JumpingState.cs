using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
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
        player.JumpBuffer.Flush();
    }

    public override MovementState ProcessInterrupts(ref KinematicState<Vector2> kinematics, IEnumerable<IInterrupt> interrupts)
    {
        // Handle cancelled jump
        if (interrupts.Any(i => i is JumpInterrupt { type: JumpInterrupt.Type.Cancelled }))
        {
            gravity = GetCancelledJumpGravityMagnitude(kinematics);
        }

        if (TryWallJump(kinematics, interrupts, out MovementState newState))
            return newState;

        return base.ProcessInterrupts(ref kinematics, interrupts);
    }

    public override MovementState UpdateKinematics(ref float t, ref KinematicState<Vector2> kinematics, out KinematicSegment<Vector2>[] motion)
    {
        onFirstUpdate?.Invoke(ref kinematics);
        motion = ApplyMotionCurves(t, ref kinematics, WalkingCurve, 
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
