using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IMovementState
{
    public struct KinematicState
    {
        public float position;
        public float velocity;

        public KinematicState(float position, float velocity)
        {
            this.position = position;
            this.velocity = velocity;
        }
    }
    
    public readonly struct KinematicSegment
    {
        public readonly KinematicState initialState;
        public readonly float acceleration;
        public readonly float duration;

        public KinematicSegment(KinematicState initialState, float acceleration, float duration)
        {
            this.initialState = initialState;
            this.acceleration = acceleration;
            this.duration = duration;
        }
    }

    IMovementState Update(float t, out IMovementState.KinematicSegment[] kinematicSegments, ref IMovementState.KinematicState kinematics);
}
