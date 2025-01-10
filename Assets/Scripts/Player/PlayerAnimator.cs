using System;
using UnityEngine;



public class PlayerAnimator : MonoBehaviour
{
    public enum AnimationState
    {
        Idle,
        Walk,
        Run,
        Jump,
        Fall,
        WallSlide,
        Swing,
        Grapple,
        WallJump,
        Impact
    }
    
    public interface IAnimationNotifier
    {
        public delegate void AnimationStateChangeHandler(AnimationState newState);
        
        event AnimationStateChangeHandler StateChanged;
        
        public void NotifyStateChange(AnimationState newState);
    }
    
    [SerializeField] private PlayerController playerController;
    [SerializeField] private Animator animator;
    
    private AnimationState currentState;

    private void OnEnable()
    {
        playerController.StateChanged += OnStateChange;
    }
    
    private void OnDisable()
    {
        playerController.StateChanged -= OnStateChange;
    }

    private void Update()
    {
        transform.localScale = Mathf.Abs(playerController.Velocity.x) < 0.1f ? transform.localScale : new Vector3(Math.Sign(playerController.Velocity.x), 1f, 1f);
    }

    private void OnStateChange(AnimationState newState)
    {
        switch (newState)
        {
            case AnimationState.Idle:
                if (currentState == AnimationState.Idle)
                    return;
                ResetAnimatorParameters(AnimatorControllerParameterType.Bool);
                animator.SetBool("Idle", true);
                break;
            case AnimationState.Walk:
                if (currentState == AnimationState.Walk)
                    return;
                ResetAnimatorParameters(AnimatorControllerParameterType.Bool);
                animator.SetBool("Walking", true);
                break;
            case AnimationState.Run:
                break;
            case AnimationState.Jump:
                break;
            case AnimationState.Fall:
                break;
            case AnimationState.WallSlide:
                break;
            case AnimationState.Swing:
                break;
            case AnimationState.Grapple:
                break;
            case AnimationState.WallJump:
                break;
            case AnimationState.Impact:
                break;
        }

        currentState = newState;
    }

    private void ResetAnimatorParameters(AnimatorControllerParameterType type)
    {
        foreach (AnimatorControllerParameter parameter in animator.parameters)
        {
            if (parameter.type != type)
                continue;
                    
            animator.SetBool(parameter.name, false);
        }
    }
}
