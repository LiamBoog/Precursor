using System.Collections.Generic;
using UnityEngine;

public abstract partial class MovementState
{
    protected KinematicSegment<float>[] WallSlidingCurve(float t, ref KinematicState<float> kinematics)
    {
        return FallingCurve(t, -parameters.WallSlideVelocity, ref kinematics);
    }
}

public class WallSlideState : MovementState
{
    public WallSlideState(MovementParameters movementParameters, IPlayerInfo playerInfo) : base(movementParameters, playerInfo) { }
    
    protected override MovementState Update(ref float t, ref KinematicState<Vector2> kinematics, IEnumerable<IInterrupt> interrupts)
    {
        if (player.Aim.x * player.WallCheck() >= 0f) // Not wall sliding anymore
            return new CancelledJumpState(parameters, player, parameters.FallGravity);
        
        ApplyMotionCurves(t, ref kinematics, (float _, ref KinematicState<float> _) => default, WallSlidingCurve);
        t = 0f;

        return this;
    }
}
