using System;
using UnityEngine;

public class VerticalGrappleState : MovementState
{
    private class VerticalGrappleMovementParameters : ModifiedMovementParameters
    {
        public VerticalGrappleMovementParameters(float jumpHeight, MovementParameters baseParameters) : base(baseParameters)
        {
            MaxJumpHeight = jumpHeight;
        }

        public override float MaxJumpHeight { get; }
    }
    
    private Action<KinematicState<Vector2>> onFirstUpdate;
    private MovementState innerState;
    
    public VerticalGrappleState(MovementParameters movementParameters, IPlayerInfo playerInfo, Vector2 anchor) : base(movementParameters, playerInfo)
    {
        onFirstUpdate = kinematics =>
        {
            onFirstUpdate = null;
            Vector2 rope = anchor - kinematics.position;
            float jumpHeight = Vector2.Dot(rope, Vector2.up);

            VerticalGrappleMovementParameters newParameters = new VerticalGrappleMovementParameters(jumpHeight, parameters);
            innerState = new JumpingState(newParameters, player, kinematics);
        };
    }

    public override MovementState UpdateKinematics(ref float t, ref KinematicState<Vector2> kinematics, out KinematicSegment<Vector2>[] motion)
    {
        onFirstUpdate?.Invoke(kinematics);
        innerState = innerState.UpdateKinematics(ref t, ref kinematics, out motion);
        
        return this;
    }
}
