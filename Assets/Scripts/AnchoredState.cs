using System;
using System.Collections.Generic;
using UnityEngine;

public class AnchoredState : MovementState
{
    private MovementState innerState;
    
    public AnchoredState(MovementParameters movementParameters, IPlayerInfo playerInfo, MovementState previousState) : base(movementParameters, playerInfo)
    {
        innerState = previousState;
    }

    public override MovementState ProcessInterrupts(ref KinematicState<Vector2> kinematics, IEnumerable<IInterrupt> interrupts)
    {
        throw new NotImplementedException();
    }

    public override MovementState UpdateKinematics(ref float t, ref KinematicState<Vector2> kinematics, out KinematicSegment<Vector2>[] motion)
    {
        KinematicState<Vector2> initialKinematics = kinematics;
        innerState.UpdateKinematics(ref t, ref kinematics, out motion);

        throw new NotImplementedException();
    }
}
