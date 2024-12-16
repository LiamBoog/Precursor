using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GrappleWallJumpState : MovementState
{
    private sealed class GrappleWallJumpMovementParameters : ModifiedMovementParameters
    {
        public GrappleWallJumpMovementParameters(MovementParameters baseParameters, float boost)
        {
            MaxJumpHeight = Mathf.Lerp(baseParameters.MaxJumpHeight, baseParameters.MaxGrappleWallJumpHeight, boost);
            JumpVelocity = Mathf.Sqrt(2f * baseParameters.RiseGravity * MaxJumpHeight);
            TerminalVelocity = Mathf.Sqrt(2f * baseParameters.FallGravity * MaxJumpHeight);
            RiseTime = JumpVelocity / baseParameters.RiseGravity;
            FallTime = TerminalVelocity / baseParameters.FallGravity;
            TopSpeed = baseParameters.MaxJumpDistance / (RiseTime + FallTime);
            CopyDataFromBaseParameters(baseParameters);
        }

        public override float TopSpeed { get; protected set; }
        public override float JumpVelocity { get; }
        public override float TerminalVelocity { get; }
        public override float MaxJumpHeight { get; }
        public override float MaxJumpDistance => MaxGrappleWallJumpDistance;
        public override float RiseTime { get; }
        public override float FallTime { get; }
    }
    
    private MovementState innerState;
    
    public GrappleWallJumpState(MovementParameters movementParameters, IPlayerInfo playerInfo, KinematicState<Vector2> initialKinematics) : base(movementParameters, playerInfo)
    {
        float boost = (initialKinematics.velocity.magnitude - movementParameters.TopSpeed) / (movementParameters.ImpactSpeed - movementParameters.TopSpeed);
        innerState = new WallJumpState(new GrappleWallJumpMovementParameters(movementParameters, boost), playerInfo, player.WallCheck(), initialKinematics);
    }

    public override MovementState ProcessInterrupts(ref KinematicState<Vector2> kinematics, IEnumerable<IInterrupt> interrupts)
    {
        MovementState previousState = innerState;
        innerState = innerState.ProcessInterrupts(ref kinematics, interrupts);
        if (innerState != previousState && innerState is not FallingState)
        {
            innerState.parameters = parameters;
            return innerState;
        }

        return this;
    }

    public override MovementState UpdateKinematics(ref float t, ref KinematicState<Vector2> kinematics, out KinematicSegment<Vector2>[] motion)
    {
        MovementState previousState = innerState;
        innerState = innerState.UpdateKinematics(ref t, ref kinematics, out motion);
        if (innerState != previousState && innerState is not FallingState)
        {
            innerState.parameters = parameters;
            return innerState;
        }

        return this;
    }
}
