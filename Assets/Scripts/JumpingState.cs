using System;
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
    
    protected KinematicSegment<float> Jump(ref float t, ref KinematicState<float> kinematics)
    {
        return AccelerateTowardTargetVelocity(ref t, 0f, parameters.RiseGravity, ref kinematics);
    }
}

public class JumpingState : MovementState
{
    private delegate void JumpInitializer(ref KinematicState<Vector2> kinematics);
    
    private float initialHeight;
    private JumpInitializer onFirstUpdate;

    public JumpingState(KinematicState<Vector2> initialKinematics, MovementParameters movementParameters, IPlayerInfo playerInfo) : base(movementParameters, playerInfo)
    {
        initialHeight = initialKinematics.position.y;
        onFirstUpdate = OnFirstUpdate;
        
        void OnFirstUpdate(ref KinematicState<Vector2> kinematics)
        {
            onFirstUpdate = null;
            kinematics.velocity.y = parameters.JumpVelocity;
        }
    }
    
    protected override MovementState Update(ref float t, ref KinematicState<Vector2> kinematics, IEnumerable<IInterrupt> interrupts)
    {
        onFirstUpdate?.Invoke(ref kinematics);
        
        KinematicState<float> xKinematics = new(kinematics.position.x, kinematics.velocity.x);
        KinematicState<float> yKinematics = new(kinematics.position.y, kinematics.velocity.y);
        
        float jumpTime = Jump(ref t, ref yKinematics).duration;
        WalkingCurve(jumpTime, ref xKinematics, player.Aim.x);
        kinematics = new(
            new(xKinematics.position, yKinematics.position), 
            new(xKinematics.velocity, yKinematics.velocity));

        if (t <= 0f)
            return this;
        
        return new WalkingState(parameters, player);
    }
}
