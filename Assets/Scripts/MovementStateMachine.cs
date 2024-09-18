using UnityEngine;

public class MovementStateMachine
{
    private IMovementState state;

    public MovementStateMachine(PlayerController.MovementParameters movementParameters)
    {
        state = new WalkingState(movementParameters);
    }

    public void Update(float t, ref KinematicState<Vector2> kinematics, IPlayerInfo playerInfo)
    {
        state = state.Update(t, ref kinematics, playerInfo);
    }
}
