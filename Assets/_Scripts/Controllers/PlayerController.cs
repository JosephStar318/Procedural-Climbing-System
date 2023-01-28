using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Interactions;

[RequireComponent(typeof(Rigidbody), typeof(Collider))]
[RequireComponent(typeof(PlayerInput))]
public class PlayerController : MonoBehaviour
{
    #region Physic Related Variables
    [Header("Physics")]
    [SerializeField] private float jumpForce = 100f;
    [SerializeField] private float fallingHeightThreshold = 2f;
    [SerializeField] private float groundingHeightThreshold = 1.1f;

    [SerializeField] private float walkSpeed = 1;
    [SerializeField] private float sprintSpeed = 2;

    [Header("States")]
    [SerializeField] private bool isGrounded = true;
    [SerializeField] private bool isFalling = false;
    [SerializeField] private bool isHanging = false;
    [SerializeField] private bool sprinting = false;

    private Rigidbody rb;
    private Vector3 moveVector;
    public Vector3 jumpPoint;
    #endregion

    #region Utility Variables
    [Header("Utility")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private LayerMask walkableLayers;

    #endregion

    #region Animator Related Variables
    private Animator animator;

    private float targetX;
    private float targetZ;
    private float speedFactor;

    #endregion

    public bool IsGrounded { get => isGrounded; 
        private set 
        {
            isGrounded = value;
            animator.SetBool(HashManager.animatorHashDict[AnimatorVariables.Grounded], value);
        }
    }
    public bool IsFalling { get => isFalling; 
        private set 
        {
            isFalling = value;
            animator.SetBool(HashManager.animatorHashDict[AnimatorVariables.Falling], value);
        }
    }
    public bool IsHanging { get => isHanging; set => isHanging = value; }
    public bool Sprinting { get => sprinting; private set => sprinting = value; }
    public bool IsPlayerEnabled { get; private set; }

    private void OnEnable()
    {
        PlayerInputHelper.OnJump += OnJump;
        PlayerInputHelper.OnMove += OnMove;
        PlayerInputHelper.OnSprint += OnSprint;
    }
    private void OnDisable()
    {
        PlayerInputHelper.OnJump -= OnJump;
        PlayerInputHelper.OnMove -= OnMove;
        PlayerInputHelper.OnSprint -= OnSprint;
    }

    private void Start()
    {
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody>();
        IsPlayerEnabled = true;
        Cursor.lockState = CursorLockMode.Confined;
        Cursor.visible = false;
    }

    private void FixedUpdate()
    {
        if (IsHanging == false && IsPlayerEnabled)
        {
            MovePlayer();
            RotatePlayer();
        }
        CheckInAirState();
    }
    private void Update()
    {
        Cursor.visible = false;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawRay(transform.position, Vector3.down);
    }

    ///<summary>
    /// Moves the player
    ///</summary>
    private void MovePlayer()
    {
        if (Sprinting)
        {
            speedFactor = Mathf.Lerp(speedFactor, sprintSpeed, Time.deltaTime * 3);
        }
        else
        {
            speedFactor = Mathf.Lerp(speedFactor, walkSpeed, Time.deltaTime * 3);
        }
        moveVector.x = Mathf.Lerp(moveVector.x, targetX * speedFactor, Time.deltaTime * 3);
        moveVector.z = Mathf.Lerp(moveVector.z, targetZ * speedFactor, Time.deltaTime * 3);

        animator.SetFloat(HashManager.animatorHashDict[AnimatorVariables.Speed], speedFactor);
        animator.SetFloat(HashManager.animatorHashDict[AnimatorVariables.SpeedX], moveVector.x);
        animator.SetFloat(HashManager.animatorHashDict[AnimatorVariables.SpeedZ], moveVector.z);

        transform.Translate(moveVector * Time.fixedDeltaTime * speedFactor);

    }

    ///<summary>
    /// Rotates player based on camera angle if the player is moving
    ///</summary>
    private void RotatePlayer()
    {
        if (moveVector.magnitude > 0.5f)
        {
            Vector3 rotationVector = transform.position - mainCamera.transform.position;
            rotationVector.y = 0;
            transform.forward = Vector3.Lerp(transform.forward, rotationVector, Time.deltaTime);
        }
    }

    ///<summary>
    /// Checks if player is grounded and falling
    ///</summary>
    private void CheckInAirState()
    {
        if (Physics.SphereCast(transform.position + Vector3.up, 0.4f, Vector3.down, out RaycastHit hit, 5f, walkableLayers))
        {
            if (hit.distance < groundingHeightThreshold)
            {
                IsGrounded = true;
            }
            else
            {
                IsGrounded = false;
            }

            if (hit.distance > fallingHeightThreshold)
            {
                IsFalling = true;
            }
            else
            {
                IsFalling = false;
            }
        }
        else
        {
            IsGrounded = false;
            IsFalling = true;
        }
    }

    /// <summary>
    /// Triggered from jump animation to jump the player
    /// </summary>
    public void Jump()
    {
        rb.AddForce(transform.up * jumpForce, ForceMode.VelocityChange);
    }

    ///<summary>
    /// Enables the player controller
    ///</summary>
    public void EnablePlayerController()
    {
        IsPlayerEnabled = true;
        rb.velocity = Vector3.zero;
    }

    ///<summary>
    /// Disables the player controller
    ///</summary>
    public void DisablePlayerController()
    {
        IsPlayerEnabled = false;
        rb.velocity = Vector3.zero;
    }

    #region Events

    ///<summary>
    /// Triggered when input for movement vector is changed
    ///</summary>
    private void OnMove(Vector2 input)
    {
        targetX = input.x;
        targetZ = input.y;
    }

    ///<summary>
    /// Triggered when input for sprint key is changed
    ///</summary>
    private void OnSprint(bool value)
    {
        Sprinting = value;
    }

    ///<summary>
    /// Triggered when input for jump key is changed
    ///</summary>
    public void OnJump(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            if (context.ReadValueAsButton() && IsGrounded && IsPlayerEnabled)
            {
                jumpPoint = transform.position;
                
                animator.SetTrigger(HashManager.animatorHashDict[AnimatorVariables.Jump]);
            }
        }
    }
    #endregion
}
