using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

[Serializable]
public class MovementParameters
{
    public const float REFERENCE_FRAMERATE = 60f;

    [field: FormerlySerializedAs("topSpeed")]
    [field: Header("Movement Parameters")]
    [field: SerializeField] public float TopSpeed { get; private set; } = 10f;
    [SerializeField] private float accelerationDistance = 0.5f;
    [SerializeField] private float decelerationDistance = 0.5f;

    [field: Header("Jump Parameters")] 
    [field: SerializeField] public float MaxJumpHeight {get; private set; } = 4f;
    [field: SerializeField] public float MinJumpHeight {get; private set; } = 2f;
    [SerializeField] private float riseDistance = 3.5f;
    [SerializeField] private float fallDistance = 2.5f;
    [field: SerializeField] public float CancelledJumpRise { get; private set; } = 0.75f;
    [SerializeField] private int coyoteFrames = 4;
    [SerializeField] private int jumpBufferFrames = 3;

    [Header("Wall Jump Parameters")] 
    [SerializeField] private float climbHeight = 0f;
    [field: SerializeField] public int GracePixels { get; private set; } = 2;
    [SerializeField] private float wallSlideVelocityFactor = 0.15f;

    [Header("Rope Dart Parameters")] 
    [SerializeField] private float ropeLength = 7.5f;
    [SerializeField] private float angleSnapIncrement = 22.5f;

    public float Acceleration => 0.5f * TopSpeed * TopSpeed / accelerationDistance;
    public float Deceleration => 0.5f * TopSpeed * TopSpeed / decelerationDistance;
    public float RiseGravity => 2f * MaxJumpHeight * TopSpeed * TopSpeed / (riseDistance * riseDistance);
    public float FallGravity => 2f * MaxJumpHeight * TopSpeed * TopSpeed / (fallDistance * fallDistance);
    public float JumpVelocity => 2f * MaxJumpHeight * TopSpeed / riseDistance;
    public float TerminalVelocity => 2f * MaxJumpHeight * TopSpeed / fallDistance;
    public float JumpBufferDuration => jumpBufferFrames / REFERENCE_FRAMERATE;
    public float CoyoteTime => coyoteFrames / REFERENCE_FRAMERATE;

    public float WallSlideVelocity => wallSlideVelocityFactor * TerminalVelocity;
}

public interface IPlayerInfo
{
    Vector2 Aim { get; }
    float JumpBuffer { get; }

    bool GroundCheck();
    int WallCheck();
}

public struct JumpInterrupt : IInterrupt
{
    public enum Type
    {
        Started,
        Cancelled
    }

    public Type type;
}

public class PlayerController : MonoBehaviour, IPlayerInfo
{
    [SerializeField] private MovementParameters movementParameters;
    [SerializeField] private new Camera camera;
    
    [Header("Input Actions")]
    [SerializeField] private InputActionReference aim;
    [SerializeField] private InputActionReference jump;
    [SerializeField] private InputActionReference grapple;
    [SerializeField] private InputActionReference anchor;
    
    [SerializeField] private CollisionResolver collisionResolver;

    private MovementStateMachine movementController;
    private Vector2 velocity;
    private List<IInterrupt> interrupts = new();

    public Vector2 Aim => aim.action.ReadValue<Vector2>();
    private float WallCheckOverlapDistance
    {
        get
        {
            float pixelWidth = 2f * camera.orthographicSize / Screen.height;
            return movementParameters.GracePixels * pixelWidth;
        }
    }
    public float JumpBuffer { get; private set; }

    private void OnEnable()
    {
        movementController = new(movementParameters, this);
        
        aim.action.Enable();
        jump.action.Enable();
        grapple.action.Enable();
        anchor.action.Enable();

        jump.action.performed += OnJump;
        jump.action.canceled += OnJumpCancelled;
    }

    private void OnDisable()
    {
        aim.action.Disable();
        jump.action.Disable();
        grapple.action.Disable();
        anchor.action.Disable();
        
        jump.action.performed -= OnJump;
        jump.action.canceled -= OnJumpCancelled;
    }

    private void Update()
    {
        // Compute new kinematics
        KinematicState<Vector2> kinematics = new (transform.position, velocity);
        movementController.Update(Time.deltaTime, ref kinematics, interrupts);
        
        // Check for collisions
        Vector2 displacement = kinematics.position - (Vector2) transform.position;
        if (collisionResolver.Collide(displacement, out ICollision collision) && collision.Normal != default)
        {
            displacement += collision.Deflection;
            interrupts.Add(collision);
            movementController.Update(0f, ref kinematics, interrupts);
        }
        
        // Apply motion
        transform.position += (Vector3) displacement;
        velocity = kinematics.velocity;
        Physics2D.SyncTransforms();
    }

    public bool GroundCheck()
    {
        return collisionResolver.Touching(Vector2.down);
    }

    public int WallCheck()
    {
        float overlapDistance = WallCheckOverlapDistance;
        int normal = collisionResolver.Touching(Vector2.left, overlapDistance) ? 1 :
            collisionResolver.Touching(Vector2.right, overlapDistance) ? -1 : 0;

        return normal;
    }

    private void OnJump(InputAction.CallbackContext _)
    {
        interrupts.Add(new JumpInterrupt
        {
            type = JumpInterrupt.Type.Started
        });
    }

    private void OnJumpCancelled(InputAction.CallbackContext _)
    {
        interrupts.Add(new JumpInterrupt
        {
            type = JumpInterrupt.Type.Cancelled
        });
    }
}