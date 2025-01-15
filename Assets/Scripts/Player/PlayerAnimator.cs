using UnityEngine;

public class PlayerAnimator : MonoBehaviour
{
    [SerializeField] private PlayerController playerController;
    [SerializeField] private Animator animator;
    [SerializeField] private ExpMovingAverageFloat horizontalVelocity;
    [SerializeField] private AnimatorControllerParameterHash verticalVelocityParameter;
    [SerializeField] private AnimatorControllerParameterHash horizontalVelocityParameter;
    [SerializeField] private AnimatorControllerParameterHash groundedParameter;
    
    private void Update()
    {
        animator.SetFloat(verticalVelocityParameter, playerController.Velocity.y);
        animator.SetBool(groundedParameter, playerController.GroundCheck());
        
        horizontalVelocity.AddSample(playerController.Velocity.x, Time.deltaTime);
        if (Mathf.Abs(horizontalVelocity) < 0.01f)
            return;
        
        animator.SetFloat(horizontalVelocityParameter, horizontalVelocity);
    }
}
