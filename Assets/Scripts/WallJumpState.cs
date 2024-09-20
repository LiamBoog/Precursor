using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WallJumpState : MovementState
{
    private delegate void WallJumpInitializer(ref KinematicState<Vector2> kinematics);
    
    private float elapsedTime;
    private float controlTime;
    private WallJumpInitializer onFirstUpdate;
    
    public WallJumpState(MovementParameters movementParameters, IPlayerInfo playerInfo, int direction) : base(movementParameters, playerInfo)
    {
        elapsedTime = 0f;
        controlTime = GetWallJumpControlTime();
        onFirstUpdate = (ref KinematicState<Vector2> kinematics) =>
        {
            onFirstUpdate = null;
            kinematics.velocity = new Vector2(direction * parameters.TopSpeed, parameters.JumpVelocity);
        };
    }
    
    protected override MovementState Update(ref float t, ref KinematicState<Vector2> kinematics, IEnumerable<IInterrupt> interrupts)
    {
        onFirstUpdate?.Invoke(ref kinematics);
        
        if (elapsedTime >= controlTime)
            return new CancelledJumpState(parameters, player, parameters.RiseGravity);

        KinematicState<float> xKinematics = new(kinematics.position.x, kinematics.velocity.x);
        KinematicState<float> yKinematics = new(kinematics.position.y, kinematics.velocity.y);
        
        float totalTime = t;
        ControlledMovement(ref t, ref xKinematics);
        elapsedTime += totalTime - t;
        JumpCurve(totalTime - t, ref yKinematics, parameters.RiseGravity);
        kinematics = new(
            new(xKinematics.position, yKinematics.position),
            new(xKinematics.velocity, yKinematics.velocity)
        );
        
        return t > 0f ? new CancelledJumpState(parameters, player, parameters.RiseGravity) : this;
    }
    
    private KinematicSegment<float> ControlledMovement(ref float t, ref KinematicState<float> kinematics)
    {
        float maxMovementTime = controlTime - elapsedTime;
        float movementTime = Mathf.Min(t, maxMovementTime);
        
        t -= movementTime;
        return LinearMotionCurve(movementTime, ref kinematics);
    }
    
    private float GetWallJumpControlTime()
    {
        float controlTime = 0.5f * (parameters.RiseDistance + parameters.FallDistance - 3f * parameters.DecelerationDistance - parameters.AccelerationDistance) / parameters.TopSpeed;
        
        if (parameters.ClimbHeight < 0f)
            return controlTime - 0.5f * parameters.ClimbHeight / parameters.TerminalVelocity;
        
        float terminalVelocity = parameters.TerminalVelocity;
        float fallTime = parameters.FallDistance / parameters.TopSpeed;
        float climbOffset = (-terminalVelocity + Mathf.Sqrt(terminalVelocity * (terminalVelocity - 2f * parameters.ClimbHeight / fallTime))) / -parameters.FallGravity;
        
        return controlTime - 0.5f * climbOffset;
    }
}
