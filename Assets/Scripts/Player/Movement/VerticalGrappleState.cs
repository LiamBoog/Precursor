using UnityEngine;

public class VerticalGrappleState : GrappleState
{
    private class VerticalGrappleMovementParameters : ModifiedMovementParameters
    {
        public VerticalGrappleMovementParameters(float jumpHeight, MovementParameters baseParameters) : base(baseParameters)
        {
            MaxJumpHeight = jumpHeight;
        }

        public override float MaxJumpHeight { get; }
    }
    
    private delegate void VerticalGrappleInitializer(ref KinematicState<Vector2> kinematics, out KinematicSegment<Vector2>[] motion);
    
    private VerticalGrappleInitializer onFirstUpdate;
    private MovementState innerState;
    
    public VerticalGrappleState(MovementParameters movementParameters, IPlayerInfo playerInfo, Vector2 anchor) : base(movementParameters, playerInfo, default)
    {
        onFirstUpdate = (ref KinematicState<Vector2> kinematics, out KinematicSegment<Vector2>[] motion) =>
        {
            onFirstUpdate = null;
            Vector2 rope = anchor - kinematics.position;
            float jumpHeight = Vector2.Dot(rope, Vector2.up);

            VerticalGrappleMovementParameters newParameters = new VerticalGrappleMovementParameters(jumpHeight, parameters);
            innerState = new JumpingState(newParameters, player, kinematics);
            
            float t = 0f;
            innerState = innerState.UpdateKinematics(ref t, ref kinematics, out motion);
        };
    }

    public override MovementState UpdateKinematics(ref float t, ref KinematicState<Vector2> kinematics, out KinematicSegment<Vector2>[] motion)
    {
        KinematicState<Vector2> initialKinematics = kinematics;
        MovementState output = PerformUpdate(ref t, ref kinematics, out motion);
        kinematics.position.x = initialKinematics.position.x;
        kinematics.velocity.x = initialKinematics.velocity.x;
        return output;

        MovementState PerformUpdate(ref float t, ref KinematicState<Vector2> kinematics, out KinematicSegment<Vector2>[] motion)
        {
            onFirstUpdate?.Invoke(ref kinematics, out motion);
        
            float remainingRiseTime = kinematics.velocity.y / innerState.parameters.RiseGravity;
            if (remainingRiseTime > t)
            {
                innerState = innerState.UpdateKinematics(ref t, ref kinematics, out motion);
                return this;
            }
        
            t -= remainingRiseTime;
            innerState.UpdateKinematics(ref remainingRiseTime, ref kinematics, out motion);
        
            if (player.JumpBuffer.Flush())
                return new VerticalGrappleJump(parameters, player, kinematics);
        
            return new FallingState(parameters, player, parameters.FallGravity);
        }
    }
}
