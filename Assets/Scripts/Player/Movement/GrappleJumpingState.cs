using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

public class GrappleJumpingState : MovementState
{
    private class GrappleJumpMovementParameters : MovementParameters
    {
        private float currentTopSpeed;
        private readonly Func<float> getAcceleration;
        private readonly Func<float> getDeceleration;

        public GrappleJumpMovementParameters(MovementParameters baseParameters, float newTopSpeed)
        {
            currentTopSpeed = Mathf.Abs(newTopSpeed);

            CopyDataFromBaseParameters(baseParameters);
            float velocityScalingFactor = currentTopSpeed / baseParameters.TopSpeed;
            getAcceleration = () => GetAcceleration(ImpactSpeed, velocityScalingFactor * AccelerationDistance);
            getDeceleration = () => GetAcceleration(ImpactSpeed, velocityScalingFactor * DecelerationDistance);
        }
        
        public override float TopSpeed => currentTopSpeed;
        public override float CancelledJumpRise => cancelledGrappleJumpRise;

        protected override float MaxHorizontalJumpSpeed => ImpactSpeed;

        public override float Acceleration => getAcceleration();
        public override float Deceleration => getDeceleration();
        public override float MaxJumpHeight => grappleJumpMaxHeight;
        public override float MaxJumpDistance => grappleJumpMaxDistance;
        
        public void SetTopSpeed(float newTopSpeed) => currentTopSpeed = newTopSpeed;
        
        private void CopyDataFromBaseParameters(MovementParameters baseParameters)
        {
            IEnumerable<PropertyInfo> properties = typeof(MovementParameters)
                .GetProperties(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
                .Where(p => p.CanRead && p.CanWrite);
            foreach (PropertyInfo property in properties)
            {
                property.SetValue(this, property.GetValue(baseParameters));
            }

            IEnumerable<FieldInfo> fields = typeof(MovementParameters).GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            foreach (FieldInfo field in fields)
            {
                field.SetValue(this, field.GetValue(baseParameters));
            }
        }
    }

    private MovementParameters initialParameters;
    private GrappleJumpMovementParameters newParameters;
    private MovementState innerState;
    private float initialVelocity;

    public GrappleJumpingState(MovementParameters movementParameters, IPlayerInfo playerInfo, KinematicState<Vector2> initialKinematics) : base(movementParameters, playerInfo)
    {
        initialParameters = parameters;
        initialVelocity = initialKinematics.velocity.x;
        newParameters = new GrappleJumpMovementParameters(initialParameters, Mathf.Abs(initialVelocity));
        innerState = new JumpingState(newParameters, playerInfo, initialKinematics);
    }
    
    public override MovementState ProcessInterrupts(ref KinematicState<Vector2> kinematics, IEnumerable<IInterrupt> interrupts)
    {
        innerState = innerState.ProcessInterrupts(ref kinematics, interrupts);
        if (innerState is not JumpingState && innerState is not FallingState)
        {
            innerState.parameters = initialParameters;
            return innerState;
        }
        
        return this;
    }

    public override MovementState UpdateKinematics(ref float t, ref KinematicState<Vector2> kinematics, out KinematicSegment<Vector2>[] motion)
    {
        newParameters.SetTopSpeed(GetTopSpeed(kinematics.velocity.x));
        
        innerState = innerState.UpdateKinematics(ref t, ref kinematics, out motion);
        if (innerState is not JumpingState && innerState is not FallingState)
        {
            innerState.parameters = initialParameters;
            return innerState;
        }
        
        return this;
    }

    private float GetTopSpeed(float currentVelocity)
    {
        currentVelocity /= player.Aim.x != 0f ? Mathf.Abs(player.Aim.x) : 1f; // this is a little tricky, but it allows the player to move at top speed without aiming perfectly horizontal
        if (Mathf.Abs(currentVelocity) > initialParameters.TopSpeed && currentVelocity * initialVelocity > 0f)
            return Mathf.Abs(currentVelocity);

        return initialParameters.TopSpeed;
    }
}
