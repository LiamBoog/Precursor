using System;
using System.Collections.Generic;
using UnityEngine;

public class WalkingState : IMovementState
{
    private PlayerController.MovementParameters parameters;
    
    public WalkingState(PlayerController.MovementParameters movementParameters)
    {
        parameters = movementParameters;
    }
    
    public IMovementState Update(float t, ref KinematicState<Vector2> kinematics)
    {
        KinematicState<float> xKinematics = new(kinematics.position.x, kinematics.velocity.x);
        WalkingCurve(t, ref xKinematics, parameters.Aim.x);
        kinematics = new(
            new(xKinematics.position, kinematics.position.y), 
            new(xKinematics.velocity, kinematics.velocity.y));
        return this;
    }

    public KinematicSegment<float>[] WalkingCurve(float t, ref KinematicState<float> kinematics, float input)
    {
        List<KinematicSegment<float>> output = new();
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
    
    public KinematicSegment<float> AccelerateTowardTargetVelocity(ref float t, float targetVelocity, float accelerationMagnitude, ref KinematicState<float> kinematics)
    {
        float acceleration = Math.Sign(targetVelocity - kinematics.velocity) * accelerationMagnitude;
        float maxAccelerationTime = acceleration == 0f ? 0f : (targetVelocity - kinematics.velocity) / acceleration;
        float accelerationTime = Mathf.Min(t, maxAccelerationTime);

        KinematicSegment<float> output = new(kinematics, acceleration, accelerationTime);
        kinematics.position += kinematics.velocity * accelerationTime + 0.5f * acceleration * accelerationTime * accelerationTime;
        kinematics.velocity = accelerationTime < maxAccelerationTime ? kinematics.velocity + acceleration * accelerationTime : targetVelocity;
        t -= accelerationTime;
        
        return output;
    }

    public KinematicSegment<float> LinearMotionCurve(float t, ref KinematicState<float> kinematics)
    {
        KinematicSegment<float> output = new(kinematics, 0f, t);
        kinematics.position += kinematics.velocity * t;
        return output;
    }
    
    
}
