using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

public class GrappleJumpingState : MovementState
{
    private class GrappleJumpMovementParameters : MovementParameters
    {
        private float previousTopSpeed;
        private float initialTopSpeed;
        private float currentTopSpeed;

        public GrappleJumpMovementParameters(MovementParameters initialParameters, float topSpeed)
        {
            previousTopSpeed = initialParameters.CurrentTopSpeed;
            currentTopSpeed = Mathf.Abs(topSpeed);
            initialTopSpeed = currentTopSpeed;
            
            foreach (PropertyInfo property in typeof(MovementParameters).GetProperties(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public).Where(p => p.CanRead && p.CanWrite))
            {
                property.SetValue(this, property.GetValue(initialParameters));
            }

            foreach (FieldInfo field in typeof(MovementParameters).GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public))
            {
                field.SetValue(this, field.GetValue(initialParameters));
            }
        }
        
        public override float CurrentTopSpeed => currentTopSpeed;

        protected override float MaxHorizontalJumpSpeed => GrappleSpeed;

        public override float Acceleration => GetAcceleration(GrappleSpeed, AccelerationDistance / previousTopSpeed * initialTopSpeed);
        public override float Deceleration => GetAcceleration(GrappleSpeed, DecelerationDistance / previousTopSpeed * initialTopSpeed);
        public override float MaxJumpHeight => grappleJumpMaxHeight;
        public override float MaxJumpDistance => grappleJumpMaxDistance;
        
        public void SetTopSpeed(float newTopSpeed) => currentTopSpeed = newTopSpeed;
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
        if (innerState is not JumpingState && !(innerState is FallingState fallingState && fallingState.Gravity != initialParameters.FallGravity))
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
        if (innerState is not JumpingState && !(innerState is FallingState fallingState && fallingState.Gravity != initialParameters.FallGravity))
        {
            innerState.parameters = initialParameters;
            return innerState;
        }
        
        return this;
    }

    private float GetTopSpeed(float currentVelocity)
    {
        currentVelocity /= player.Aim.x != 0f ? Mathf.Abs(player.Aim.x) : 1f;
        if (Mathf.Abs(currentVelocity) > initialParameters.CurrentTopSpeed && currentVelocity * initialVelocity > 0f)
        {
            return Mathf.Abs(currentVelocity);
        }

        return initialParameters.CurrentTopSpeed;
    }
}
