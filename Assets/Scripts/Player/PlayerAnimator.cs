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
    [SerializeField] private UnityEvent onGrounded;
    [SerializeField] private UnityEvent onJump;

    private void Update()
    {
        animator.SetFloat(verticalVelocityParameter, playerController.Velocity.y);
        
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
        
        horizontalVelocity.AddSample(playerController.Velocity.x, Time.deltaTime);
        if (Mathf.Abs(horizontalVelocity) < 0.01f)
            return;
        
        animator.SetFloat(horizontalVelocityParameter, horizontalVelocity);
    }
}
