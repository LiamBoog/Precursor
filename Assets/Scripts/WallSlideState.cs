using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public abstract partial class MovementState
{
    protected KinematicSegment<float>[] WallSlidingCurve(float t, ref KinematicState<float> kinematics)
    {
        return FallingCurve(t, -parameters.WallSlideVelocity, parameters.FallGravity, ref kinematics);
    }
}

public class WallSlideState : MovementState
{
    public WallSlideState(MovementParameters movementParameters, IPlayerInfo playerInfo) : base(movementParameters, playerInfo) { }

    public override MovementState ProcessInterrupts(ref KinematicState<Vector2> kinematics, IEnumerable<IInterrupt> interrupts)
    {
        if (player.Aim.x * player.WallCheck() >= 0f) // Not wall sliding anymore
            return new FallingState(parameters, player, parameters.FallGravity);

        if (interrupts.Any(i => i is JumpInterrupt { type: JumpInterrupt.Type.Started }))
            return new WallJumpState(parameters, player, -Math.Sign(player.Aim.x), kinematics);

        return this;
    }

    public override MovementState UpdateKinematics(ref float t, ref KinematicState<Vector2> kinematics, out KinematicSegment<Vector2>[] motion)
    {
        motion = ApplyMotionCurves(
            t,
            ref kinematics,
            (float t, ref KinematicState<float> kinematics) => new[] { new KinematicSegment<float>(kinematics, 0f, t) },
            WallSlidingCurve
        );
        t = 0f;

        return this;
    }
}
