using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class WallJumpState : JumpingState
{
    private float elapsedTime;
    private float controlTime;
    private JumpInitializer onFirstUpdate;

    public WallJumpState(MovementParameters movementParameters, IPlayerInfo playerInfo, int direction, KinematicState<Vector2> initialKinematics) : base(movementParameters, playerInfo, initialKinematics)
    {
        elapsedTime = 0f;
        controlTime = GetWallJumpControlTime();
        onFirstUpdate = (ref KinematicState<Vector2> kinematics) =>
        {
            onFirstUpdate = null;
            kinematics.velocity.x = direction * parameters.CurrentTopSpeed;
        };
        player.JumpBuffer.Flush();
    }
    
    public override MovementState UpdateKinematics(ref float t, ref KinematicState<Vector2> kinematics, out KinematicSegment<Vector2>[] motion)
    {
        onFirstUpdate?.Invoke(ref kinematics);
        
        KinematicState<float> xKinematics = new(kinematics.position.x, kinematics.velocity.x);
        KinematicSegment<float>[] xMotion = HorizontalMotion(t, ref xKinematics);

        MovementState output = base.UpdateKinematics(ref t, ref kinematics, out motion);
        KinematicState<float> yKinematics = new(kinematics.position.y, kinematics.velocity.y);

        kinematics = new(
            new(xKinematics.position, yKinematics.position),
            new(xKinematics.velocity, yKinematics.velocity)
        );

        KinematicSegment<float>[] yMotion = motion
            .Select(segment =>
                new KinematicSegment<float>(
                    new(segment.initialState.position.y, segment.initialState.velocity.y),
                    segment.acceleration.y,
                    segment.duration
                ))
            .ToArray();
        motion = MergeKinematicSegments(xMotion, yMotion);

        return output;
    }
    
    private KinematicSegment<float>[] HorizontalMotion(float t, ref KinematicState<float> kinematics)
    {
        List<KinematicSegment<float>> output = new();
        if (elapsedTime < controlTime)
        {
            float maxMovementTime = controlTime - elapsedTime;
            float movementTime = Mathf.Min(t, maxMovementTime);
            elapsedTime += movementTime;
        
            t -= movementTime;
            output.Add(LinearMotionCurve(movementTime, ref kinematics));
        }
        
        output.AddRange(WalkingCurve(t, ref kinematics));
        return output.ToArray();
    }
    
    private float GetWallJumpControlTime()
    {
        float controlTime = 0.5f * (parameters.RiseDistance + parameters.FallDistance - 3f * parameters.DecelerationDistance - parameters.AccelerationDistance) / parameters.CurrentTopSpeed;
        
        if (parameters.ClimbHeight < 0f)
            return controlTime - 0.5f * parameters.ClimbHeight / parameters.TerminalVelocity;
        
        float terminalVelocity = parameters.TerminalVelocity;
        float fallTime = parameters.FallDistance / parameters.CurrentTopSpeed;
        float climbOffset = (-terminalVelocity + Mathf.Sqrt(terminalVelocity * (terminalVelocity - 2f * parameters.ClimbHeight / fallTime))) / -parameters.FallGravity;
        
        return controlTime - 0.5f * climbOffset;
    }
}
