using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class AnchoredState : MovementState
{
    private Vector2 anchor;
    private float radius;
    private MovementState innerState;
    private Action<KinematicState<Vector2>> onFirstUpdate;
    
    public AnchoredState(MovementParameters movementParameters, IPlayerInfo playerInfo, Vector2 anchor, MovementState previousState) : base(movementParameters, playerInfo)
    {
        this.anchor = anchor;
        onFirstUpdate = kinematics =>
        {
            onFirstUpdate = null;
            radius = Mathf.Max(parameters.MinRopeLengthFactor * parameters.RopeLength, (kinematics.position - this.anchor).magnitude);
        };

        Debug.Log(previousState);
        innerState = previousState is AnchoredState anchoredState ? anchoredState.innerState : previousState;
    }

    public override MovementState ProcessInterrupts(ref KinematicState<Vector2> kinematics, IEnumerable<IInterrupt> interrupts)
    {
        if (interrupts.Any(i => i is AnchorInterrupt { type: AnchorInterrupt.Type.Cancelled }))
            return innerState;
        
        innerState = innerState.ProcessInterrupts(ref kinematics, interrupts.Where(i => i is not AnchorInterrupt));
        return base.ProcessInterrupts(ref kinematics, interrupts);
    }

    public override MovementState UpdateKinematics(ref float t, ref KinematicState<Vector2> kinematics, out KinematicSegment<Vector2>[] motion)
    {
        onFirstUpdate?.Invoke(kinematics);

        MovementState initialInnerState = innerState;
        KinematicState<Vector2> initialKinematics = kinematics;

        motion = default;
        while (t > 0f)
        {
            innerState = innerState.UpdateKinematics(ref t, ref kinematics, out motion);
        }
        Debug.DrawLine(Vector3.zero, initialKinematics.position, Color.green);
        Debug.DrawLine(Vector3.zero, kinematics.position, Color.magenta);
        Debug.DrawLine(initialKinematics.position, kinematics.position, Color.cyan);
        if (Vector2.Distance(kinematics.position, anchor) >= radius && Vector2.Distance(kinematics.position, anchor) - Vector2.Distance(initialKinematics.position, anchor) > 0f)
        {
            innerState = initialInnerState; 
            t = ComputeCircleIntersectionTime(initialKinematics, kinematics, motion);
            kinematics = initialKinematics;
            while (t > 0f)
            {
                innerState = innerState.UpdateKinematics(ref t, ref kinematics, out motion);
            }

            kinematics.velocity = Vector2.zero;
            
            CustomDebug.DrawCircle2D(anchor, radius, Color.blue);

            //EditorApplication.isPaused = true;
        }

        if (innerState is FallingState)
        {
            // Start swinging
        }
        
        Debug.DrawLine(anchor, kinematics.position, Color.blue);

        return this;
    }

    private float ComputeCircleIntersectionTime(KinematicState<Vector2> initialKinematics, KinematicState<Vector2> finalKinematics, KinematicSegment<Vector2>[] motion)
    {
        float elapsedTime = 0f;
        foreach (KinematicSegment<Vector2> segment in motion)
        {
            float[] intersectionTimes = GetQuarticRoots(segment.initialState.position, segment.initialState.velocity, segment.acceleration)
                    .Where(t => t > -CustomMath.EPSILON && t <= segment.duration)
                    .Select(t => (float) t + elapsedTime)
                    .OrderBy(t => t)
                    .ToArray();

            if (intersectionTimes.Length > 0)
            {
                Vector3 Curve(float t) => segment.initialState.position + segment.initialState.velocity * t + 0.5f * segment.acceleration * t * t;

                CustomDebug.DrawCurve(
                    Enumerable
                    .Range(-100, 200)
                    .Select(i => i * 0.001f)
                    .Select(Curve)
                    .ToArray(),
                    Color.yellow,
                    0.5f);
                Debug.DrawLine(Vector3.zero, Curve(intersectionTimes[0] - elapsedTime), Color.yellow);
                Debug.Log("Quartic");
                return intersectionTimes[0];
            }

            elapsedTime += segment.duration;
        }

        // Failsafe in case no quartic solution is found (can happen when moving in a straight line or when initial position is outside the circle for some reason) 
        Debug.Log("Linear");
        return (float) GetQuadraticRoots(initialKinematics.position, finalKinematics.position)
            .Select(t => t * Time.deltaTime)
            .OrderBy(t => t)
            .LastOrDefault();

        double[] GetQuarticRoots(Vector2 position, Vector2 velocity, Vector2 acceleration)
        {
            Vector2 f = new Vector2(-1f, 1f) * acceleration;
            Vector2 g = new Vector2(1f, -1f) * anchor;
            Vector2 h = new Vector2(-1f, 1f) * position;
            
            double a = -0.25d * Vector2.Dot(acceleration, acceleration);
            double b = Vector2.Dot(f, velocity);
            double c = Vector2.Dot(acceleration, g + h) - Vector2.Dot(velocity, velocity);
            double d = 2d * Vector2.Dot(velocity, anchor - position);
            double e = 2d * Vector2.Dot(anchor, position) - Vector2.Dot(position, position) - Vector2.Dot(anchor, anchor) + radius * radius;
            
            return CustomMath.SolveQuartic(a, b, c, d, e);
        }

        double[] GetQuadraticRoots(Vector2 initialPosition, Vector2 finalPosition)
        {
            Vector2 d = finalPosition - initialPosition;
            Vector2 f = initialPosition - anchor;
            
            double a = Vector2.Dot(d, d);
            double b = 2d * Vector2.Dot(f, d);
            double c = Vector2.Dot(f, f) - radius * radius;
            
            return CustomMath.SolveQuadratic(a, b, c);
        }
    }
}
