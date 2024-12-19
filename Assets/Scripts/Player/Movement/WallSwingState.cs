using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

public class WallSwingState : SwingingState
{
    private delegate void WallSwingInitializer(ref KinematicState<Vector2> kinematics);
    
    private WallSwingInitializer onFirstUpdate;
    
    public WallSwingState(MovementParameters movementParameters, IPlayerInfo playerInfo, Vector2 anchor) : base(movementParameters, playerInfo, anchor)
    {
        onFirstUpdate = (ref KinematicState<Vector2> kinematics) =>
        {
            onFirstUpdate = null;
            radius = GetAnchoredRadius(kinematics, anchor);

            Vector2 ropeDirection = (kinematics.position - anchor).normalized;
            Vector2 velocityDirection = new Vector2(-ropeDirection.y, ropeDirection.x);
            
            float angle = Vector2.SignedAngle(Vector2.down, ropeDirection);
            float initialAngularVelocity = GetMaximalAngularVelocity(angle);
            Vector2 initialVelocity = initialAngularVelocity * radius * velocityDirection;

            kinematics.velocity = initialVelocity;
        };
    }

    public override MovementState ProcessInterrupts(ref KinematicState<Vector2> kinematics, IEnumerable<IInterrupt> interrupts)
    {
        if (interrupts.Any(i => i is ICollision))
            return new SwingingState(parameters, player, anchor);

        return base.ProcessInterrupts(ref kinematics, interrupts);
    }

    public override MovementState UpdateKinematics(ref float t, ref KinematicState<Vector2> kinematics, out KinematicSegment<Vector2>[] motion)
    {
        onFirstUpdate?.Invoke(ref kinematics);
        
        Vector2 ropeDirection = (kinematics.position - anchor).normalized;
        Vector2 velocityDirection = new Vector2(-ropeDirection.y, ropeDirection.x);
        Vector2 tangentialVelocity = Vector3.Project(kinematics.velocity, velocityDirection);
        
        float angle = Vector2.SignedAngle(Vector2.down, ropeDirection);
        float angularVelocity = Mathf.Rad2Deg * Vector2.Dot(tangentialVelocity, velocityDirection) / radius;
        
        float previousAngle = angle;
        IdealPendulumCurve(ref t, ref angle, ref angularVelocity);
        
        Vector2 rope = radius * (Quaternion.Euler(0f, 0f, angle - previousAngle) * ropeDirection);
        kinematics.position = anchor + rope;
        kinematics.velocity = Mathf.Deg2Rad * angularVelocity * radius * new Vector2(-rope.y, rope.x).normalized;

        Debug.DrawLine(anchor, kinematics.position, Color.blue);
        Debug.DrawRay(anchor, radius * (Quaternion.Euler(0f, 0f, 90f * Math.Sign(angle)) * Vector2.down), Color.yellow);
 
        motion = null;
        return this;

        void IdealPendulumCurve(ref float t, ref float angle, ref float angularVelocity)
        {
            float maxAngle = Mathf.Deg2Rad * 80f;
            
            float omega = Omega;
            float b = B(omega);
            float alpha = Alpha(omega, b);

            float c5 = Mathf.Clamp(Mathf.Deg2Rad * angle, -maxAngle, maxAngle);
            float c6 = Math.Sign(angularVelocity / alpha) * Mathf.Sqrt(maxAngle * maxAngle - c5 * c5);
            
            angle = Mathf.Rad2Deg * (c5 * Mathf.Cos(alpha * t) + c6 * Mathf.Sin(alpha * t));
            angularVelocity = Mathf.Rad2Deg * (alpha * (c6 * Mathf.Cos(alpha * t) - c5 * Mathf.Sin(alpha * t)));
            t = 0f;
        }
    }

    private float GetMaximalAngularVelocity(float angle)
    {
        float maxAngle = Mathf.Deg2Rad * 80f;
        
        float omega = Omega;
        float b = B(omega);
        float alpha = Alpha(omega, b);

        float c5 = Mathf.Deg2Rad * angle;
        float c6 = Math.Sign(angle / alpha) * Mathf.Sqrt(maxAngle * maxAngle - c5 * c5);

        return Math.Sign(alpha * c6) * Mathf.Rad2Deg * parameters.JumpVelocity / radius;
        return Mathf.Rad2Deg * alpha * c6;
    }
}
