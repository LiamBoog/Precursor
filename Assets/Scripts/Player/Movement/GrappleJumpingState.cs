using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

public class GrappleJumpingState : MovementState
{
    private class ModifiedMovementParameters : MovementParameters
    {
        private readonly float topSpeed;

        public ModifiedMovementParameters(MovementParameters initialParameters, float topSpeed)
        {
            this.topSpeed = topSpeed;

            foreach (PropertyInfo property in typeof(MovementParameters).GetProperties(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public).Where(p => p.CanRead && p.CanWrite))
            {
                property.SetValue(this, property.GetValue(initialParameters));
            }

            foreach (FieldInfo field in typeof(MovementParameters).GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public))
            {
                field.SetValue(this, field.GetValue(initialParameters));
            }
        }
        
        public override float TopSpeed => topSpeed;

        public override float MaxJumpHeight => grappleJumpMaxHeight;
        public override float MaxJumpDistance => grappleJumpMaxDistance;
    }

    private MovementParameters initialParameters;
    private MovementState innerState;

    public GrappleJumpingState(MovementParameters movementParameters, IPlayerInfo playerInfo, KinematicState<Vector2> initialKinematics) : base(movementParameters, playerInfo)
    {
        initialParameters = parameters;
        innerState = new JumpingState(new ModifiedMovementParameters(initialParameters, Mathf.Abs(initialKinematics.velocity.x)), playerInfo, initialKinematics);
    }
    
    public override MovementState ProcessInterrupts(ref KinematicState<Vector2> kinematics, IEnumerable<IInterrupt> interrupts)
    {
        innerState = innerState.ProcessInterrupts(ref kinematics, interrupts);
        if (innerState is not JumpingState && !(innerState is FallingState fallingState && fallingState.Gravity != initialParameters.FallGravity) )
        {
            innerState.parameters = initialParameters;
            return innerState;
        }
        
        return this;
    }

    public override MovementState UpdateKinematics(ref float t, ref KinematicState<Vector2> kinematics, out KinematicSegment<Vector2>[] motion)
    {
        innerState = innerState.UpdateKinematics(ref t, ref kinematics, out motion);
        if (innerState is not JumpingState && !(innerState is FallingState fallingState && fallingState.Gravity != initialParameters.FallGravity) )
        {
            innerState.parameters = initialParameters;
            return innerState;
        }
        
        return this;
    }
}
