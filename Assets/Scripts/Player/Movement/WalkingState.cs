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
        // TODO - This transition is a little janky maybe this needs to be its own state or something??
        if (!CanJump(kinematics))
            return new FallingState(parameters, player, parameters.FallGravity);
        
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
                    return new JumpingState(parameters, player, kinematics);
            }
        }

        return base.ProcessInterrupts(ref kinematics, interrupts);
    }

    public override MovementState UpdateKinematics(ref float t, ref KinematicState<Vector2> kinematics, out KinematicSegment<Vector2>[] motion)
    {
        MotionCurve verticalCurve = player.GroundCheck() ? 
            (float t, ref KinematicState<float> kinematics) => new[] { new KinematicSegment<float>(kinematics, 0f, t) } : 
            FreeFallingCurve;
        motion = ApplyMotionCurves(t, ref kinematics, WalkingCurve,verticalCurve);
        t = 0f;

        return this;
    }
    
    private bool CanJump(KinematicState<Vector2> kinematics)
    {
        float fallTime = -kinematics.velocity.y / parameters.FallGravity;
        return fallTime < parameters.CoyoteTime || player.GroundCheck();
    }
}
