using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class SwingingState : MovementState
{
    private Vector2 anchor;
    private float radius;
    private Action<KinematicState<Vector2>> onFirstUpdate;

    public SwingingState(MovementParameters movementParameters, IPlayerInfo playerInfo, Vector2 anchor) : base(movementParameters, playerInfo)
    {
        this.anchor = anchor;
        onFirstUpdate = kinematics =>
        {
            onFirstUpdate = null;
            radius = GetAnchoredRadius(kinematics, this.anchor);
        };
    }

    public override MovementState ProcessInterrupts(ref KinematicState<Vector2> kinematics, IEnumerable<IInterrupt> interrupts)
    {
        if (interrupts.Any(i => i is AnchorInterrupt))
            return new FallingState(parameters, player, parameters.FallGravity);
        
        return base.ProcessInterrupts(ref kinematics, interrupts);
    }

    public override MovementState UpdateKinematics(ref float t, ref KinematicState<Vector2> kinematics, out KinematicSegment<Vector2>[] motion)
    {
        onFirstUpdate?.Invoke(kinematics);
        
        float omega = Mathf.Sqrt(parameters.FallGravity / radius);
        float b = 5f * omega / Mathf.Sqrt(Mathf.PI * Mathf.PI * parameters.DeadSwingCount * parameters.DeadSwingCount + 25f);
        float alpha = Mathf.Sqrt(omega * omega - b * b);
        
        Vector2 ropeDirection = (kinematics.position - anchor).normalized;
        Vector2 velocityDirection = new Vector2(-ropeDirection.y, ropeDirection.x);
        Vector2 tangentialVelocity = Vector3.Project(kinematics.velocity, velocityDirection);
        
        float angle = Vector2.SignedAngle(Vector2.down, ropeDirection);
        float angularVelocity = Mathf.Rad2Deg * Vector2.Dot(tangentialVelocity, velocityDirection) / radius;
                    
        float c1 = Mathf.Deg2Rad * angle;
        float c2 = (Mathf.Deg2Rad * angularVelocity + b * c1) / alpha;
        
        float previousAngle = angle;
        UnderdampedPendulumCurve(Time.deltaTime, ref angle, ref angularVelocity);

        float swing = parameters.AngularAcceleration * player.Aim.x;
        if (swing != 0f && (angle >= 0f && angularVelocity >= 0f || angle <= 0f && angularVelocity <= 0f || angle >= 0f && angularVelocity <= 0f && swing < 0f || angle <= 0f && angularVelocity >= 0f && swing > 0f))
        {
            (float v1, float v2) = MaxAngularVelocity(angle);
            if (swing > 0f && angularVelocity < Mathf.Max(v1, v2))
            {
                angularVelocity = Mathf.Min(angularVelocity + swing * Time.deltaTime, Mathf.Max(v1, v2));
            }
            else if (swing < 0f && angularVelocity > Mathf.Min(v1, v2))
            {
                angularVelocity = Mathf.Max(angularVelocity + swing * Time.deltaTime, Mathf.Min(v1, v2));
            }
        }
        
        Vector2 rope = radius * (Quaternion.Euler(0f, 0f, angle - previousAngle) * ropeDirection);
        kinematics.position = anchor + rope;
        kinematics.velocity = Mathf.Deg2Rad * angularVelocity * radius * new Vector2(-rope.y, rope.x).normalized;
        
        Debug.DrawLine(anchor, kinematics.position, Color.blue);
        
        motion = null;
        t = 0f;
        return this;
        
        void UnderdampedPendulumCurve(float t, ref float angle, ref float angularVelocity)
        {
            angle = Mathf.Rad2Deg * Mathf.Exp(-b * t) * (c1 * Mathf.Cos(alpha * t) + c2 * Mathf.Sin(alpha * t));
            angularVelocity = Mathf.Rad2Deg * Mathf.Exp(-b * t) * ((c2 * alpha - b * c1) * Mathf.Cos(alpha * t) - (b * c2 + c1 * alpha) * Mathf.Sin(alpha * t));
        }

        (float, float) MaxAngularVelocity(float angle)
        {
            angle *= Mathf.Deg2Rad;
            float c3 = Mathf.Deg2Rad * parameters.MaxSwingAngle / Mathf.Sqrt(c1 * c1 + c2 * c2);

            float sqrt = Mathf.Sqrt(c3 * c3 * (c1 * c1 + c2 * c2) - angle * angle);
            float t1 = Mathf.Acos((c1 * angle - c2 * sqrt) / (c3 * (c1 * c1 + c2 * c2))) / alpha;
            float t2 = -Mathf.Acos((c1 * angle + c2 * sqrt) / (c3 * (c1 * c1 + c2 * c2))) / alpha;
            t1 *= (c2 > 0f ? -1f : 1f) * (angle > c3 * angle ? -1f : 1f);
            t2 *= (c2 > 0f ? -1f : 1f) * (angle < -c3 * angle ? -1f : 1f);

            return (Mathf.Rad2Deg * UndampedAngularVelocityCurve(t1), Mathf.Rad2Deg * UndampedAngularVelocityCurve(t2));

            float UndampedAngularVelocityCurve(float t)
            {
                return alpha * c3 * (c2 * Mathf.Cos(alpha * t) - c1 * Mathf.Sin(alpha * t));
            }
        }
    }
}
