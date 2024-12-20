using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class GrappleState : MovementState
{
    protected Vector2 anchor;
    private Action<KinematicState<Vector2>> onFirstUpdate;
    
    public GrappleState(MovementParameters movementParameters, IPlayerInfo playerInfo, Vector2 anchor) : base(movementParameters, playerInfo)
    {
        this.anchor = anchor;
        onFirstUpdate = kinematics =>
        {
            onFirstUpdate = null;

            float speed = parameters.RopeLength / parameters.MaxGrappleDuration + parameters.FallGravity * parameters.MaxGrappleDuration;
            float deltaX = anchor.x - kinematics.position.x;
            float A = (anchor.y - kinematics.position.y) / deltaX;
            float B = parameters.FallGravity * deltaX / (speed * speed);
            float angle = Mathf.Atan((float) CustomMath.SolveQuadratic(-B, 1f, -(A + B))[0]);
            Vector2 initialVelocity = speed * new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));

            int n = 101;
            Vector3[] curve = Enumerable.Range(0, n)
                .Select(i => (float) i / n * parameters.MaxGrappleDuration)
                .Select(Curve)
                .ToArray();
            CustomDebug.DrawCurve(curve, Color.yellow, 2f);
            Debug.DrawLine(kinematics.position, anchor, Color.green, 2f);
            Debug.Log((Mathf.Rad2Deg * angle, speed, initialVelocity, initialVelocity.magnitude));
            
            Vector3 Curve(float t)
            {
                t *= Mathf.Sign(deltaX);
                return new Vector3(
                    kinematics.position.x + initialVelocity.x * t,
                    kinematics.position.y + initialVelocity.y * t - parameters.FallGravity * t * t
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
        onFirstUpdate?.Invoke(kinematics);
        
        Vector2 direction = anchor - kinematics.position;
        float distance = direction.magnitude;

        kinematics.velocity = parameters.GrappleSpeed / distance * direction;
        float moveTime = Mathf.Min(t, distance / parameters.GrappleSpeed);

        motion = ApplyMotionCurves(
            moveTime,
            ref kinematics,
            (float t, ref KinematicState<float> kinematics) => new[] { LinearMotionCurve(t, ref kinematics) },
            (float t, ref KinematicState<float> kinematics) => new[] { LinearMotionCurve(t, ref kinematics) }
        );
        t -= moveTime;

        if (t > 0f)
            return new FallingState(parameters, player, parameters.FallGravity);

        return this;
    }
}
