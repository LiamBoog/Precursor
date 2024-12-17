using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class GrappleWallJumpState : MovementState
{
    private sealed class GrappleWallJumpMovementParameters : ModifiedMovementParameters
    {
        public GrappleWallJumpMovementParameters(MovementParameters baseParameters, float boost)
        {
            MaxJumpHeight = Mathf.Lerp(baseParameters.MaxJumpHeight, baseParameters.MaxGrappleWallJumpHeight, boost);
            JumpVelocity = GetJumpVelocity(baseParameters.RiseGravity, MaxJumpHeight);
            TerminalVelocity = GetJumpVelocity(baseParameters.FallGravity, MaxJumpHeight);
            RiseTime = JumpVelocity / baseParameters.RiseGravity;
            FallTime = TerminalVelocity / baseParameters.FallGravity;
            CopyDataFromBaseParameters(baseParameters);
            
            float maxRiseTime = GetJumpVelocity(RiseGravity, MaxGrappleWallJumpHeight) / RiseGravity;
            float maxFallTime = GetJumpVelocity(FallGravity, MaxGrappleWallJumpHeight) / FallGravity;
            TopSpeed = MaxJumpDistance / (maxRiseTime + maxFallTime);
            
            float velocityScalingFactor = TopSpeed / baseParameters.TopSpeed;
            AccelerationDistance *= velocityScalingFactor;
            DecelerationDistance *= velocityScalingFactor;
        }

        public override float JumpVelocity { get; }
        public override float TerminalVelocity { get; }
        public override float MaxJumpHeight { get; }
        public override float RiseTime { get; }
        public override float FallTime { get; }
        
        private new float GetJumpVelocity(float gravity, float height) => Mathf.Sqrt(2f * gravity * height);
    }
    
    private MovementState innerState;
    
    public GrappleWallJumpState(MovementParameters movementParameters, IPlayerInfo playerInfo, KinematicState<Vector2> initialKinematics) : base(movementParameters, playerInfo)
    {
        float boost = (initialKinematics.velocity.magnitude - movementParameters.TopSpeed) / (movementParameters.ImpactSpeed - movementParameters.TopSpeed);
        GrappleWallJumpMovementParameters newMovementParameters = new GrappleWallJumpMovementParameters(movementParameters, boost);
        innerState = new WallJumpState(newMovementParameters, playerInfo, player.WallCheck(), initialKinematics);
    }

    public override MovementState ProcessInterrupts(ref KinematicState<Vector2> kinematics, IEnumerable<IInterrupt> interrupts)
    {
        MovementParameters previousParameters = innerState.parameters;
        MovementState previousState = innerState;
        
        innerState = innerState.ProcessInterrupts(ref kinematics, interrupts);
        if (innerState != previousState && innerState is not FallingState)
        {
            // TODO - Very yucky hack >:((
            if (innerState is WallJumpState)
                return new WallJumpState(parameters, player, player.WallCheck(), kinematics);
            
            innerState.parameters = parameters;
            return innerState;
        }

        innerState.parameters = previousParameters;
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
