using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.Events;

public class PlayerAnimator : MonoBehaviour
{
    [SerializeField] private PlayerController playerController;
    [SerializeField] private Animator animator;
    [SerializeField] private ExpMovingAverageFloat horizontalVelocity;
    [SerializeField] private ParameterSelector<AnimatorController> verticalVelocityParameter;
    [SerializeField] private ParameterSelector<AnimatorController> horizontalVelocityParameter;
    [SerializeField] private ParameterSelector<AnimatorController> groundedParameter;
    [SerializeField] private ParameterSelector<AnimatorController> wallParameter;
    [SerializeField] private ParameterSelector<AnimatorController> hangingParameter;
    [SerializeField] private ParameterSelector<AnimatorController> swingingParameter;
    [SerializeField] private ParameterSelector<AnimatorController> angularVelocityParameter;
    [SerializeField] private ParameterSelector<AnimatorController> grapplingParameter;
    [SerializeField] private ParameterSelector<AnimatorController> stationaryParameter;
    
    [SerializeField] private UnityEvent onGrounded;
    [SerializeField] private UnityEvent onJump;

    private void Update()
    {
        animator.SetFloat(verticalVelocityParameter, playerController.Velocity.y);
        animator.SetInteger(wallParameter, playerController.WallCheck());
        animator.SetBool(grapplingParameter, playerController.State is GrappleState);
        animator.SetBool(stationaryParameter, Mathf.Abs(playerController.Velocity.x) < 0.01f);

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
        if (!grounded && animator.GetBool(groundedParameter) && playerController.Velocity.y > 0f)
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
            animator.SetBool(hangingParameter, false);
            return;
        }
        animator.SetBool(hangingParameter, true);
        
        Vector2 anchor = swingingState.Anchor;
        Vector2 ropeDirection = (playerController.Position - anchor).normalized;
        Vector2 velocityDirection = new Vector2(-ropeDirection.y, ropeDirection.x);
        Vector2 tangentialVelocity = Vector3.Project(playerController.Velocity, velocityDirection);
        
        float angularVelocity = Mathf.Rad2Deg * Vector2.Dot(tangentialVelocity, velocityDirection) / Vector2.Distance(anchor, playerController.Position);

        animator.SetBool(swingingParameter, playerController.Aim.x * angularVelocity > 0f);
        animator.SetFloat(angularVelocityParameter, angularVelocity);
    }
}
