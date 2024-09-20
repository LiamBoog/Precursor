using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.PlayerLoop;

public abstract partial class MovementState
{
    protected KinematicSegment<float>[] JumpCurve(float t, ref KinematicState<float> kinematics, float gravity)
    {
        List<KinematicSegment<float>> output = new();
        if (kinematics.velocity > 0f)
        {
            output.Add(AccelerateTowardTargetVelocity(ref t, 0f, gravity, ref kinematics));
        }
        output.AddRange(FallingCurve(t, ref kinematics));
        return output.ToArray();
    }
}

public class JumpingState : MovementState
{
    private delegate void JumpInitializer(ref KinematicState<Vector2> kinematics);
    
    private readonly float initialHeight;
    private JumpInitializer onFirstUpdate;

    public JumpingState(MovementParameters movementParameters, IPlayerInfo playerInfo, KinematicState<Vector2> initialKinematics) : base(movementParameters, playerInfo)
    {
        initialHeight = initialKinematics.position.y;
        onFirstUpdate = OnFirstUpdate;
        
        void OnFirstUpdate(ref KinematicState<Vector2> kinematics)
        {
            onFirstUpdate = null;
            kinematics.velocity.y = parameters.JumpVelocity;
        }
    }

    protected override MovementState Update(float t, ref KinematicState<Vector2> kinematics, IEnumerable<IInterrupt> interrupts)
    {
        onFirstUpdate?.Invoke(ref kinematics);

        if (interrupts.Any(i => i is JumpInterrupt { type: JumpInterrupt.Type.Cancelled }))
        {
            return new CancelledJumpState(parameters, player, GetCancelledJumpGravityMagnitude(kinematics));
        }
        if (interrupts.FirstOrDefault(i => i is ICollision) is ICollision collision)
        {
            if (Vector2.Dot(collision.Normal, Vector2.up) > 0.5f) // Collision with ground
            {
                if (player.JumpBuffer.Flush())
                    return new JumpingState(parameters, player, kinematics);
                
                return new WalkingState(parameters, player);
            }
        }
        
        KinematicSegment<float>[] Vertical(float t, ref KinematicState<float> kinematics) => JumpCurve(t, ref kinematics, parameters.RiseGravity);
        ApplyMotionCurves(t, ref kinematics, WalkingCurve, Vertical);

        return this;
    }
    
    private float GetCancelledJumpGravityMagnitude(KinematicState<Vector2> kinematics)
    {
        float verticalDisplacement = kinematics.position.y - initialHeight;
        float maxRise = Mathf.Max(parameters.CancelledJumpRise, parameters.MinJumpHeight - verticalDisplacement);
        float remainingRise = Mathf.Min(maxRise, parameters.MaxJumpHeight - verticalDisplacement);
        
        return 0.5f * kinematics.velocity.y * kinematics.velocity.y / remainingRise;
    }
}

public class CancelledJumpState : MovementState
{
    private readonly float gravity;
    
    public CancelledJumpState(MovementParameters movementParameters, IPlayerInfo playerInfo, float gravity) : base(movementParameters, playerInfo)
    {
        parameters = movementParameters;
        player = playerInfo;
        this.gravity = gravity;
    }

    protected override MovementState Update(float t, ref KinematicState<Vector2> kinematics, IEnumerable<IInterrupt> interrupts)
    {
        if (interrupts.FirstOrDefault(i => i is ICollision) is ICollision collision)
        {
            if (Vector2.Dot(collision.Normal, Vector2.up) > 0.5f) // Collision with ground
            {
                if (player.JumpBuffer.Flush())
                    return new JumpingState(parameters, player, kinematics);
                
                return new WalkingState(parameters, player);
            }
        }
        
        KinematicSegment<float>[] Vertical(float t, ref KinematicState<float> kinematics) => JumpCurve(t, ref kinematics, gravity);
        ApplyMotionCurves(t, ref kinematics, WalkingCurve, Vertical);

        return this;
    }
}
