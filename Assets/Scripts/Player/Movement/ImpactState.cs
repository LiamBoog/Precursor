using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ImpactState : MovementState
{
    private delegate void ImpactInitializer(ref KinematicState<Vector2> kinematics);
    
    private Vector2 slideDirection;
    private float elapsedTime;
    private float acceleration;
    private ImpactInitializer onFirstUpdate;

    public ImpactState(MovementParameters movementParameters, IPlayerInfo playerInfo, Vector2 slideDirection, Vector2 incomingDirection) : base(movementParameters, playerInfo)
    {
        this.slideDirection = slideDirection;
        float initialSpeed = /*Vector2.Dot(slideDirection, incomingDirection) * */parameters.GrappleSpeed;
        acceleration = (parameters.TopSpeed - initialSpeed) / parameters.ImpactDuration;
        onFirstUpdate = (ref KinematicState<Vector2> kinematics) =>
        {
            kinematics.velocity = initialSpeed * slideDirection;
            onFirstUpdate = null;
        };
    }

    public override MovementState ProcessInterrupts(ref KinematicState<Vector2> kinematics, IEnumerable<IInterrupt> interrupts)
    {
        if (elapsedTime < parameters.ImpactDelay && interrupts.FirstOrDefault(i => i is JumpInterrupt) is JumpInterrupt { type: JumpInterrupt.Type.Started })
        {
            Debug.Log("zuba jump");
            return player.GroundCheck() ? new JumpingState(parameters, player, kinematics) : new WallJumpState(parameters, player, player.WallCheck(), kinematics);
        }
        
        return base.ProcessInterrupts(ref kinematics, interrupts);
    }

    public override MovementState UpdateKinematics(ref float t, ref KinematicState<Vector2> kinematics, out KinematicSegment<Vector2>[] motion)
    {
        onFirstUpdate?.Invoke(ref kinematics);

        motion = ImpactCurve(ref t, ref kinematics);
        if (t > 0f)
        {
            kinematics.velocity = Vector2.zero;
            return new FallingState(parameters, player, parameters.FallGravity);
        }

        return this;
    }

    private KinematicSegment<Vector2>[] ImpactCurve(ref float t, ref KinematicState<Vector2> kinematics)
    {
        Vector2 acceleration = this.acceleration * slideDirection;
        Vector2 targetVelocity = parameters.TopSpeed * slideDirection;

        KinematicState<float> xKinematics = new(kinematics.position.x, kinematics.velocity.x);
        KinematicState<float> yKinematics = new(kinematics.position.y, kinematics.velocity.y);

        float temp = t;
        KinematicSegment<float> xMotion = AccelerateTowardTargetVelocity(ref temp, targetVelocity.x, Mathf.Abs(acceleration.x), ref xKinematics);
        KinematicSegment<float> yMotion = AccelerateTowardTargetVelocity(ref t, targetVelocity.y, Mathf.Abs(acceleration.y), ref yKinematics);
        kinematics = new KinematicState<Vector2>(new(xKinematics.position, yKinematics.position), new(xKinematics.velocity, yKinematics.velocity));
        
        t = Mathf.Min(temp, t);

        return MergeKinematicSegments(new[] { xMotion }, new[] { yMotion });
    }
}
