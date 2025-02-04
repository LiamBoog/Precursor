using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

public class PlayerAnimator : MonoBehaviour
{
    [SerializeField] private PlayerController playerController;
    [SerializeField] private Animator animator;
    [SerializeField] private ExpMovingAverageFloat horizontalVelocity;
    [SerializeField] private ParameterSelector<AnimatorController> verticalVelocityParameter;
    [SerializeField] private ParameterSelector<AnimatorController> horizontalVelocityParameter;
    [SerializeField] private ParameterSelector<AnimatorController> groundedParameter;
    [SerializeField] private ParameterSelector<AnimatorController> wallParameter;
    [SerializeField] private ParameterSelector<AnimatorController> swingingParameter;
    [SerializeField] private ParameterSelector<AnimatorController> angleParameter;
    [SerializeField] private ParameterSelector<AnimatorController> angularVelocityParameter;
    
    [SerializeField] private UnityEvent onGrounded;
    [SerializeField] private UnityEvent onJump;

    private void Update()
    {
        animator.SetFloat(verticalVelocityParameter, playerController.Velocity.y);
        animator.SetInteger(wallParameter, playerController.WallCheck());
        
        SetGroundedProperty();
        SetHorizontalVelocityProperty();
        SetSwingingProperties();
    }

    private void SetGroundedProperty()
    {
        bool grounded = playerController.GroundCheck();
        if (grounded && !animator.GetBool(groundedParameter))
        {
            onGrounded.Invoke(); // TODO - Should make a nicer way to do this
        }
        if (!grounded && animator.GetBool(groundedParameter))
        {
            onJump.Invoke();
        }
        
        animator.SetBool(groundedParameter, grounded);
    }

    private void SetHorizontalVelocityProperty()
    {
        horizontalVelocity.AddSample(playerController.Velocity.x, Time.deltaTime);
        if (Mathf.Abs(horizontalVelocity) < 0.01f)
            return;
        
        animator.SetFloat(horizontalVelocityParameter, horizontalVelocity);
    }
    
    private void SetSwingingProperties()
    {
        if (playerController.State is not SwingingState swingingState)
        {
            animator.SetBool(swingingParameter, false);
            return;
        }
        animator.SetBool(swingingParameter, true);
        
        Vector2 anchor = swingingState.Anchor;
        Vector2 ropeDirection = (playerController.Position - anchor).normalized;
        Vector2 velocityDirection = new Vector2(-ropeDirection.y, ropeDirection.x);
        Vector2 tangentialVelocity = Vector3.Project(playerController.Velocity, velocityDirection);
        
        float angle = Vector2.SignedAngle(Vector2.down, ropeDirection);
        float angularVelocity = Mathf.Rad2Deg * Vector2.Dot(tangentialVelocity, velocityDirection) / Vector2.Distance(anchor, playerController.Position);

        animator.SetFloat(angleParameter, angle);
        animator.SetFloat(angularVelocityParameter, angularVelocity);
    }
}
