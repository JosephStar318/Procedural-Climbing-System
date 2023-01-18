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
    [SerializeField] private float fallingHeightThreshold;

    [SerializeField] private float walkSpeed = 1;
    [SerializeField] private float sprintSpeed = 2;

    public bool isGrounded = true;
    public bool isFalling = false;
    public bool isHanging = false;
    public bool sprinting = false;
    
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
        if (isHanging == false)
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
    private void MovePlayer()
    {
        if (sprinting)
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

    private void RotatePlayer()
    {
        if (moveVector.magnitude > 0.5f)
        {
            Vector3 rotationVector = transform.position - mainCamera.transform.position;
            rotationVector.y = 0;
            transform.forward = Vector3.Lerp(transform.forward, rotationVector, Time.deltaTime);
        }
    }
    private void CheckInAirState()
    {
        rb.velocity = new Vector3(0, rb.velocity.y, 0);
        if (Physics.SphereCast(transform.position + Vector3.up, 0.4f, Vector3.down, out RaycastHit hit, 5f, walkableLayers))
        {
            if (hit.distance < 1.1f)
            {
                isGrounded = true;
                animator.SetBool(HashManager.animatorHashDict[AnimatorVariables.Grounded], true);
                OnDropped.Invoke();
            }
            else
            {
                isGrounded = false;
                animator.SetBool(HashManager.animatorHashDict[AnimatorVariables.Grounded], false);
            }

            if (hit.distance > fallingHeightThreshold)
            {
                isFalling = true;
                animator.SetBool(HashManager.animatorHashDict[AnimatorVariables.Falling], true);
            }
            else
            {
                isFalling = false;
                animator.SetBool(HashManager.animatorHashDict[AnimatorVariables.Falling], false);
            }
        }
        else
        {
            isGrounded = false;
            animator.SetBool(HashManager.animatorHashDict[AnimatorVariables.Grounded], false);

            isFalling = true;
            animator.SetBool(HashManager.animatorHashDict[AnimatorVariables.Falling], true);
        }
    }
    public void Jump()
    {
        jumpPoint = transform.position;
        rb.AddForce(transform.up * jumpForce, ForceMode.VelocityChange);
    }
    
    #region Events
    private void OnMove(InputValue value)
    {
        targetX = value.Get<Vector2>().x;
        targetZ = value.Get<Vector2>().y;
    }
    private void OnSprint(InputValue value)
    {
        sprinting = value.isPressed;
    }
    private void OnJump(InputValue value)
    {
        if(isGrounded && value.isPressed)
        {
            animator.SetTrigger(HashManager.animatorHashDict[AnimatorVariables.Jump]);
        }
    }

    private void OnDrop(InputValue value)
    {
        if (isHanging)
        {
            OnDropPressed.Invoke();
            //rb.AddForce(-transform.forward * jumpForce * 10, ForceMode.VelocityChange);
        }
    }
    #endregion
}
