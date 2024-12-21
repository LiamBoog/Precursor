using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class GrappleState : MovementState
{
    private delegate void GrappleInitializer(ref KinematicState<Vector2> kinematics);
    
    protected Vector2 anchor;
    private GrappleInitializer onFirstUpdate;
    
    public GrappleState(MovementParameters movementParameters, IPlayerInfo playerInfo, Vector2 anchor) : base(movementParameters, playerInfo)
    {
        this.anchor = anchor;
        onFirstUpdate = (ref KinematicState<Vector2> kinematics) =>
        {
            onFirstUpdate = null;
            
            float speed = parameters.RopeLength / parameters.MaxGrappleDuration + 0.5f * parameters.FallGravity * parameters.MaxGrappleDuration;
            float deltaX = anchor.x - kinematics.position.x;
            float A = (anchor.y - kinematics.position.y) / deltaX;
            float B = parameters.FallGravity * deltaX / (2f * speed * speed);
            float angle = deltaX == 0f ? Mathf.PI / 2f : Mathf.Atan2(-1f + Mathf.Sqrt(1f - 4f * B * (A + B)), -2f * B) + Mathf.PI;
            Vector2 initialVelocity = speed * new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));

            kinematics.velocity = initialVelocity;
            KinematicState<Vector2> initialKinematics = kinematics;

            int n = 101;
            Vector3[] curve = Enumerable.Range(0, n)
                .Select(i => (float) i / n * parameters.MaxGrappleDuration)
                .Select(Curve)
                .ToArray();
            //CustomDebug.DrawCurve(curve, Color.yellow, 2f);
            //Debug.DrawLine(kinematics.position, anchor, Color.green, 2f);
            Debug.Log((speed, parameters.FallGravity));
            
            Vector3 Curve(float t)
            {
                //t *= Mathf.Sign(deltaX);
                return new Vector3(
                    initialKinematics.position.x + initialKinematics.velocity.x * t,
                    initialKinematics.position.y + initialKinematics.velocity.y * t - 0.5f * parameters.FallGravity * t * t
                );
            }
        };
    }

    public override MovementState ProcessInterrupts(ref KinematicState<Vector2> kinematics, IEnumerable<IInterrupt> interrupts)
    {
        if (interrupts.FirstOrDefault(i => i is ICollision) is ICollision collision)
        {
            if (collision.Normal != Vector2.down)
            {
                Vector2 incomingDirection = (anchor - kinematics.position).normalized;
                Vector2 slideDirection = Vector3.Cross(collision.Normal, Vector3.Cross(incomingDirection, collision.Normal)).normalized;
                return new ImpactState(parameters, player, slideDirection);
            }
        }
        
        if (base.ProcessInterrupts(ref kinematics, interrupts) is { } newState && newState != this)
            return newState;
        
        if (interrupts.Any(i => i is ICollision))
            return new FallingState(parameters, player, parameters.FallGravity);
        
        return this;
    }

    public override MovementState UpdateKinematics(ref float t, ref KinematicState<Vector2> kinematics, out KinematicSegment<Vector2>[] motion)
    {
        onFirstUpdate?.Invoke(ref kinematics);
        
        /*Vector2 direction = anchor - kinematics.position;
        float distance = direction.magnitude;

        kinematics.velocity = parameters.GrappleSpeed / distance * direction;
        float moveTime = Mathf.Min(t, distance / parameters.GrappleSpeed);*/

        motion = ApplyMotionCurves(
            t,
            ref kinematics,
            (float t, ref KinematicState<float> kinematics) => new[] { LinearMotionCurve(t, ref kinematics) },
            (float t, ref KinematicState<float> kinematics) => FallingCurve(t, -parameters.TerminalVelocity, parameters.FallGravity, ref kinematics)
        );
        t -= t;

        if (t > 0f)
            return new FallingState(parameters, player, parameters.FallGravity);

        return this;
    }
}
