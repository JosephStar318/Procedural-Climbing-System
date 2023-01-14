using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerController))]
public class ClimbingController : MonoBehaviour
{
    private Animator animator;
    private PlayerController playerController;
    private Rigidbody rb;

    private Vector3 moveVector;
    private float targetX;
    private float targetY;
    private bool jumpButtonPressed;

    private RaycastHit downCastHit;
    private RaycastHit forwardCastHit;
    
    [Header("Climb Settings")]
    [SerializeField] private float wallAngleMax;
    [SerializeField] private float groundAngleMax;
    [SerializeField] private LayerMask climbableLayers;

    [SerializeField] private float stepHeight;
    [SerializeField] private float vaultHeight;
    [SerializeField] private float hangHeight;
    [SerializeField] private Vector3 climbOriginDown;
    [SerializeField] private float minStepHeight;
    [SerializeField] private Vector3 endOffset;

    private Vector3 endPosition;
    private Quaternion forwardNormalXZRotation;
    private Vector3 matchTargetPosition;
    private Quaternion matchTargetRotation;
    private MatchTargetWeightMask weightMask = new MatchTargetWeightMask(Vector3.one, 1);
    private Vector3 hangOffset;


    private float hangTimeout = 0;


    private void OnEnable()
    {
        PlayerController.OnDropPressed += OnDropPressed;
        PlayerController.OnDropped += OnDropped;
    }
    private void OnDisable()
    {
        PlayerController.OnDropPressed -= OnDropPressed;
        PlayerController.OnDropped -= OnDropped;
    }
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.blue;
        Gizmos.DrawSphere(endPosition, 0.1f);
    }
    private void Start()
    {
        animator = GetComponent<Animator>();
        playerController = GetComponent<PlayerController>();
        rb = GetComponent<Rigidbody>();
    }
   
    private void FixedUpdate()
    {
        hangTimeout -= Time.fixedDeltaTime;
        if (playerController.isHanging)
        {
            ClimbMovement();
        }
        else if(CanClimb() == true)
        {
            float jumpHeight = 0;
            if(playerController.isFalling)
            {
                float groundDistance;
                if(Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, 100f, climbableLayers))
                {
                    groundDistance = hit.distance;
                    jumpHeight = groundDistance - downCastHit.point.y + climbOriginDown.y;
                }
                if(jumpHeight > vaultHeight)
                {
                    Hang();
                    if (GlobalSettings.Instance.debugMode) Debug.Log("Hanging while falling");
                }
            }
            else if(playerController.isGrounded == false)
            {
                jumpHeight = downCastHit.point.y - playerController.jumpPoint.y;
                if(jumpHeight > vaultHeight && jumpHeight <= hangHeight)
                {
                    Hang();
                    if (GlobalSettings.Instance.debugMode) Debug.Log("Hanging");
                }
                else if(jumpHeight > stepHeight && jumpHeight <= vaultHeight)
                {
                    Vault();
                    if (GlobalSettings.Instance.debugMode) Debug.Log($"Vaulting {jumpHeight}");
                }
                else if(jumpHeight < stepHeight)
                {
                    //do nothing
                    if (GlobalSettings.Instance.debugMode) Debug.Log("Doing Nothing");
                }
            }
        }
    }

    private void Vault()
    {
        hangTimeout = 1f;
    }

    private void Hang()
    {
        animator.SetBool(HashManager.animatorHashDict[AnimatorVariables.Hang], true);
        playerController.isHanging = true;
        rb.useGravity = false;
        rb.velocity = Vector3.zero;
        hangTimeout = 1f;
    }

    private void ClimbMovement()
    {
        moveVector.x = Mathf.Lerp(moveVector.x, targetX, Time.deltaTime * 3);
        moveVector.y = Mathf.Lerp(moveVector.y, targetY, Time.deltaTime * 3);

        if (moveVector.x > 0)
        {

        }
        if(moveVector.y > 0)
        {
           
        }
        if (jumpButtonPressed)
        {
            if (CanClimbOver() == true)
            {
                animator.SetTrigger(HashManager.animatorHashDict[AnimatorVariables.ClimbOver]);

                Vector3 forwardNormalXZ = Vector3.ProjectOnPlane(forwardCastHit.normal, Vector3.up);
                forwardNormalXZRotation = Quaternion.LookRotation(-forwardNormalXZ, Vector3.up);

                matchTargetPosition = forwardCastHit.point + forwardNormalXZRotation * hangOffset;
                matchTargetRotation = forwardNormalXZRotation;
            }
            else
            {

            }
        }
    }

    private bool CanClimb()
    {
        bool downHit;
        bool forwardHit;
        float groundAngle;
        float wallAngle;

        Vector3 forwardDirectionXZ;
        Vector3 forwardNormalXZ;

        Vector3 downDirection = Vector3.down;
        Vector3 downOrigin = transform.TransformPoint(climbOriginDown);
        if (GlobalSettings.Instance.debugMode)
            Debug.DrawLine(transform.position, downOrigin, Color.red);

        downHit = Physics.Raycast(downOrigin, downDirection, out downCastHit, climbOriginDown.y - minStepHeight, climbableLayers);

        if(downHit)
        {
            if (hangTimeout > 0) return false;
            float forwardDistance = climbOriginDown.z;
            Vector3 forwardOrigin = new Vector3(transform.position.x, downCastHit.point.y - 0.1f, transform.position.z);

            forwardDirectionXZ = Vector3.ProjectOnPlane(transform.forward, Vector3.up);
            forwardHit = Physics.Raycast(forwardOrigin, forwardDirectionXZ, out forwardCastHit, forwardDistance, climbableLayers);
            if(forwardHit)
            {
                forwardNormalXZ = Vector3.ProjectOnPlane(forwardCastHit.normal, Vector3.up);
                groundAngle = Vector3.Angle(downCastHit.normal, Vector3.up);
                wallAngle = Vector3.Angle(-forwardNormalXZ, forwardDirectionXZ);

                if(wallAngle <= wallAngleMax)
                {
                    if(groundAngle <= groundAngleMax)
                    {
                        Vector3 vectSurface = Vector3.ProjectOnPlane(forwardDirectionXZ, downCastHit.normal);
                        endPosition = downCastHit.point + Quaternion.LookRotation(vectSurface, Vector3.up) * endOffset;
                        return true;
                    }
                }
            }
        }
        return false;
    }

    private bool CanClimbOver()
    {

        return false;
    }
    #region Events
    private void OnMove(InputValue value)
    {
        targetX = value.Get<Vector2>().x;
        targetY = value.Get<Vector2>().y;
    }

    private void OnJump(InputValue value)
    {
        jumpButtonPressed = value.isPressed;
    }

    private void OnClimbingStart(Vector3 target, Vector3 forward)
    {
        Hang();
        playerController.transform.position = target;
        playerController.transform.forward = forward;
        //StartCoroutine(rb.ChangeRbPositionUntil(target, 1f));
    }
    private void OnDropPressed()
    {
        animator.SetBool(HashManager.animatorHashDict[AnimatorVariables.Hang], false);
        playerController.isHanging = false;
        rb.useGravity = true;

        hangTimeout = 1f;
    }
    private void OnDropped()
    {
    }

    #endregion
}
