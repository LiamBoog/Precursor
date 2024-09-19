using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract partial class MovementState
{
    protected bool CanJump(KinematicState<Vector2> kinematics)
    {
        float fallTime = -kinematics.velocity.y / parameters.FallGravity;
        bool coyoteCheck = this is not JumpingState && fallTime < parameters.CoyoteTime;
        return coyoteCheck || player.GroundCheck();
    }
}

public class JumpingState : MovementState
{
    private float initialHeight;

    protected JumpingState(KinematicState<Vector2> initialKinematics, MovementParameters movementParameters, IPlayerInfo playerInfo) : base(movementParameters, playerInfo)
    {
        initialHeight = initialKinematics.position.y;
    }
    
    protected override MovementState Update(float t, ref KinematicState<Vector2> kinematics, IEnumerable<IInterrupt> interrupts)
    {
        return this;
    }
    
    
}
