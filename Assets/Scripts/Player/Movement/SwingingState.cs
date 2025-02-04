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
    private const double BISECTION_RANGE_OFFSET = 0.0001d;
    private const double BISECTION_PRECISION = 1e-14d;
    private const int BISECTION_MAX_ITERATIONS = 100;

    public Vector2 Anchor { get; }
    protected float radius;
    private Action<KinematicState<Vector2>> onFirstUpdate;

    public SwingingState(MovementParameters movementParameters, IPlayerInfo playerInfo, Vector2 anchor) : base(movementParameters, playerInfo)
    {
        this.Anchor = anchor;
        onFirstUpdate = kinematics =>
        {
            onFirstUpdate = null;
            radius = GetAnchoredRadius(kinematics, this.Anchor);
        };
    }

    public override MovementState ProcessInterrupts(ref KinematicState<Vector2> kinematics, IEnumerable<IInterrupt> interrupts)
    {
        if (interrupts.Any(i => i is JumpInterrupt { type: JumpInterrupt.Type.Started }))
        {
            if (player.WallCheck() != 0)
                return new WallSwingState(parameters, player, Anchor);
            
            return new JumpingState(parameters, player, kinematics);
        }
        
        if (interrupts.Any(i => i is AnchorInterrupt))
            return new FallingState(parameters, player, parameters.FallGravity);
        
        if (interrupts.Any(i => i is GrappleInterrupt) && player.WallCheck() != 0)
            return new VerticalGrappleState(parameters, player, Anchor);
        
        return base.ProcessInterrupts(ref kinematics, interrupts);
    }

    public override MovementState UpdateKinematics(ref float t, ref KinematicState<Vector2> kinematics, out KinematicSegment<Vector2>[] motion)
    {
        onFirstUpdate?.Invoke(kinematics);

        Vector2 ropeDirection = (kinematics.position - Anchor).normalized;
        Vector2 velocityDirection = new Vector2(-ropeDirection.y, ropeDirection.x);
        Vector2 tangentialVelocity = Vector3.Project(kinematics.velocity, velocityDirection);
        
        float angle = Vector2.SignedAngle(Vector2.down, ropeDirection);
        float angularVelocity = Mathf.Rad2Deg * Vector2.Dot(tangentialVelocity, velocityDirection) / radius;

        float previousAngle = angle;
        DrivenUnderdampedPendulumCurve(ref t, ref angle, ref angularVelocity);

        Vector2 rope = radius * (Quaternion.Euler(0f, 0f, angle - previousAngle) * ropeDirection);
        kinematics.position = Anchor + rope;
        kinematics.velocity = Mathf.Deg2Rad * angularVelocity * radius * new Vector2(-rope.y, rope.x).normalized;
        
        Debug.DrawLine(Anchor, kinematics.position, Color.blue);
        player.ShowRope(true);
        player.DrawRope(Anchor, kinematics.position);
        Debug.DrawRay(Anchor, radius * (Quaternion.Euler(0f, 0f, parameters.MaxSwingAngle) * Vector2.down), Color.yellow);
        Debug.DrawRay(Anchor, radius * (Quaternion.Euler(0f, 0f, -parameters.MaxSwingAngle) * Vector2.down), Color.yellow);
        
        motion = null;
        return this;

        void DrivenUnderdampedPendulumCurve(ref float t, ref float angle, ref float angularVelocity)
        {
            float omega = Omega(parameters.FallGravity);
            float b = B(omega);
            float alpha = Alpha(omega, b);

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

                return Position((float) t0, (float) c3, (float) c4) - (double) Mathf.Sign(Mathf.Deg2Rad * velocity) * Mathf.Deg2Rad * parameters.MaxSwingAngle;
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
                        searchRange[0] + BISECTION_RANGE_OFFSET, 
                        searchRange[1] - BISECTION_RANGE_OFFSET, 
                        BISECTION_PRECISION, 
                        BISECTION_MAX_ITERATIONS, 
                        out double output))
                    return Mathf.Rad2Deg * (float) output;

                return 0f;
            }
        }
    }
    
    protected float Omega(float gravity) => Mathf.Sqrt(gravity / radius);
    
    protected float B(float omega) => 5f * omega / Mathf.Sqrt(Mathf.PI * Mathf.PI * parameters.DeadSwingCount * parameters.DeadSwingCount + 25f);
    
    protected float Alpha(float omega, float b) => Mathf.Sqrt(omega * omega - b * b);
}
