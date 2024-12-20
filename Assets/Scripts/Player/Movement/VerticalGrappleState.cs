using System;
using UnityEngine;

public class VerticalGrappleState : GrappleState
{
    private Action<KinematicState<Vector2>> onFirstUpdate;
    
    public VerticalGrappleState(MovementParameters movementParameters, IPlayerInfo playerInfo, Vector2 anchor) : base(movementParameters, playerInfo, anchor)
    {
        onFirstUpdate = kinematics =>
        {
            onFirstUpdate = null;
            Vector2 rope = anchor - kinematics.position;
            this.anchor = kinematics.position + (Vector2) Vector3.Project(rope, Vector2.up);
        };
    }

    public override MovementState UpdateKinematics(ref float t, ref KinematicState<Vector2> kinematics, out KinematicSegment<Vector2>[] motion)
    {
        onFirstUpdate?.Invoke(kinematics);
        return base.UpdateKinematics(ref t, ref kinematics, out motion);
    }
}
