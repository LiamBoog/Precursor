using System.Collections.Generic;
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
        while (t > 0f)
        {
            state = state.Update(ref t, ref kinematics, interrupts);
        }
    }
}
