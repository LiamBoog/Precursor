using System.Collections.Generic;
using UnityEngine;

public class ImpactState : MovementState
{
    private Vector2 direction;
    private float remainingTime;
    private float elapsedTime;
    
    public ImpactState(MovementParameters movementParameters, IPlayerInfo playerInfo, Vector2 direction) : base(movementParameters, playerInfo)
    {
        this.direction = direction;
    }

    public override MovementState ProcessInterrupts(ref KinematicState<Vector2> kinematics, IEnumerable<IInterrupt> interrupts)
    {
        return base.ProcessInterrupts(ref kinematics, interrupts);
    }

    public override MovementState UpdateKinematics(ref float t, ref KinematicState<Vector2> kinematics, out KinematicSegment<Vector2>[] motion)
    {
        motion = ImpactCurve(ref t, ref kinematics);

        if (t > 0f)
            return new FallingState(parameters, player, parameters.FallGravity);

        return this;
    }

    private KinematicSegment<Vector2>[] ImpactCurve(ref float t, ref KinematicState<Vector2> kinematics)
    {
        List<KinematicSegment<Vector2>> output = new();
        if (elapsedTime < parameters.ImpactDelay)
        {
            output.Add(DelayCurve(ref t, ref kinematics));
        }

        if (elapsedTime < parameters.ImpactDelay + parameters.ImpactDuration)
        {
            output.Add(SlideCurve(ref t, ref kinematics));
        }

        if (elapsedTime < parameters.ImpactDelay + parameters.ImpactDuration + parameters.ImpactPause)
        {
            output.Add(PauseCurve(ref t, ref kinematics));   
        }

        return output.ToArray();

        KinematicSegment<Vector2> DelayCurve(ref float t, ref KinematicState<Vector2> kinematics)
        {
            float duration = Mathf.Min(t, parameters.ImpactDelay - elapsedTime);
            elapsedTime += duration;
            t -= duration;
            kinematics.velocity = Vector2.zero;
            return new(kinematics, Vector2.zero, duration);
        }

        KinematicSegment<Vector2> SlideCurve(ref float t, ref KinematicState<Vector2> kinematics)
        {
            float duration = Mathf.Min(t, parameters.ImpactDelay + parameters.ImpactDuration - elapsedTime);
            elapsedTime += duration;
            t -= duration;
            kinematics.velocity = parameters.GrappleSpeed * direction;
            KinematicSegment<Vector2> output = new(kinematics, Vector2.zero, duration);
            kinematics.position += kinematics.velocity * duration;
            return output;
        }

        KinematicSegment<Vector2> PauseCurve(ref float t, ref KinematicState<Vector2> kinematics)
        {
            float duration = Mathf.Min(t, parameters.ImpactDelay + parameters.ImpactDuration + parameters.ImpactPause - elapsedTime);
            elapsedTime += duration;
            t -= duration;
            kinematics.velocity = Vector2.zero;
            return new(kinematics, Vector2.zero, duration);
        }
    }
}
