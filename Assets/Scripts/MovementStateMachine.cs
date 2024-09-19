using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public interface IInterrupt { }

public struct JumpInterrupt : IInterrupt { }

public class MovementStateMachine
{
    private IMovementState state;

    public MovementStateMachine(PlayerController.MovementParameters movementParameters, IPlayerInfo playerInfo)
    {
        state = new WalkingState(movementParameters, playerInfo);
    }

    public void Update(float t, ref KinematicState<Vector2> kinematics, IEnumerable<IInterrupt> interrupts)
    {
        state = state.Update(t, ref kinematics, interrupts);
    }
}
