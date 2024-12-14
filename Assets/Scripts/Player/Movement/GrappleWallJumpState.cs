using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GrappleWallJumpState : MovementState
{
    private class GrappleWallJumpMovementParameters : ModifiedMovementParameters
    {
        public GrappleWallJumpMovementParameters(MovementParameters baseParameters)
        {
            CopyDataFromBaseParameters(baseParameters);
        }

        public override float MaxJumpHeight => grappleWallJump.MaxJumpHeight;
        public override float MinJumpHeight => grappleWallJump.MinJumpHeight;
        public override float MaxJumpDistance => grappleWallJump.MaxJumpDistance;
        public override float CancelledJumpRise => grappleWallJump.CancelledJumpRise;
    }
    
    private MovementParameters initialParameters;
    private MovementState innerState;
    
    public GrappleWallJumpState(MovementParameters movementParameters, IPlayerInfo playerInfo, KinematicState<Vector2> initialKinematics) : base(movementParameters, playerInfo)
    {
        initialParameters = movementParameters;
        innerState = new WallJumpState(new GrappleWallJumpMovementParameters(movementParameters), playerInfo, player.WallCheck(), initialKinematics);
    }

    public override MovementState ProcessInterrupts(ref KinematicState<Vector2> kinematics, IEnumerable<IInterrupt> interrupts)
    {
        innerState = innerState.ProcessInterrupts(ref kinematics, interrupts);
        if (innerState is not WallJumpState && innerState is not FallingState)
        {
            innerState.parameters = initialParameters;
            return innerState;
        }

        return this;
    }

    public override MovementState UpdateKinematics(ref float t, ref KinematicState<Vector2> kinematics, out KinematicSegment<Vector2>[] motion)
    {
        innerState = innerState.UpdateKinematics(ref t, ref kinematics, out motion);
        if (innerState is not WallJumpState && innerState is not FallingState)
        {
            innerState.parameters = initialParameters;
            return innerState;
        }

        return this;
    }
}
