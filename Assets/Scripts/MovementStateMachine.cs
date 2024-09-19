using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using UnityEngine;

public interface IInterrupt { }

public class MovementStateMachine
{
    private MovementState state;

    public MovementStateMachine(MovementParameters movementParameters, IPlayerInfo playerInfo)
    {
        state = new WalkingState(movementParameters, playerInfo);
    }

    public void Update(float t, ref KinematicState<Vector2> kinematics, List<IInterrupt> interrupts)
    {
        state = state.Update(t, ref kinematics, interrupts);
    }
}
