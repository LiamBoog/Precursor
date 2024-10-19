using System;
using System.Collections.Generic;
using System.Linq;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;
using MathNet.Numerics.Optimization;
using MathNet.Numerics.RootFinding;
using UnityEditor;
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
        //UnderdampedPendulumCurve(Time.deltaTime, ref angle, ref angularVelocity);

        float swing = parameters.AngularAcceleration * player.Aim.x;
        if (swing != 0f && (angle >= 0f && angularVelocity >= 0f || angle <= 0f && angularVelocity <= 0f || angle >= 0f && angularVelocity <= 0f && swing < 0f || angle <= 0f && angularVelocity >= 0f && swing > 0f))
        {
            DrivenUnderdampedPendulumCurve(ref t, ref angle, ref angularVelocity);
            /*(float v1, float v2) = MaxAngularVelocity(angle);
            if (swing > 0f && angularVelocity < Mathf.Max(v1, v2))
            {
                angularVelocity = Mathf.Min(angularVelocity + swing * Time.deltaTime, Mathf.Max(v1, v2));
            }
            else if (swing < 0f && angularVelocity > Mathf.Min(v1, v2))
            {
                angularVelocity = Mathf.Max(angularVelocity + swing * Time.deltaTime, Mathf.Min(v1, v2));
            }*/
        }
        else
        {
            UnderdampedPendulumCurve(t, ref angle, ref angularVelocity);
        }

        Vector2 rope = radius * (Quaternion.Euler(0f, 0f, angle - previousAngle) * ropeDirection);
        kinematics.position = anchor + rope;
        kinematics.velocity = Mathf.Deg2Rad * angularVelocity * radius * new Vector2(-rope.y, rope.x).normalized;
        
        Debug.DrawLine(anchor, kinematics.position, Color.blue);
        Debug.DrawRay(anchor, radius * (Quaternion.Euler(0f, 0f, parameters.MaxSwingAngle) * Vector2.down), Color.yellow);
        Debug.DrawRay(anchor, radius * (Quaternion.Euler(0f, 0f, -parameters.MaxSwingAngle) * Vector2.down), Color.yellow);
        
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
        
        void DrivenUnderdampedPendulumCurve(ref float t, ref float angle, ref float angularVelocity)
        {
            float swing = parameters.AngularAcceleration * player.Aim.x;
            
            float c3 = c1 - Mathf.Deg2Rad * swing / (omega * omega);
            float c4 = (Mathf.Deg2Rad * angularVelocity + b * c3) / alpha;

            (int, float) peak = Enumerable.Range(0, 2)
                .Select(i => (i, (i * Mathf.PI - Mathf.Atan(-(c4 * alpha - b * c3) / (b * c4 + c3 * alpha))) / alpha))
                .OrderBy(pair => pair.Item2)
                .First(pair => pair.Item2 >= 0f);

            if (Mathf.Abs(Position(peak.Item2, angularVelocity, swing)) <= parameters.MaxSwingAngle)
            {
                angle = Position(t, angularVelocity, swing);
                angularVelocity = Velocity(t, angularVelocity, swing);
                t = 0f;
                return;
            }

            float discontinuity = omega * omega * (Mathf.Deg2Rad * angularVelocity / (b + alpha * alpha / b) + Mathf.Deg2Rad * angle);
            float[] searchRangeA = new[] {0f, discontinuity}
                .OrderBy(A => A)
                .ToArray();
            float[] searchRangeB = new[] {discontinuity, Mathf.Deg2Rad * swing}
                .OrderBy(A => A)
                .ToArray();

            float v = angularVelocity;
            if (!Bisection.TryFindRoot(A => AngularAccelerationOptimizer(A, v), searchRangeA[0] + 0.0001d, searchRangeA[1] - 0.0001d, 1e-14d, 100, out double optimalAngularAcceleration))
            {
                Bisection.TryFindRoot(A => AngularAccelerationOptimizer(A, v), searchRangeB[0] + 0.0001d, searchRangeB[1] - 0.0001d, 1e-14d, 100, out optimalAngularAcceleration);
            }

            angle = Position(t, angularVelocity, Mathf.Rad2Deg * (float) optimalAngularAcceleration);
            angularVelocity = Velocity(t, angularVelocity, Mathf.Rad2Deg * (float) optimalAngularAcceleration);
            t = 0f;
            return;
            
            float Position(float t, float angularVelocity, float angularAcceleration)
            {
                float c3 = c1 - Mathf.Deg2Rad * angularAcceleration / (omega * omega);
                float c4 = (Mathf.Deg2Rad * angularVelocity + b * c3) / alpha;

                return Mathf.Rad2Deg * (Mathf.Exp(-b * t) * (c3 * Mathf.Cos(alpha * t) + c4 * Mathf.Sin(alpha * t)) + Mathf.Deg2Rad * angularAcceleration / (omega * omega));
            }

            float Velocity(float t, float angularVelocity, float angularAcceleration)
            {
                float c3 = c1 - Mathf.Deg2Rad * angularAcceleration / (omega * omega);
                float c4 = (Mathf.Deg2Rad * angularVelocity + b * c3) / alpha;

                return Mathf.Rad2Deg * (Mathf.Exp(-b * t) * ((c4 * alpha - b * c3) * Mathf.Cos(alpha * t) - (b * c4 + c3 * alpha) * Mathf.Sin(alpha * t)));
            }
            
            double AngularAccelerationOptimizer(double A, double angularVelocity)
            {
                double c3 = c1 - A / (omega * omega);
                double c4 = (Mathf.Deg2Rad * angularVelocity + b * c3) / alpha;
                double t0 = (peak.Item1 * Math.PI - Math.Atan(-(c4 * alpha - b * c3) / (b * c4 + c3 * alpha))) / alpha;

                return Math.Exp(-b * t0) * (c3 * Math.Cos(alpha * t0) + c4 * Math.Sin(alpha * t0)) + A / (omega * omega) - (double) Mathf.Sign(Mathf.Deg2Rad * (float) angularVelocity) * Mathf.Deg2Rad * parameters.MaxSwingAngle;
            }
        }
    }

    [MenuItem("TEST/Solve")]
    public static void Solver()
    {
        float maxAngle = 5f;

        float angle = 3.48f;
        float angularVelocity = 2.79f;
        float angularAcceleration = 1f;

        float b = 0.22748585329f;
        float omega = 0.75f;
        float alpha = Mathf.Sqrt(omega * omega - b * b);
        
        float c1 = angle;
        float c3 = C3(angularAcceleration);
        float c4 = C4(angularAcceleration);
        
        int peak = Enumerable.Range(0, 2)
            .Select(i => (i, (i * Mathf.PI - Mathf.Atan(-(c4 * alpha - b * c3) / (b * c4 + c3 * alpha))) / alpha))
            .OrderBy(pair => pair.Item2)
            .First(pair => pair.Item2 >= 0f)
            .Item1;
        
        float discontinuity = omega * omega * (angularVelocity / (b + alpha * alpha / b) + angle);
        float[] searchRange = new[] {0f, discontinuity}
            .OrderBy(A => A)
            .ToArray();

        float optimalAngularAcceleration = (float) Bisection.FindRoot(Function, searchRange[0], searchRange[1]);
        
        c3 = C3(optimalAngularAcceleration);
        c4 = C4(optimalAngularAcceleration);
        float peakTime = Enumerable.Range(0, 2)
            .Select(i => (i, (i * Mathf.PI - Mathf.Atan(-(c4 * alpha - b * c3) / (b * c4 + c3 * alpha))) / alpha))
            .OrderBy(pair => pair.Item2)
            .First(pair => pair.Item2 >= 0f)
            .Item2;
        Debug.Log(DrivenUnderdampedPendulumCurve(peakTime, optimalAngularAcceleration));

        float DrivenUnderdampedPendulumCurve(float t, float A)
        {
            float c3 = C3(A);
            float c4 = C4(A);

            return Mathf.Exp(-b * t) * (c3 * Mathf.Cos(alpha * t) + c4 * Mathf.Sin(alpha * t)) + A / (omega * omega);
        }

        float C3(float A) => c1 - A / (omega * omega);
        float C4(float A) => (angularVelocity + b * C3(A)) / alpha;

        double Function(double A)
        {
            double c3 = c1 - A / (omega * omega);
            double c4 = (angularVelocity + b * c3) / alpha;
            double t0 = (peak * Math.PI - Math.Atan(-(c4 * alpha - b * c3) / (b * c4 + c3 * alpha))) / alpha;

            return Math.Exp(-b * t0) * (c3 * Math.Cos(alpha * t0) + c4 * Math.Sin(alpha * t0)) + A / (omega * omega) - maxAngle;
        }
    }
}
