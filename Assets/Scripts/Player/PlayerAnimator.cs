using UnityEngine;

public class PlayerAnimator : MonoBehaviour
{
    [SerializeField] private PlayerController playerController;
    [SerializeField] private Animator animator;
    [SerializeField] private ExpMovingAverageFloat horizontalVelocity;
    [SerializeField] private ExpMovingAverageFloat verticalVelocity;
    
    private AnimationState currentState;

    private void Update()
    {
        verticalVelocity.AddSample(playerController.Velocity.y, Time.deltaTime);
        animator.SetFloat("VerticalVelocity", verticalVelocity);
        
        animator.SetBool("Grounded", playerController.GroundCheck());
        
        horizontalVelocity.AddSample(playerController.Velocity.x, Time.deltaTime);
        if (Mathf.Abs(horizontalVelocity) < 0.01f)
            return;
        
        animator.SetFloat("HorizontalVelocity", horizontalVelocity);
    }
}
