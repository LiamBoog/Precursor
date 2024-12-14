using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ImpactState : MovementState
{
    private delegate void ImpactInitializer(ref KinematicState<Vector2> kinematics);
    
    private Vector2 direction;
    private ImpactInitializer onFirstUpdate;

    public ImpactState(MovementParameters movementParameters, IPlayerInfo playerInfo, Vector2 direction) : base(movementParameters, playerInfo)
    {
        this.direction = direction;
        onFirstUpdate = (ref KinematicState<Vector2> kinematics) =>
        {
            kinematics.velocity = parameters.ImpactSpeed * direction;
            onFirstUpdate = null;
        };
    }

    public override MovementState ProcessInterrupts(ref KinematicState<Vector2> kinematics, IEnumerable<IInterrupt> interrupts)
    {
        if (interrupts.FirstOrDefault(i => i is JumpInterrupt) is JumpInterrupt { type: JumpInterrupt.Type.Started })
                return GetJumpState(kinematics);
        
        return base.ProcessInterrupts(ref kinematics, interrupts);
    }

    public override MovementState UpdateKinematics(ref float t, ref KinematicState<Vector2> kinematics, out KinematicSegment<Vector2>[] motion)
    {
        onFirstUpdate?.Invoke(ref kinematics);

        // TODO This isn't great, processing interrupts shouldn't be done here
        if (player.JumpBuffer.Flush())
        {
            motion = default;
            return GetJumpState(kinematics);
        }

        motion = ImpactCurve(ref t, ref kinematics);
        if (t > 0f)
        {
            kinematics.velocity = Vector2.zero;
            return new FallingState(parameters, player, parameters.FallGravity);
        }

        return this;
    }

    private MovementState GetJumpState(KinematicState<Vector2> kinematics)
    {
        return player.GroundCheck() ? 
            new GrappleJumpState(parameters, player, kinematics) :
            new GrappleWallJumpState(parameters, player, kinematics, boost);
    }

    private KinematicSegment<Vector2>[] ImpactCurve(ref float t, ref KinematicState<Vector2> kinematics)
    {
        Vector2 acceleration = parameters.ImpactAcceleration * direction;
        Vector2 targetVelocity = parameters.TopSpeed * direction;

        KinematicState<float> xKinematics = new(kinematics.position.x, kinematics.velocity.x);
        KinematicState<float> yKinematics = new(kinematics.position.y, kinematics.velocity.y);

        float t1 = t;
        KinematicSegment<float> xMotion = AccelerateTowardTargetVelocity(ref t1, targetVelocity.x, Mathf.Abs(acceleration.x), ref xKinematics);
        KinematicSegment<float> yMotion = AccelerateTowardTargetVelocity(ref t, targetVelocity.y, Mathf.Abs(acceleration.y), ref yKinematics);
        kinematics = new KinematicState<Vector2>(new(xKinematics.position, yKinematics.position), new(xKinematics.velocity, yKinematics.velocity));
        
        t = Mathf.Min(t1, t);
        return MergeKinematicSegments(new[] { xMotion }, new[] { yMotion });
    }
}
