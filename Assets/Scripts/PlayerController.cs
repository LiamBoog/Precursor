using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

public class PlayerController : MonoBehaviour
{
    [Serializable]
    public class MovementParameters
    {
        public const float REFERENCE_FRAMERATE = 60f;

        [Header("Movement Parameters")]
        [SerializeField] private float topSpeed = 10f;
        [SerializeField] private float accelerationDistance = 0.5f;
        [SerializeField] private float decelerationDistance = 0.5f;

        [Header("Jump Parameters")] 
        [SerializeField] private float maxJumpHeight = 4f;
        [SerializeField] private float minJumpHeight = 2f;
        [SerializeField] private float riseDistance = 3.5f;
        [SerializeField] private float fallDistance = 2.5f;
        [SerializeField] private float cancelledJumpRise = 0.75f;
        [SerializeField] private int coyoteFrames = 4;
        [SerializeField] private int jumpBufferFrames = 3;

        [Header("Wall Jump Parameters")] 
        [SerializeField] private float climbHeight = 0f;
        [SerializeField] private int gracePixels = 2;
        [SerializeField] private float wallSlideVelocityFactor = 0.15f;

        [Header("Rope Dart Parameters")] 
        [SerializeField] private float ropeLength = 7.5f;
        [SerializeField] private float angleSnapIncrement = 22.5f;
        
        [Header("Input Actions")]
        [SerializeField] private InputActionReference aim;

        public float TopSpeed => topSpeed;
        public float Acceleration => 0.5f * topSpeed * topSpeed / accelerationDistance;
        public float Deceleration => 0.5f * topSpeed * topSpeed / decelerationDistance;
        public float RiseGravity => 2f * maxJumpHeight * topSpeed * topSpeed / (riseDistance * riseDistance);
        public float FallGravity => 2f * maxJumpHeight * topSpeed * topSpeed / (fallDistance * fallDistance);
        public float JumpVelocity => 2f * maxJumpHeight * topSpeed / riseDistance;
        public float TerminalVelocity => 2f * maxJumpHeight * topSpeed / fallDistance;
        public float JumpBufferDuration => jumpBufferFrames / REFERENCE_FRAMERATE;
        public float CoyoteTime => coyoteFrames / REFERENCE_FRAMERATE;

        public float WallSlideVelocity => wallSlideVelocityFactor * TerminalVelocity;

        public Vector2 Aim => aim.action.ReadValue<Vector2>();

        public void EnableInputActions(bool enable = true)
        {
            if (enable)
            {
                aim.action.Enable();
                return;
            }

            aim.action.Disable();
        }
    }

    [SerializeField] private MovementParameters movementParameters;
    
    [Header("Input Actions")]
    [SerializeField] private InputActionReference jump;
    [SerializeField] private InputActionReference grapple;
    [SerializeField] private InputActionReference anchor;
    
    [SerializeField] private CollisionResolver collisionResolver;

    private MovementStateMachine movementController;
    private Vector2 velocity;

    private void OnEnable()
    {
        movementController = new(movementParameters);
        
        movementParameters.EnableInputActions();
        jump.action.Enable();
        grapple.action.Enable();
        anchor.action.Enable();
    }

    private void OnDisable()
    {
        movementParameters.EnableInputActions(false);
        jump.action.Disable();
        grapple.action.Disable();
        anchor.action.Disable();
    }

    private void Update()
    {
        KinematicState<Vector2> kinematics = new (transform.position, velocity);
        movementController.Update(Time.deltaTime, ref kinematics);
        Vector2 displacement = kinematics.position - (Vector2) transform.position;
        displacement = Quaternion.Euler(0f, 0f, 15f) * displacement;
        CollisionResolver.ICollision collision = collisionResolver.Collide(displacement);
        //Debug.Log(collision.Normal);
        if (collision.Normal != default)
        {
            Bounds bounds = GetComponent<BoxCollider2D>().bounds;
            IEnumerable<Vector2> corners = new Vector2[]
            {
                bounds.center + bounds.extents, 
                bounds.center - bounds.extents, 
                bounds.center + Quaternion.Euler(0f, 0f, 90f) * bounds.extents, 
                bounds.center - Quaternion.Euler(0f, 0f, 90f) * bounds.extents
            };
            foreach (Vector2 corner in corners)
            {
                Debug.DrawRay(corner, displacement, Color.blue, 1f);
                Debug.DrawRay(corner, displacement + collision.Deflection, Color.red, 1f);
            }

            displacement += collision.Deflection;
            //EditorApplication.isPaused = true;
        }
        
        transform.position += (Vector3) displacement;
        velocity = kinematics.velocity;
        Physics2D.SyncTransforms();
    }
}
