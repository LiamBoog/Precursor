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
    [field: SerializeField] public float AccelerationDistance { get; private set; } = 0.5f;
    [field: SerializeField] public float DecelerationDistance { get; private set; } = 0.5f;

    [field: Header("Jump Parameters")] 
    [field: SerializeField] public float MaxJumpHeight {get; private set; } = 4f;
    [field: SerializeField] public float MinJumpHeight {get; private set; } = 2f;
    [field: SerializeField] public float RiseDistance {get; private set; } = 3.5f;
    [field: SerializeField] public float FallDistance {get; private set; } = 2.5f;
    [field: SerializeField] public float CancelledJumpRise { get; private set; } = 0.75f;
    [SerializeField] private int coyoteFrames = 4;
    [SerializeField] private int jumpBufferFrames = 3;

    [field: FormerlySerializedAs("climbHeight")]
    [field: Header("Wall Jump Parameters")] 
    [field: SerializeField] public float ClimbHeight { get; private set; } = 0f;
    [field: SerializeField] public int GracePixels { get; private set; } = 2;
    [SerializeField] private float wallSlideVelocityFactor = 0.15f;

    [field: Header("Rope Dart Parameters")] 
    [field: SerializeField] public float RopeLength { get; private set; } = 7.5f;

    [field: SerializeField] public float AngleSnapIncrement { get; private set; } = 22.5f;

    public float Acceleration => 0.5f * TopSpeed * TopSpeed / AccelerationDistance;
    public float Deceleration => 0.5f * TopSpeed * TopSpeed / DecelerationDistance;
    public float RiseGravity => 2f * MaxJumpHeight * TopSpeed * TopSpeed / (RiseDistance * RiseDistance);
    public float FallGravity => 2f * MaxJumpHeight * TopSpeed * TopSpeed / (FallDistance * FallDistance);
    public float JumpVelocity => 2f * MaxJumpHeight * TopSpeed / RiseDistance;
    public float TerminalVelocity => 2f * MaxJumpHeight * TopSpeed / FallDistance;
    public float JumpBufferDuration => jumpBufferFrames / REFERENCE_FRAMERATE;
    public float CoyoteTime => coyoteFrames / REFERENCE_FRAMERATE;

    public float WallSlideVelocity => wallSlideVelocityFactor * TerminalVelocity;
}

public interface IPlayerInfo
{
    Vector2 Aim { get; }
    IInputBuffer JumpBuffer { get; }

    bool GroundCheck();
    int WallCheck();
    bool GrappleRaycast(out Vector2 anchor);
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

public interface IInputBuffer
{
    bool Flush();
}

public class PlayerController : MonoBehaviour, IPlayerInfo, ICameraTarget
{
    private struct InputBuffer : IInputBuffer
    {
        private readonly Func<float> bufferDuration;
    
        public float LastInputTime { get; set; }

        public InputBuffer(Func<float> bufferDuration)
        {
            this.bufferDuration = bufferDuration;
            LastInputTime = float.MinValue;
        }

        public bool Flush()
        {
            bool output = Time.time - LastInputTime <= bufferDuration();
            LastInputTime = float.MaxValue;
            return output;
        }
    }
    
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
    private InputBuffer jumpBuffer;

    public Vector2 Aim => aim.action.ReadValue<Vector2>();
    private float WallCheckOverlapDistance
    {
        get
        {
            float pixelWidth = 2f * camera.orthographicSize / Screen.height;
            return movementParameters.GracePixels * pixelWidth;
        }
    }

    public IInputBuffer JumpBuffer => jumpBuffer;
    public Vector2 Position => transform.position;
    public Vector2 Velocity => velocity;

    private void OnEnable()
    {
        movementController = new(movementParameters, this);
        jumpBuffer = new InputBuffer(() => movementParameters.JumpBufferDuration);
        
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

    public bool GrappleRaycast(out Vector2 anchor)
    {
        float aimAngle = Vector2.SignedAngle(Vector2.right, aim.action.ReadValue<Vector2>());
        float snappedAngle = Mathf.Round(aimAngle / movementParameters.AngleSnapIncrement) * movementParameters.AngleSnapIncrement;
        Vector2 direction = Quaternion.Euler(0f, 0f, snappedAngle) * Vector3.right;
        
        RaycastHit2D hit = Physics2D.Raycast(transform.position, direction, movementParameters.RopeLength, LayerMask.GetMask("Ground"));
        anchor = hit.point;
        return hit;
    }

    private void OnJump(InputAction.CallbackContext _)
    {
        jumpBuffer.LastInputTime = Time.time;
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