using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class GrappleState : MovementState
{
    private Vector2 anchor;
    
    public GrappleState(MovementParameters movementParameters, IPlayerInfo playerInfo, Vector2 anchor) : base(movementParameters, playerInfo)
    {
        this.anchor = anchor;
    }

    public override MovementState ProcessInterrupts(ref KinematicState<Vector2> kinematics, IEnumerable<IInterrupt> interrupts)
    {
        if (interrupts.FirstOrDefault(i => i is ICollision) is ICollision collision)
        {
            if (collision.Normal != Vector2.down)
            {
                if (player.JumpBuffer.Flush())
                    return player.GroundCheck() ? new JumpingState(parameters, player, kinematics) : new WallJumpState(parameters, player, player.WallCheck(), kinematics);

                Vector2 incomingDirection = (anchor - kinematics.position).normalized;
                Vector2 slideDirection = Vector3.Cross(collision.Normal, Vector3.Cross(incomingDirection, collision.Normal)).normalized;
                return new ImpactState(parameters, player, slideDirection, incomingDirection);
            }
        }
        
        if (base.ProcessInterrupts(ref kinematics, interrupts) is { } newState && newState != this)
            return newState;
        
        if (interrupts.Any(i => i is ICollision))
            return new FallingState(parameters, player, parameters.FallGravity);
        
        return this;
    }

    public override MovementState UpdateKinematics(ref float t, ref KinematicState<Vector2> kinematics, out KinematicSegment<Vector2>[] motion)
    {
        Vector2 direction = anchor - kinematics.position;
        float distance = direction.magnitude;

        kinematics.velocity = parameters.GrappleSpeed / distance * direction;
        float moveTime = Mathf.Min(t, distance / parameters.GrappleSpeed);

        motion = ApplyMotionCurves(
            moveTime,
            ref kinematics,
            (float t, ref KinematicState<float> kinematics) => new[] { LinearMotionCurve(t, ref kinematics) },
            (float t, ref KinematicState<float> kinematics) => new[] { LinearMotionCurve(t, ref kinematics) }
        );
        t -= moveTime;

        if (t > 0f)
            return new FallingState(parameters, player, parameters.FallGravity);

        return this;
    }
}
