using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

[Serializable]
public class MovementParameters
{
    [Serializable]
    public class JumpParameters
    {
        [field: SerializeField] public float MaxJumpHeight { get; private set; } = 4f;
        [field: SerializeField] public float MinJumpHeight { get; private set; } = 2f;
        [field: SerializeField] public float MaxJumpDistance { get; private set; } = 6f;
        [field: SerializeField] public float CancelledJumpRise { get; private set; } = 0.75f;
    }
    
    public const float REFERENCE_FRAMERATE = 60f;

    [field: Header("Movement Parameters")]
    [field: SerializeField] public virtual float TopSpeed { get; protected set; } = 10f;
    [field: SerializeField] public float AccelerationDistance { get; protected set; } = 0.5f;
    [field: SerializeField] public float DecelerationDistance { get; protected set; } = 0.5f;

    [field: Header("Jump Parameters")] 
    [SerializeField] protected JumpParameters defaultJump;
    [SerializeField, Range(0f, 1f)] private float riseRatio = 0.58333333333f;
    [SerializeField] private int coyoteFrames = 4;
    [SerializeField] private int jumpBufferFrames = 3;

    [field: Header("Wall Jump Parameters")] 
    [field: SerializeField] public float ClimbHeight { get; private set; } = 0f;
    [field: SerializeField] public int GracePixels { get; private set; } = 2;
    [SerializeField] private float wallSlideVelocityFactor = 0.15f;

    [field: Header("Rope Dart Parameters")] 
    [field: SerializeField] public float RopeLength { get; private set; } = 7.5f;

    [SerializeField] public int angleSubdivisions = 8;
    [field: SerializeField] public float MinRopeLengthFactor { get; private set; } = 0.75f;

    [field: Header("Swing Parameters")]
    [field: SerializeField] public float AngularAcceleration { get; private set; } = 180f;
    [field: SerializeField] public float MaxSwingAngle { get; private set; } = 45f;
    [field: SerializeField] public int DeadSwingCount { get; private set; } = 40;

    [field: Header("Grapple Parameters")]
    [field: SerializeField] public float GrappleSpeed { get; private set; } = 10f;
    [SerializeField] private float impactDistance = 0.6f;
    [field: SerializeField] public float ImpactSpeed { get; private set; } = 20f;
    [field: SerializeField] public float MaxGrappleDuration { get; private set; } = 0.5f;
    
    [field: Header("Jump Variant Parameters")]
    [field: SerializeField] public float MaxGrappleWallJumpHeight { get; private set; } = 6f;
    [field: SerializeField] public float WallSwingMaxAngle { get; private set; } = 90f;

    protected virtual float MaxHorizontalJumpSpeed => TopSpeed;
    private float ImpactDuration => 2f * impactDistance / (ImpactSpeed + TopSpeed);
    protected float JumpDuration => MaxJumpDistance / MaxHorizontalJumpSpeed;

    public virtual float Acceleration => GetAcceleration(TopSpeed, AccelerationDistance);
    public virtual float Deceleration => GetAcceleration(TopSpeed, DecelerationDistance);
    
    public virtual float MaxJumpHeight => defaultJump.MaxJumpHeight;
    public float MinJumpHeight => defaultJump.MinJumpHeight;
    public virtual float MaxJumpDistance => defaultJump.MaxJumpDistance;
    public float CancelledJumpRise => defaultJump.CancelledJumpRise;
    public virtual float RiseTime => riseRatio * JumpDuration;
    public virtual float FallTime => (1f - riseRatio) * JumpDuration;
    public float RiseGravity => GetGravity(MaxJumpHeight, RiseTime);
    public float FallGravity => GetGravity(MaxJumpHeight, FallTime);
    public virtual float JumpVelocity => GetJumpVelocity(MaxJumpHeight, RiseTime);
    public virtual float TerminalVelocity => GetJumpVelocity(MaxJumpHeight, FallTime);
    public float JumpBufferDuration => jumpBufferFrames / REFERENCE_FRAMERATE;
    public float CoyoteTime => coyoteFrames / REFERENCE_FRAMERATE;

    public float WallSlideVelocity => wallSlideVelocityFactor * TerminalVelocity;

    public float AngleSnapIncrement => 360f / angleSubdivisions;
    
    public float ImpactAcceleration => (TopSpeed - ImpactSpeed) / ImpactDuration;
    
    protected float GetAcceleration(float topSpeed, float distance) => 0.5f * topSpeed * topSpeed / distance;
    protected float GetGravity(float maxJumpHeight, float duration) => GetJumpVelocity(maxJumpHeight, duration) / duration;
    protected float GetJumpVelocity(float maxJumpHeight, float duration) => 2f * maxJumpHeight / duration;
}

public interface IPlayerInfo
{
    Vector2 Aim { get; }
    IInputBuffer JumpBuffer { get; }

    bool GroundCheck();
    int WallCheck();
    bool GrappleRaycast(out Vector2 anchor);
    IEnumerable<Vector2> Touching(IEnumerable<Vector2> directions);
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

public struct AnchorInterrupt : IInterrupt { }

public struct GrappleInterrupt : IInterrupt { }

public interface IInputBuffer
{
    bool Flush();
}

public class PlayerController : MonoBehaviour, IPlayerInfo, ICameraTarget
{
    private class InputBuffer : IInputBuffer
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
            LastInputTime = float.MinValue;
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
    [SerializeField] private LayerMask grappleLayer;

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
        anchor.action.performed += OnAnchor;
        grapple.action.performed += OnGrapple;
    }

    private void OnDisable()
    {
        aim.action.Disable();
        jump.action.Disable();
        grapple.action.Disable();
        anchor.action.Disable();
        
        jump.action.performed -= OnJump;
        jump.action.canceled -= OnJumpCancelled;
        anchor.action.performed -= OnAnchor;
        grapple.action.performed -= OnGrapple;
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
        Debug.DrawRay(transform.position, displacement, Color.blue, 2f);
        
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
        Vector2 aimDirection = aim.action.ReadValue<Vector2>();
        if (aimDirection == Vector2.zero)
        {
            anchor = default;
            return false;
        }
        
        float aimAngle = Vector2.SignedAngle(Vector2.up, aimDirection);
        float snappedAngle = Mathf.Round(aimAngle / movementParameters.AngleSnapIncrement) * movementParameters.AngleSnapIncrement;
        Vector2 direction = Quaternion.Euler(0f, 0f, snappedAngle) * Vector3.up;
        
        Debug.DrawRay(transform.position, movementParameters.RopeLength * direction, Color.magenta, 0.5f);
        RaycastHit2D hit = Physics2D.Raycast(transform.position, direction, movementParameters.RopeLength, grappleLayer);
        anchor = hit.point;
        return hit;
    }

    public IEnumerable<Vector2> Touching(IEnumerable<Vector2> directions)
    {
        return directions.Where(d => collisionResolver.Touching(d));
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

    private void OnAnchor(InputAction.CallbackContext _)
    {
        interrupts.Add(new AnchorInterrupt());
    }

    private void OnGrapple(InputAction.CallbackContext _)
    {
        interrupts.Add(new GrappleInterrupt());
    }
}