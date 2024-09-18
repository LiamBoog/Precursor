using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MovementStateMachine
{
    private IMovementState state;

    public MovementStateMachine(PlayerController.MovementParameters movementParameters)
    {
        state = new WalkingState(movementParameters);
    }

    public void Update(float t, ref IMovementState.KinematicState kinematics)
    {
        state = state.Update(t, out IMovementState.KinematicSegment[] _, ref kinematics);
    }
}
