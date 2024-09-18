using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class WalkingState : IMovementState
{
    private PlayerController.MovementParameters parameters;
    
    public WalkingState(PlayerController.MovementParameters movementParameters)
    {
        parameters = movementParameters;
    }
    
    public IMovementState Update(float t, out IMovementState.KinematicSegment[] kinematicSegments, ref IMovementState.KinematicState kinematics)
    {
        kinematicSegments = WalkingCurve(t, ref kinematics, parameters.Aim.x);
        return this;
    }

    public IMovementState.KinematicSegment[] WalkingCurve(float t, ref IMovementState.KinematicState kinematics, float input)
    {
        List<IMovementState.KinematicSegment> output = new();

        float targetVelocity = parameters.TopSpeed * input;

        float decelerationTarget = kinematics.velocity * targetVelocity < 0f ? 0f : targetVelocity;
        if (Mathf.Abs(decelerationTarget) < Mathf.Abs(kinematics.velocity))
        {
            output.Add(AccelerateTowardTargetVelocity(ref t, decelerationTarget, parameters.Deceleration, ref kinematics));
        }

        output.Add(AccelerateTowardTargetVelocity(ref t, targetVelocity, parameters.Acceleration, ref kinematics));
        output.Add(LinearMotionCurve(t, ref kinematics));
        return output.ToArray();
    }
    
    public IMovementState.KinematicSegment AccelerateTowardTargetVelocity(ref float t, float targetVelocity, float accelerationMagnitude, ref IMovementState.KinematicState kinematics)
    {
        float acceleration = Math.Sign(targetVelocity - kinematics.velocity) * accelerationMagnitude;
        float maxAccelerationTime = acceleration == 0f ? 0f : (targetVelocity - kinematics.velocity) / acceleration;
        float accelerationTime = Mathf.Min(t, maxAccelerationTime);

        IMovementState.KinematicSegment output = new IMovementState.KinematicSegment(kinematics, acceleration, accelerationTime);
        kinematics.position += kinematics.velocity * accelerationTime + 0.5f * acceleration * accelerationTime * accelerationTime;
        kinematics.velocity = accelerationTime < maxAccelerationTime ? kinematics.velocity + acceleration * accelerationTime : targetVelocity;
        t -= accelerationTime;
        
        return output;
    }

    public IMovementState.KinematicSegment LinearMotionCurve(float t, ref IMovementState.KinematicState kinematics)
    {
        IMovementState.KinematicSegment output = new IMovementState.KinematicSegment(kinematics, 0f, t);
        kinematics.position += kinematics.velocity * t;
        return output;
    }
}
