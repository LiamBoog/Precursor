using UnityEngine;

public class VerticalGrappleJump : MovementState
{
    private class VerticalGrappleJumpParameters : ModifiedMovementParameters
    {
        public VerticalGrappleJumpParameters(MovementParameters baseParameters) : base(baseParameters)
        {
            MaxJumpHeight = baseParameters.MaxVerticalGrappleJumpHeight;
            float durationScalingFactor = Mathf.Sqrt(MaxVerticalGrappleJumpHeight / baseParameters.MaxJumpHeight);
            JumpDuration = durationScalingFactor * (baseParameters.RiseTime + baseParameters.FallTime);
        }

        public override float MaxJumpHeight { get; }
        public override float JumpDuration { get; }
    }

    private MovementState innerState;
    
    public VerticalGrappleJump(MovementParameters movementParameters, IPlayerInfo playerInfo, KinematicState<Vector2> initialKinematics) : base(movementParameters, playerInfo)
    {
        VerticalGrappleJumpParameters newParameters = new VerticalGrappleJumpParameters(parameters);
        innerState = new JumpingState(newParameters, player, initialKinematics);
    }

    public override MovementState UpdateKinematics(ref float t, ref KinematicState<Vector2> kinematics, out KinematicSegment<Vector2>[] motion)
    {
        innerState = innerState.UpdateKinematics(ref t, ref kinematics, out motion);
        if (innerState is not JumpingState && innerState is not FallingState)
        {
            innerState.parameters = parameters;
            return innerState;
        }

        return this;
    }
}
