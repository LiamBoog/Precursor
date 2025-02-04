using System.Collections.Generic;
using UnityEngine;

public interface IInterrupt { }

public class MovementStateMachine
{
    public MovementState State { get; private set; }

    public MovementStateMachine(MovementParameters movementParameters, IPlayerInfo playerInfo)
    {
        State = new WalkingState(movementParameters, playerInfo);
    }

    public void Update(float t, ref KinematicState<Vector2> kinematics, List<IInterrupt> interrupts)
    {
        State = State.ProcessInterrupts(ref kinematics, interrupts);
        interrupts.Clear();
        State = State.FullyUpdateKinematics(ref t, ref kinematics, out _);
    }
}
