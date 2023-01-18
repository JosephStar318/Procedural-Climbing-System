using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody), typeof(Collider))]
[RequireComponent(typeof(PlayerInput))]
public class PlayerController : MonoBehaviour
{
    public static event Action OnDropPressed;
    public static event Action OnDropped;

    #region Physic Related
    [Header("Physics")]
    [SerializeField] private float jumpForce = 100f;
    [SerializeField] private float fallingHeightThreshold = 2f;
    [SerializeField] private float groundingHeightThreshold = 1.1f;

    [SerializeField] private float walkSpeed = 1;
    [SerializeField] private float sprintSpeed = 2;

    [SerializeField] private bool isGrounded = true;
    [SerializeField] private bool isFalling = false;
    [SerializeField] private bool isHanging = false;
    [SerializeField] private bool sprinting = false;

    private Rigidbody rb;
    private Vector3 moveVector;
    public Vector3 jumpPoint;
    #endregion
    #region Utility
    [Header("Utility")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private LayerMask walkableLayers;

    #endregion
    #region Animator Related
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

    private void Start()
    {
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody>();
        #region HashList
        HashManager.AddToAnimatorHash(AnimatorVariables.Speed,                    "Speed");
        HashManager.AddToAnimatorHash(AnimatorVariables.SpeedX,                   "SpeedX");
        HashManager.AddToAnimatorHash(AnimatorVariables.SpeedZ,                   "SpeedZ");
        HashManager.AddToAnimatorHash(AnimatorVariables.Jump,                     "Jump");
        HashManager.AddToAnimatorHash(AnimatorVariables.FallingHang,              "Falling Hang");
        HashManager.AddToAnimatorHash(AnimatorVariables.Grounded,                 "Grounded");
        HashManager.AddToAnimatorHash(AnimatorVariables.Falling,                  "Falling");
        HashManager.AddToAnimatorHash(AnimatorVariables.Braced,                   "Braced");
        HashManager.AddToAnimatorHash(AnimatorVariables.Drop,                     "Drop");
        HashManager.AddToAnimatorHash(AnimatorVariables.ClimbOver,                "Climb Over");
        HashManager.AddToAnimatorHash(AnimatorVariables.ClimbingOverState,        "Climbing Over");
        HashManager.AddToAnimatorHash(AnimatorVariables.HangingIdleState,         "Hanging Blend Tree");
        HashManager.AddToAnimatorHash(AnimatorVariables.FallingToBracedHangState, "Falling To Braced Hang");
        #endregion
        Cursor.lockState = CursorLockMode.Confined;
        Cursor.visible = false;
    }

    private void FixedUpdate()
    {
        if (IsHanging == false)
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
        rb.velocity = new Vector3(0, rb.velocity.y, 0);
        if (Physics.SphereCast(transform.position + Vector3.up, 0.4f, Vector3.down, out RaycastHit hit, 5f, walkableLayers))
        {
            if (hit.distance < groundingHeightThreshold)
            {
                IsGrounded = true;
                OnDropped?.Invoke();
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

     ///<summary>
     /// Jumps the player by jumpForce
     ///</summary>
    public void Jump()
    {
        jumpPoint = transform.position;
        rb.AddForce(transform.up * jumpForce, ForceMode.VelocityChange);
    }

    #region Events

    ///<summary>
    /// Triggered when input for movement vector is changed
    ///</summary>
    private void OnMove(InputValue value)
    {
        targetX = value.Get<Vector2>().x;
        targetZ = value.Get<Vector2>().y;
    }

    ///<summary>
    /// Triggered when input for sprint key is changed
    ///</summary>
    private void OnSprint(InputValue value)
    {
        Sprinting = value.isPressed;
    }

    ///<summary>
    /// Triggered when input for jump key is changed
    ///</summary>
    private void OnJump(InputValue value)
    {
        if(IsGrounded && value.isPressed)
        {
            animator.SetTrigger(HashManager.animatorHashDict[AnimatorVariables.Jump]);
        }
    }

    ///<summary>
    /// Triggered when input for drop key is changed
    ///</summary>
    private void OnDrop(InputValue value)
    {
        if (IsHanging)
        {
            OnDropPressed.Invoke();
            //rb.AddForce(-transform.forward * jumpForce * 10, ForceMode.VelocityChange);
        }
    }
    #endregion
}
