using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public abstract partial class MovementState
{
    
}

public class JumpingState : MovementState
{
    private delegate void JumpInitializer(ref KinematicState<Vector2> kinematics);
    
    private float initialHeight;
    private float gravity;
    private JumpInitializer onFirstUpdate;

    public JumpingState(MovementParameters movementParameters, IPlayerInfo playerInfo, KinematicState<Vector2> initialKinematics) : base(movementParameters, playerInfo)
    {
        initialHeight = initialKinematics.position.y;
        onFirstUpdate = OnFirstUpdate;
        gravity = parameters.RiseGravity;
        
        void OnFirstUpdate(ref KinematicState<Vector2> kinematics)
        {
            onFirstUpdate = null;
            kinematics.velocity.y = parameters.JumpVelocity;
        }
    }

    private JumpingState(JumpingState previous, float gravity) : base(previous.parameters, previous.player)
    {
        this.gravity = gravity;
    }
    
    protected override MovementState Update(ref float t, ref KinematicState<Vector2> kinematics, IEnumerable<IInterrupt> interrupts)
    {
        onFirstUpdate?.Invoke(ref kinematics);

        if (interrupts.Any(i => i is JumpInterrupt { type: JumpInterrupt.Type.Cancelled }))
        {
            return new JumpingState(this, GetCancelledJumpGravityMagnitude(kinematics));
        }
        if (interrupts.FirstOrDefault(i => i is ICollision) is ICollision collision)
        {
            if (Vector2.Dot(collision.Normal, Vector2.up) > 0.5f) // Collision with ground
                return new WalkingState(parameters, player);
        }
        
        KinematicState<float> xKinematics = new(kinematics.position.x, kinematics.velocity.x);
        KinematicState<float> yKinematics = new(kinematics.position.y, kinematics.velocity.y);
        
        Jump(t, ref yKinematics);
        WalkingCurve(t, ref xKinematics, player.Aim.x);
        kinematics = new(
            new(xKinematics.position, yKinematics.position), 
            new(xKinematics.velocity, yKinematics.velocity));

        t = 0f;
        return this;
    }
    
    protected KinematicSegment<float>[] Jump(float t, ref KinematicState<float> kinematics)
    {
        List<KinematicSegment<float>> output = new();
        if (kinematics.velocity > 0f)
        {
            output.Add(AccelerateTowardTargetVelocity(ref t, 0f, gravity, ref kinematics));
        }
        output.AddRange(FallingCurve(t, ref kinematics, player.Aim.x, player.WallCheck));
        return output.ToArray();
    }
    
    private float GetCancelledJumpGravityMagnitude(KinematicState<Vector2> kinematics)
    {
        float verticalDisplacement = kinematics.position.y - initialHeight;
        float maxRise = Mathf.Max(parameters.CancelledJumpRise, parameters.MinJumpHeight - verticalDisplacement);
        float remainingRise = Mathf.Min(maxRise, parameters.MaxJumpHeight - verticalDisplacement);
        
        return 0.5f * kinematics.velocity.y * kinematics.velocity.y / remainingRise;
    }
}
