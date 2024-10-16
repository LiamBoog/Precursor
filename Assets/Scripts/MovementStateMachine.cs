using System.Collections.Generic;
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
        state = state.ProcessInterrupts(ref kinematics, interrupts);
        interrupts.Clear();
        state = state.FullyUpdateKinematics(ref t, ref kinematics, out _);
    }
}
