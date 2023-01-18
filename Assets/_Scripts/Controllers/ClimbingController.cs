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
    private CapsuleCollider capsuleCollider;

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
    [SerializeField] private Vector3 hangOffset;

    private Vector3 endPosition;
    private Quaternion forwardNormalXZRotation;
    private Vector3 matchTargetPosition;
    private Quaternion matchTargetRotation;
    private MatchTargetWeightMask weightMask = new MatchTargetWeightMask(Vector3.one, 1);
    private float startNormalizedTime;
    private float targetNormalizedTime;

    private float hangTimeout = 0;


    private void OnEnable()
    {
        PlayerController.OnDropPressed += OnDropPressed;
        PlayerController.OnDropped += OnDropped;
        SMBEvent.OnSMBEvent += OnSMBEvent;
    }
    private void OnDisable()
    {
        PlayerController.OnDropPressed -= OnDropPressed;
        PlayerController.OnDropped -= OnDropped;
        SMBEvent.OnSMBEvent -= OnSMBEvent;
    }
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.blue;
        Gizmos.DrawSphere(endPosition, 0.1f);
        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(downCastHit.point, 0.1f);
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(forwardCastHit.point, 0.1f);
        Gizmos.color = Color.green;
        Gizmos.DrawSphere(matchTargetPosition, 0.1f);
    }
    private void Start()
    {
        animator = GetComponent<Animator>();
        playerController = GetComponent<PlayerController>();
        rb = GetComponent<Rigidbody>();
        capsuleCollider = GetComponent<CapsuleCollider>();
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
                if(transform.position.y + vaultHeight/2 < downCastHit.point.y)
                {
                    Hang();
                    if (GlobalSettings.Instance.debugMode) Debug.Log("Hanging while falling");
                }
            }
            else if(playerController.isGrounded == false)
            {
                jumpHeight = downCastHit.point.y - playerController.jumpPoint.y;
                if(jumpHeight > vaultHeight && jumpHeight <= hangHeight && Vector3.Distance(transform.position,downCastHit.point) < 1.8f)
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
    private void OnAnimatorMove()
    {
        if (animator.isMatchingTarget)
            animator.ApplyBuiltinRootMotion();
    }

    private void Vault()
    {
        hangTimeout = 1f;

        matchTargetPosition = endPosition;
        matchTargetRotation = forwardNormalXZRotation;

        //animator.setbool
    }

    private void Hang()
    {
        Vector3 forwardNormalXZ = Vector3.ProjectOnPlane(forwardCastHit.normal, Vector3.up);
        forwardNormalXZRotation = Quaternion.LookRotation(-forwardNormalXZ, Vector3.up);

        matchTargetPosition = forwardCastHit.point + forwardNormalXZRotation * hangOffset;
        matchTargetRotation = forwardNormalXZRotation;

        animator.SetTrigger(HashManager.animatorHashDict[AnimatorVariables.FallingHang]);

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
                Debug.Log("can climb over");
                //ClimbOver();
            }
            else
            {
                Debug.Log("can't climb over");
            }
        }
    }

    private void ClimbOver()
    {
        matchTargetPosition = endPosition;
        matchTargetRotation = forwardNormalXZRotation;
        playerController.isHanging = false;
        Debug.Log("climbing over..");
        animator.SetTrigger(HashManager.animatorHashDict[AnimatorVariables.ClimbOver]);
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

                        Collider colliderLedge = downCastHit.collider;
                        bool penetrationOverlap = Physics.ComputePenetration(
                            colliderA: capsuleCollider,
                            positionA: endPosition,
                            rotationA: transform.rotation,
                            colliderB: colliderLedge,
                            positionB: colliderLedge.transform.position,
                            rotationB: colliderLedge.transform.rotation,
                            direction: out Vector3 penetrationDirection,
                            distance: out float penetrationDistance
                            );
                        if (penetrationOverlap)
                            endPosition += penetrationDirection * penetrationDistance;
                        return true;
                    }
                }
            }
        }
        return false;
    }

    private bool CanClimbOver()
    {
        float inflate = -0.05f;
        float upsweepDistance = downCastHit.point.y - transform.position.y;
        Vector3 upSweepDirection = transform.up;
        Vector3 upSweepOrigin = transform.position;
        bool upSweepHit = CharacterSweep(
            position: upSweepOrigin,
            rotation: transform.rotation,
            direction: upSweepDirection,
            distance: upsweepDistance,
            layerMask: climbableLayers,
            inflate: inflate
            );

        Vector3 forwardSweepOrigin = transform.position + upSweepDirection * upsweepDistance;
        Vector3 forwardSweepVector = endPosition - forwardSweepOrigin;
        bool forwardSweepHit = CharacterSweep(
                position: forwardSweepOrigin,
                rotation: transform.rotation,
                direction: forwardSweepVector.normalized,
                distance: forwardSweepVector.magnitude,
                layerMask: climbableLayers,
                inflate: inflate
            );
        if(!upSweepHit && !forwardSweepHit)
        {
            return true;
        }
        return false;
    }

    private bool CharacterSweep(Vector3 position, Quaternion rotation, Vector3 direction, float distance, LayerMask layerMask, float inflate)
    {
        float heightScale = Mathf.Abs(transform.lossyScale.y);
        float radiusScale = Mathf.Max(Mathf.Abs(transform.lossyScale.x), Mathf.Abs(transform.lossyScale.z));

        float radius = capsuleCollider.radius * radiusScale;
        float totalHeight = Mathf.Max(capsuleCollider.height * heightScale, radius * 2);

        Vector3 capsuleUp = rotation * Vector3.up;
        Vector3 center = position + rotation * capsuleCollider.center;
        Vector3 top = center + capsuleUp * (totalHeight / 2 - radius);
        Vector3 bottom = center - capsuleUp * (totalHeight / 2 - radius);

        bool sweepHit = Physics.CapsuleCast(
                point1: bottom,
                point2: top,
                radius: radius + inflate,
                direction: direction,
                maxDistance: distance,
                layerMask: layerMask
            );
        return sweepHit;
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
    private void OnDropPressed()
    {
        animator.SetTrigger(HashManager.animatorHashDict[AnimatorVariables.Drop]);

        playerController.isHanging = false;
        rb.useGravity = true;
        rb.isKinematic = false;

        hangTimeout = 1f;
    }
    private void OnDropped()
    {
    }
    private void OnSMBEvent(AnimatorStateInfo asInfo, AnimatorState animatorState)
    {

        if (asInfo.shortNameHash == HashManager.animatorHashDict[AnimatorVariables.FallingToBracedHangState])
        {
            if (animatorState == AnimatorState.Enter)
            {
                rb.isKinematic = true;
            }
            else if (animatorState == AnimatorState.Update)
            {
                if (animator.IsInTransition(0)) return;
                if (animator.isMatchingTarget) return;

                startNormalizedTime = 0;
                targetNormalizedTime = 0.25f;
                animator.MatchTarget(matchTargetPosition, matchTargetRotation, AvatarTarget.Root, weightMask, startNormalizedTime, targetNormalizedTime);
                rb.velocity = Vector3.zero;
            }
            else if (animatorState == AnimatorState.Exit)
            {

            }
        }
        else if (asInfo.shortNameHash == HashManager.animatorHashDict[AnimatorVariables.ClimbingOverState])
        {
            if (animatorState == AnimatorState.Enter)
            {
                
            }
            else if (animatorState == AnimatorState.Update)
            {
                if (animator.IsInTransition(0)) return;
                if (animator.isMatchingTarget) return;
                startNormalizedTime = 0.3f;
                targetNormalizedTime = 0.8f;
                animator.MatchTarget(matchTargetPosition, matchTargetRotation, AvatarTarget.Root, weightMask, startNormalizedTime, targetNormalizedTime);
            }
            else if (animatorState == AnimatorState.Exit)
            {
                rb.isKinematic = false;
            }
        }
    }
    #endregion
}
