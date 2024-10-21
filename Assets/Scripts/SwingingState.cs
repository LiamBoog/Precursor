using System;
using System.Collections.Generic;
using System.Linq;
using MathNet.Numerics.RootFinding;
using UnityEngine;
using Quaternion = UnityEngine.Quaternion;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;

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

        Vector2 ropeDirection = (kinematics.position - anchor).normalized;
        Vector2 velocityDirection = new Vector2(-ropeDirection.y, ropeDirection.x);
        Vector2 tangentialVelocity = Vector3.Project(kinematics.velocity, velocityDirection);
        
        float angle = Vector2.SignedAngle(Vector2.down, ropeDirection);
        float angularVelocity = Mathf.Rad2Deg * Vector2.Dot(tangentialVelocity, velocityDirection) / radius;

        float previousAngle = angle;
        DrivenUnderdampedPendulumCurve(ref t, ref angle, ref angularVelocity);

        Vector2 rope = radius * (Quaternion.Euler(0f, 0f, angle - previousAngle) * ropeDirection);
        kinematics.position = anchor + rope;
        kinematics.velocity = Mathf.Deg2Rad * angularVelocity * radius * new Vector2(-rope.y, rope.x).normalized;
        
        Debug.DrawLine(anchor, kinematics.position, Color.blue);
        Debug.DrawRay(anchor, radius * (Quaternion.Euler(0f, 0f, parameters.MaxSwingAngle) * Vector2.down), Color.yellow);
        Debug.DrawRay(anchor, radius * (Quaternion.Euler(0f, 0f, -parameters.MaxSwingAngle) * Vector2.down), Color.yellow);
        
        motion = null;
        t = 0f;
        return this;

        void DrivenUnderdampedPendulumCurve(ref float t, ref float angle, ref float angularVelocity)
        {
            float omega = Mathf.Sqrt(parameters.FallGravity / radius);
            float b = 5f * omega / Mathf.Sqrt(Mathf.PI * Mathf.PI * parameters.DeadSwingCount * parameters.DeadSwingCount + 25f);
            float alpha = Mathf.Sqrt(omega * omega - b * b);

            float position = angle;
            float velocity = angularVelocity;
            
            float swing = parameters.AngularAcceleration * player.Aim.x;
            swing = OptimalSwing();

            Curve(t, ref angle, ref angularVelocity);
            t = 0f;
            return;
            
            float Position(float t, float c3, float c4)
            {
                return Mathf.Rad2Deg * (Mathf.Exp(-b * t) * (c3 * Mathf.Cos(alpha * t) + c4 * Mathf.Sin(alpha * t)) + Mathf.Deg2Rad * swing / (omega * omega));
            }

            void Curve(float t, ref float angle, ref float angularVelocity)
            {
                float c3 = Mathf.Deg2Rad * position - Mathf.Deg2Rad * swing / (omega * omega);
                float c4 = (Mathf.Deg2Rad * velocity + b * c3) / alpha;

                angle = Position(t, c3, c4);
                angularVelocity = Mathf.Rad2Deg * (Mathf.Exp(-b * t) * ((c4 * alpha - b * c3) * Mathf.Cos(alpha * t) - (b * c4 + c3 * alpha) * Mathf.Sin(alpha * t)));
            }
            
            double AngularAccelerationOptimizer(double angularAcceleration)
            {
                double c3 = Mathf.Deg2Rad * position - Mathf.Deg2Rad * angularAcceleration / (omega * omega);
                double c4 = (Mathf.Deg2Rad * velocity + b * c3) / alpha;
                double t0 = NextPeak((float) angularAcceleration).Item1;

                return Math.Exp(-b * t0) * (c3 * Math.Cos(alpha * t0) + c4 * Math.Sin(alpha * t0)) + angularAcceleration / (omega * omega) - (double) Mathf.Sign(Mathf.Deg2Rad * velocity) * Mathf.Deg2Rad * parameters.MaxSwingAngle;
            }

            (float, float) NextPeak(float angularAcceleration)
            {
                float c3 = Mathf.Deg2Rad * position - Mathf.Deg2Rad * angularAcceleration / (omega * omega);
                float c4 = (Mathf.Deg2Rad * velocity + b * c3) / alpha;
                
                float y = c4 * alpha - b * c3;
                float x = b * c4 + c3 * alpha;
                float tangent = y / x; // Atan handles infinity correctly so division by zero isn't an issue

                float time = (Mathf.Atan(tangent) + (tangent < 0f ? Mathf.PI : 0f)) / alpha;
                float value = Position(time, c3, c4);
                return (time, value);
            }

            float OptimalSwing()
            {
                if (position * velocity < 0f && swing * velocity < 0f)
                    return 0f;

                if (Mathf.Abs(NextPeak(swing).Item2) <= parameters.MaxSwingAngle)
                    return swing;
                    
                float discontinuity = omega * omega * (Mathf.Deg2Rad * velocity / (b + alpha * alpha / b) + Mathf.Deg2Rad * position);
                float[] searchRange = new[] {0f, discontinuity}
                    .OrderBy(A => A)
                    .ToArray();

                if (Bisection.TryFindRoot(
                        AngularAccelerationOptimizer, 
                        searchRange[0] + 0.0001d, 
                        searchRange[1] - 0.0001d, 
                        1e-14d, 
                        100, 
                        out double output))
                    return Mathf.Rad2Deg * (float) output;

                return 0f;
            }
        }
    }
}
