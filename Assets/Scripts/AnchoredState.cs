using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
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

        innerState = previousState;
    }

    public override MovementState UpdateKinematics(ref float t, ref KinematicState<Vector2> kinematics, out KinematicSegment<Vector2>[] motion)
    {
        onFirstUpdate?.Invoke(kinematics);

        MovementState initialInnerState = innerState;
        KinematicState<Vector2> initialKinematics = kinematics;
        float totalT = t;

        motion = default;
        while (t > 0f)
        {
            innerState = innerState.UpdateKinematics(ref t, ref kinematics, out motion);
        }
        if (Vector2.Distance(kinematics.position, anchor) >= radius)
        {
            innerState = initialInnerState; 
            Debug.DrawLine(initialKinematics.position, kinematics.position, Color.yellow);
            t = ComputeCircleIntersectionTime(initialKinematics, kinematics, motion);
            Debug.Log(t);
            kinematics = initialKinematics;
            while (t > 0f)
            {
                innerState = innerState.UpdateKinematics(ref t, ref kinematics, out motion);
            }
            //Debug.DrawLine(anchor, kinematics.position, Color.magenta);
            CustomDebug.DrawArc2D(kinematics.position, anchor, radius, 360f, Color.blue);

            EditorApplication.isPaused = true;
        }

        return this;
    }

    private float ComputeCircleIntersectionTime(KinematicState<Vector2> initialKinematics, KinematicState<Vector2> finalKinematics, KinematicSegment<Vector2>[] motion)
    {
        IEnumerable<float> intersectionPoints = new float[] {};
        IEnumerable<double> possibleRoots = Enumerable.Empty<double>();
        float elapsedTime = 0f;

        Color[] colours = { Color.red, Color.magenta, Color.blue, Color.cyan, Color.green, Color.yellow };
        int j = 0;
        foreach (KinematicSegment<Vector2> segment in motion)
        {
            double a = -(segment.acceleration.x * segment.acceleration.x + parameters.FallGravity * parameters.FallGravity) / 4d;
            double b = parameters.FallGravity * segment.initialState.velocity.y - segment.acceleration.x * segment.initialState.velocity.x;
            double c = segment.acceleration.x * (anchor.x - segment.initialState.position.x) + parameters.FallGravity * (segment.initialState.position.y - anchor.y) - segment.initialState.velocity.x * segment.initialState.velocity.x - segment.initialState.velocity.y * segment.initialState.velocity.y;
            double d = 2d * (segment.initialState.velocity.x * (anchor.x - segment.initialState.position.x) + segment.initialState.velocity.y * (anchor.y - segment.initialState.position.y));
            double e = -anchor.x * anchor.x + 2d * anchor.x * segment.initialState.position.x - anchor.y * anchor.y + 2d * anchor.y * segment.initialState.position.y + radius * radius - segment.initialState.position.x * segment.initialState.position.x - segment.initialState.position.y * segment.initialState.position.y;
            double[] roots = CustomMath.SolveQuartic(a, b, c, d, e);
            possibleRoots = possibleRoots.Concat(roots.Select(root => elapsedTime + root)).ToArray();

            intersectionPoints = intersectionPoints
                .Concat(roots
                    .Select(t => (float) (elapsedTime + t))
                    .Where(t => t >= 0f)
                )
                .ToArray();

            elapsedTime += segment.duration;
            
            Vector2 ParametricCurve(float t)
            {
                return segment.initialState.position + segment.initialState.velocity * t + 0.5f * segment.acceleration * t * t;
            }

            Vector3[] curve = Enumerable
                .Range(-1000, 2000)
                .Select(i => i * 0.001f)
                .Select(ParametricCurve)
                .Select(v => (Vector3) v)
                .ToArray();
            CustomDebug.DrawCurve(curve, colours[j]);
            Debug.DrawLine(Vector3.zero, segment.initialState.position, colours[j++]);
            foreach (double root in roots)
            {
                Debug.DrawLine(anchor, ParametricCurve((float) root), Color.cyan);
            }
        }
        
        foreach (double root in possibleRoots)
        {
            if (root < 0f)
            {
                KinematicState<float> xKinematics = new(initialKinematics.position.x, initialKinematics.velocity.x);
                KinematicState<float> yKinematics = new(initialKinematics.position.y, initialKinematics.velocity.y);
                AccelerationCurve((float) root, ref xKinematics, motion[0].acceleration.x);
                AccelerationCurve((float) root, ref yKinematics, motion[0].acceleration.y);
                //Debug.DrawLine(new Vector3(xKinematics.position, yKinematics.position), anchor, Color.red);
            }
            else    
            {
                float elapsed = 0f;
                int i = 0;
                while (elapsed + motion[i].duration < root)
                {
                    elapsed += motion[i++].duration;
                }
                
                KinematicState<float> xKinematics = new(motion[i].initialState.position.x, motion[i].initialState.velocity.x);
                KinematicState<float> yKinematics = new(motion[i].initialState.position.y, motion[i].initialState.velocity.y);
                AccelerationCurve((float) root - elapsedTime, ref xKinematics, motion[i].acceleration.x);
                AccelerationCurve((float) root - elapsedTime, ref yKinematics, motion[i].acceleration.y);
                //Debug.DrawLine(new Vector3(xKinematics.position, yKinematics.position), anchor, Color.cyan);
            }

            Debug.Log(root);
        }
        
        float t;
        if (intersectionPoints.Any())
        {
            t = intersectionPoints.Min();
            Debug.Log("Quartic");
        }
        else
        {
            // Failsafe in case no quartic solution is found (can happen when moving in a straight line or when initial position is outside the circle for some reason) 
            Vector2 d = finalKinematics.position - initialKinematics.position;
            Vector2 f = initialKinematics.position - anchor;
            double a = Vector2.Dot(d, d);
            double b = 2f * Vector2.Dot(f, d);
            double c = Vector2.Dot(f, f) - radius * radius;
            double[] roots = CustomMath.SolveQuadratic(a, b, c);
            t = (float) roots
                .Select(t => t * Time.deltaTime)
                .OrderBy(t => t)
                .LastOrDefault();
            Debug.Log("Linear");
        }

        return t;
    }
}
