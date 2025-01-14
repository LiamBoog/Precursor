using UnityEngine;

public class PlayerAnimator : MonoBehaviour
{
    [SerializeField] private PlayerController playerController;
    [SerializeField] private Animator animator;
    [SerializeField] private ExpMovingAverageFloat horizontalVelocity;
    
    private AnimationState currentState;

    private void Update()
    {
        animator.SetFloat("VerticalVelocity", playerController.Velocity.y);
        animator.SetBool("Grounded", playerController.GroundCheck());
        
        horizontalVelocity.AddSample(playerController.Velocity.x, Time.deltaTime);
        if (Mathf.Abs(horizontalVelocity) < 0.01f)
            return;
        
        animator.SetFloat("HorizontalVelocity", horizontalVelocity);
    }
}
