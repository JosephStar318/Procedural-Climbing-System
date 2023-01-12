using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody), typeof(Collider))]
[RequireComponent(typeof(PlayerInput))]
public class PlayerController : MonoBehaviour
{
    [SerializeField] private Camera mainCamera;

    [Header("Physics")]
    private Rigidbody rb;
    [SerializeField] private float jumpForce = 100f;

    [SerializeField] private float walkSpeed = 1;
    [SerializeField] private float sprintSpeed = 2;

    [SerializeField] private bool isGrounded = true;
    [SerializeField] private bool isHanging = false;
    [SerializeField] private bool sprinting = false;
    
    private Vector3 moveVector;

    #region Utility
    [Header("Utility")]
    [SerializeField] private LayerMask walkableLayers;

    #endregion

    #region Animator Related
    private Animator animator;

    private int speedHash;
    private int speedXHash;
    private int speedZHash;
    private int jumpTriggerHash;
    private int groundedHash;
    private int hangingHash;
    private int fallingHash;
    private int bracedHash;


    private float targetX;
    private float targetZ;
    private float speedFactor;
    #endregion

    private void Start()
    {
        animator = GetComponentInChildren<Animator>();
        rb = GetComponent<Rigidbody>();

        speedHash = Animator.StringToHash("Speed");
        speedXHash = Animator.StringToHash("SpeedX");
        speedZHash = Animator.StringToHash("SpeedZ");
        jumpTriggerHash = Animator.StringToHash("Jump");
        hangingHash = Animator.StringToHash("Hanging");
        groundedHash = Animator.StringToHash("Grounded");
        fallingHash = Animator.StringToHash("Falling");
        //bracedHash = Animator.StringToHash("Braced");

        Cursor.lockState = CursorLockMode.Confined;
        Cursor.visible = false;
    }

    private void FixedUpdate()
    {
        Cursor.visible = false;
        if (isHanging == false)
        {
            MovePlayer();
            RotatePlayer();
            CheckGrounding();
        }
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

        animator.SetFloat(speedHash, speedFactor);
        animator.SetFloat(speedXHash, moveVector.x);
        animator.SetFloat(speedZHash, moveVector.z);

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
    private void CheckGrounding()
    {
        if (Physics.Raycast(transform.position + Vector3.up, Vector3.down, out RaycastHit hit, 5f, walkableLayers))
        {
            if (hit.distance < 2)
            {
                isGrounded = true;
                animator.SetBool(fallingHash, false);
                animator.SetBool(groundedHash, true);
            }
            else
            {
                isGrounded = false;
                animator.SetBool(fallingHash, true);
                animator.SetBool(groundedHash, false);
            }
        }
        else
        {
            isGrounded = false;
            animator.SetBool(fallingHash, true);
            animator.SetBool(groundedHash, false);
        }
    }
    public void Jump()
    {
        rb.AddForce(transform.up * jumpForce, ForceMode.VelocityChange);
    }

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
        if(isGrounded)
        {
            animator.SetTrigger(jumpTriggerHash);
        }
    }
    
}
