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

        motion = default;
        while (t > 0f)
        {
            innerState = innerState.UpdateKinematics(ref t, ref kinematics, out motion);
        }
        if (Vector2.Distance(kinematics.position, anchor) >= radius)
        {
            innerState = initialInnerState; 
            t = ComputeCircleIntersectionTime(initialKinematics, kinematics, motion);
            kinematics = initialKinematics;
            while (t > 0f)
            {
                innerState = innerState.UpdateKinematics(ref t, ref kinematics, out motion);
            }

            EditorApplication.isPaused = true;
        }

        return this;
    }

    private float ComputeCircleIntersectionTime(KinematicState<Vector2> initialKinematics, KinematicState<Vector2> finalKinematics, KinematicSegment<Vector2>[] motion)
    {
        float elapsedTime = 0f;
        foreach (KinematicSegment<Vector2> segment in motion)
        {
            IEnumerable<float> intersectionTimes = GetQuarticRoots(segment)
                    .Where(t => t > -CustomMath.EPSILON && t <= segment.duration)
                    .Select(t => (float) t + elapsedTime);

            if (intersectionTimes.Any())
            {
                Debug.Log("Quartic");
                return intersectionTimes.Min();
            }

            elapsedTime += segment.duration;
        }

        // Failsafe in case no quartic solution is found (can happen when moving in a straight line or when initial position is outside the circle for some reason) 
        Debug.Log("Linear");
        return (float) GetQuadraticRoots()
            .Select(t => t * Time.deltaTime)
            .OrderBy(t => t)
            .LastOrDefault();

        double[] GetQuarticRoots(KinematicSegment<Vector2> segment)
        {
            double a = -(segment.acceleration.x * segment.acceleration.x + parameters.FallGravity * parameters.FallGravity) / 4d;
            double b = parameters.FallGravity * segment.initialState.velocity.y - segment.acceleration.x * segment.initialState.velocity.x;
            double c = segment.acceleration.x * (anchor.x - segment.initialState.position.x) + parameters.FallGravity * (segment.initialState.position.y - anchor.y) - segment.initialState.velocity.x * segment.initialState.velocity.x - segment.initialState.velocity.y * segment.initialState.velocity.y;
            double d = 2d * (segment.initialState.velocity.x * (anchor.x - segment.initialState.position.x) + segment.initialState.velocity.y * (anchor.y - segment.initialState.position.y));
            double e = -anchor.x * anchor.x + 2d * anchor.x * segment.initialState.position.x - anchor.y * anchor.y + 2d * anchor.y * segment.initialState.position.y + radius * radius - segment.initialState.position.x * segment.initialState.position.x - segment.initialState.position.y * segment.initialState.position.y;
            return CustomMath.SolveQuartic(a, b, c, d, e);
        }

        double[] GetQuadraticRoots()
        {
            Vector2 d = finalKinematics.position - initialKinematics.position;
            Vector2 f = initialKinematics.position - anchor;
            double a = Vector2.Dot(d, d);
            double b = 2d * Vector2.Dot(f, d);
            double c = Vector2.Dot(f, f) - radius * radius;
            return CustomMath.SolveQuadratic(a, b, c);
        }
    }
}
