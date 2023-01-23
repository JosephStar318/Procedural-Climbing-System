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
    private LedgeDetector ledgeDetector;

    private Vector3 moveVector;
    private Vector3 targetVector;
    private float cornerAngle;
    private bool isCorner;
    private bool jumpButtonPressed;
    private GameObject grabbedLedge;


    [Header("Climb Settings")]
    [SerializeField] private float maxStepHeight = 0.5f;
    [SerializeField] private float maxVaultHeight = 2f;
    [SerializeField] private float maxHangHeight = 2.5f;

    #region Target Matching
    private RaycastHit sideCastHit;

    private Vector3 matchTargetPositionHanging;
    private Quaternion matchTargetRotationHanging;

    private Vector3 matchTargetPositionHoping;
    private Quaternion matchTargetRotationHoping;

    private Vector3 matchTargetPositionVaulting;
    private Quaternion matchTargetRotationVaulting;

    private Vector3 matchTargetPositionClimbOver;
    private Quaternion matchTargetRotationClimbOver;

    private MatchTargetWeightMask weightMask = new MatchTargetWeightMask(Vector3.one, 1);
    private MatchTargetWeightMask noRotWeightMask = new MatchTargetWeightMask(Vector3.one, 0);
    private MatchTargetWeightMask ledgeWeightMask = new MatchTargetWeightMask(new Vector3(0, 1, 1), 0);

    private float startNormalizedTime;
    private float targetNormalizedTime;
    #endregion

    public bool IsClimbMovementEnabled { get; private set; }

    private void OnEnable()
    {
        PlayerInputHelper.OnJump += OnJump;
        PlayerInputHelper.OnDrop += OnDrop;
        PlayerInputHelper.OnMove += OnMove;

        SMBEvent.OnSMBEvent += OnSMBEvent;
    }
    private void OnDisable()
    {
        PlayerInputHelper.OnJump -= OnJump;
        PlayerInputHelper.OnDrop -= OnDrop;
        PlayerInputHelper.OnMove -= OnMove;

        SMBEvent.OnSMBEvent -= OnSMBEvent;
    }
    private void OnDrawGizmos()
    {

    }
    private void Start()
    {
        animator = GetComponent<Animator>();
        playerController = GetComponent<PlayerController>();
        rb = GetComponent<Rigidbody>();
        ledgeDetector = GetComponent<LedgeDetector>();
        IsClimbMovementEnabled = true;
    }
   
    private void FixedUpdate()
    {
        if (playerController.IsHanging)
        {
            ClimbMovement();
        }
        if (ledgeDetector.CanClimb(transform))
        {
            //matching target to keep alligned with the wall
            ledgeDetector.SetTargetMatchingToLedge(out matchTargetPositionHanging, out matchTargetRotationHanging);

            if (playerController.IsHanging || playerController.IsPlayerEnabled == false) return;
            RaycastHit downCastHit = ledgeDetector.ReturnDownCastHit();
            float jumpHeight = downCastHit.point.y - playerController.jumpPoint.y;

            if (playerController.IsFalling)
            {
                if(transform.position.y + maxVaultHeight/2 < downCastHit.point.y)
                {

                    if (GlobalSettings.Instance.debugMode) Debug.Log("Hanging while falling");
                    Hang();
                }
            }
            else if(playerController.IsGrounded == false)
            {
                if(jumpHeight > maxVaultHeight && jumpHeight <= maxHangHeight && transform.position.y + maxVaultHeight / 2 < downCastHit.point.y)
                {
                    if (GlobalSettings.Instance.debugMode) Debug.Log("Hanging");
                    Hang();
                }
            }
            if (jumpHeight > maxStepHeight && jumpHeight <= maxVaultHeight && jumpButtonPressed)
            {
                if (GlobalSettings.Instance.debugMode) Debug.Log($"Vaulting {jumpHeight}");
                Vault();
            }
            else if (jumpHeight < maxStepHeight && jumpButtonPressed)
            {
                if (GlobalSettings.Instance.debugMode) Debug.Log("Doing Nothing");
            }
        }

    }
  
    private void OnAnimatorMove()
    {
        if (animator.isMatchingTarget)
            animator.ApplyBuiltinRootMotion();
        else if (animator.GetCurrentAnimatorStateInfo(0).tagHash == HashManager.animatorHashDict[AnimatorVariables.ApplyRootMotionTag])
        {
            animator.ApplyBuiltinRootMotion();
        }
    }

    ///<summary>
    ///Moves the player while hanging
    ///</summary>
    private void ClimbMovement()
    {
        bool doesLedgeExist = ledgeDetector.CheckLedgeInMoveDirection(targetVector.x, out isCorner, out cornerAngle, out sideCastHit);
        if (IsClimbMovementEnabled == false) return;

        if (doesLedgeExist)
        {
            moveVector.x = Mathf.Lerp(moveVector.x, targetVector.x, Time.deltaTime * 3);
            animator.SetFloat(HashManager.animatorHashDict[AnimatorVariables.SpeedX], moveVector.x);
        }
        else if(isCorner)
        {
            ledgeDetector.SetTargetMatchingToHitPoint(sideCastHit, out matchTargetPositionHoping, out matchTargetRotationHoping);
            Hop();

            if(targetVector.x == 0 && targetVector.y == 0)
            {
                moveVector.x = Mathf.Lerp(moveVector.x, 0, Time.deltaTime * 5);
                animator.SetFloat(HashManager.animatorHashDict[AnimatorVariables.SpeedX], moveVector.x);
            }
        }
        else
        {
            moveVector.x = Mathf.Lerp(moveVector.x, 0, Time.deltaTime * 5);
            animator.SetFloat(HashManager.animatorHashDict[AnimatorVariables.SpeedX], moveVector.x);
            if (ledgeDetector.CheckJumpableLedgeInMoveDirection(targetVector, 3f) == true)
            {
                if (grabbedLedge != ledgeDetector.ReturnDownCastHit().transform.gameObject)
                {
                    ledgeDetector.SetTargetMatchingToLedge(out matchTargetPositionHoping, out matchTargetRotationHoping);
                    Hop();
                }
            }
        }

        if (jumpButtonPressed)
        {
            if(animator.GetCurrentAnimatorStateInfo(0).shortNameHash != HashManager.animatorHashDict[AnimatorVariables.HangingBlendState])
            {
                return;
            }
            if (ledgeDetector.CanClimbOver() == true)
            {
                Debug.Log("can climb over");
                ClimbOver();
            }
            else
            {
                Debug.Log("can't climb over");
            }
        }
    }

    ///<summary>
    ///Plays the Vaulting animation
    ///</summary>
    private void Vault()
    {
        ledgeDetector.SetTargetMatchingToEndpoint(out matchTargetPositionVaulting, out matchTargetRotationVaulting);
        animator.SetTrigger(HashManager.animatorHashDict[AnimatorVariables.Vault]);
        playerController.DisablePlayerController();
    }

    ///<summary>
    ///Plays the hanging animation
    ///</summary>
    private void Hang()
    {
        if (grabbedLedge == ledgeDetector.ReturnDownCastHit().transform.gameObject) return;

        ledgeDetector.SetTargetMatchingToLedge(out matchTargetPositionHanging, out matchTargetRotationHanging);
        animator.SetTrigger(HashManager.animatorHashDict[AnimatorVariables.FallingHang]);
        grabbedLedge = ledgeDetector.ReturnDownCastHit().transform.gameObject;

        playerController.DisablePlayerController();
        playerController.IsHanging = true;
        rb.isKinematic = true;
    }

    /// <summary>
    /// Hops in the moveDirection
    /// </summary>
    private void Hop()
    {
        Debug.Log("hopp");
        if (targetVector.x == 1 && targetVector.y == 0)
        {
            animator.CrossFade(HashManager.animatorHashDict[AnimatorVariables.BracedHangHopRightState], 0.1f);
        }
        else if (targetVector.x == -1 && targetVector.y == 0)
        {
            animator.CrossFade(HashManager.animatorHashDict[AnimatorVariables.BracedHangHopLeftState], 0.1f);
        }
        else if (targetVector.y == 1)
        {
            animator.CrossFade(HashManager.animatorHashDict[AnimatorVariables.BracedHangHopUpState], 0.1f);
            Debug.Log("hop uppp");
        }
        else if (targetVector.y == -1)
        {
            animator.CrossFade(HashManager.animatorHashDict[AnimatorVariables.BracedHangHopDownState], 0.1f);
        }
        grabbedLedge = ledgeDetector.ReturnDownCastHit().transform.gameObject;
        
        IsClimbMovementEnabled = false;
    }

    ///<summary>
    ///Plays the ClimbOver animation
    ///</summary>
    private void ClimbOver()
    {
        ledgeDetector.SetTargetMatchingToEndpoint(out matchTargetPositionClimbOver, out matchTargetRotationClimbOver);
        playerController.IsHanging = false;
        Debug.Log("climbing over..");
        animator.SetTrigger(HashManager.animatorHashDict[AnimatorVariables.ClimbOver]);
    }
 
    #region Events

    ///<summary>
    /// Triggered when the input for move vector is changed
    ///</summary>
    private void OnMove(Vector2 input)
    {
        targetVector = input;
    }

    ///<summary>
    /// Triggered when the input for jump key changed
    ///</summary>
    private void OnJump(InputAction.CallbackContext context)
    {
        jumpButtonPressed = context.ReadValueAsButton();
    }

    ///<summary>
    /// Triggered when the input for drop key pressed
    ///</summary>
    private void OnDrop(InputAction.CallbackContext context)
    {
        if(context.performed && context.ReadValueAsButton())
        {
            animator.SetTrigger(HashManager.animatorHashDict[AnimatorVariables.Drop]);
            playerController.IsHanging = false;
            rb.isKinematic = false;
            rb.AddForce(-transform.forward * 25, ForceMode.Acceleration);
        }
    }
    private void MatchRotation(Quaternion matchRotation, float currentTime, float startNormalizedTime, float endNormalizedTime)
    {
        if(currentTime > startNormalizedTime && currentTime < endNormalizedTime)
        {
            animator.rootRotation = Quaternion.RotateTowards(animator.rootRotation, matchRotation, Time.fixedDeltaTime * 90);
        }

    }
    ///<summary>
    /// State machine behaivor events.
    ///</summary>
    private void OnSMBEvent(AnimatorStateInfo asInfo, AnimatorState animatorState)
    {
        if (asInfo.shortNameHash == HashManager.animatorHashDict[AnimatorVariables.FallingToBracedHangState])
        {
            if (animatorState == AnimatorState.Enter)
            {
                rb.isKinematic = true;
                animator.InterruptMatchTarget(false);
            }
            else if (animatorState == AnimatorState.Update)
            {
                animator.rootRotation= Quaternion.RotateTowards(animator.rootRotation, matchTargetRotationHanging, Time.fixedDeltaTime * 75);
                if (animator.IsInTransition(0)) return;
                if (animator.isMatchingTarget) return;

                startNormalizedTime = 0;
                targetNormalizedTime = 0.3f;
                animator.MatchTarget(matchTargetPositionHanging, matchTargetRotationHanging, AvatarTarget.RightHand, noRotWeightMask, startNormalizedTime, targetNormalizedTime);
            }
            else if (animatorState == AnimatorState.Exit)
            {

            }
        }
        else if (asInfo.shortNameHash == HashManager.animatorHashDict[AnimatorVariables.ClimbingOverState])
        {
            if (animatorState == AnimatorState.Enter)
            {
                animator.InterruptMatchTarget(false);
            }
            else if (animatorState == AnimatorState.Update)
            {
                if (animator.IsInTransition(0)) return;
                if (animator.isMatchingTarget) return;
                startNormalizedTime = 0.7f;
                targetNormalizedTime = 1;
                animator.MatchTarget(matchTargetPositionClimbOver, matchTargetRotationClimbOver, AvatarTarget.Root, noRotWeightMask, startNormalizedTime, targetNormalizedTime);
            }
            else if (animatorState == AnimatorState.Exit)
            {
                rb.isKinematic = false;

                playerController.EnablePlayerController();
            }
        }
        else if (asInfo.shortNameHash == HashManager.animatorHashDict[AnimatorVariables.VaultingState])
        {
            if (animatorState == AnimatorState.Enter)
            {
                rb.isKinematic = true;
                animator.InterruptMatchTarget(false);
            }
            else if (animatorState == AnimatorState.Update)
            {
                if (animator.IsInTransition(0)) return;
                if (animator.isMatchingTarget) return;
                startNormalizedTime = 0.15f;
                targetNormalizedTime = 0.4f;

                Debug.Log("vault matching");
                animator.MatchTarget(matchTargetPositionVaulting, matchTargetRotationVaulting, AvatarTarget.RightFoot, noRotWeightMask, startNormalizedTime, targetNormalizedTime);
            }
            else if (animatorState == AnimatorState.Exit)
            {
                playerController.EnablePlayerController();
                rb.isKinematic = false;
            }
        }
        else if (asInfo.shortNameHash == HashManager.animatorHashDict[AnimatorVariables.LandingState])
        {
            if (animatorState == AnimatorState.Enter)
            {
            }
            else if (animatorState == AnimatorState.Update)
            {

            }
            else if (animatorState == AnimatorState.Exit)
            {
                playerController.EnablePlayerController();
                grabbedLedge = null;
            }
        }
        else if (asInfo.shortNameHash == HashManager.animatorHashDict[AnimatorVariables.HangingBlendState])
        {
            if (animatorState == AnimatorState.Enter)
            {
            }
            else if (animatorState == AnimatorState.Update)
            {
                animator.rootRotation = Quaternion.RotateTowards(animator.rootRotation, matchTargetRotationHanging, Time.fixedDeltaTime * 75);

                if (animator.IsInTransition(0)) return;
                if (animator.isMatchingTarget) return;
                startNormalizedTime = 0;
                targetNormalizedTime = 1;
                animator.MatchTarget(matchTargetPositionHanging, matchTargetRotationHanging, AvatarTarget.RightHand, ledgeWeightMask, startNormalizedTime, targetNormalizedTime);
            }
            else if (animatorState == AnimatorState.Exit)
            {
            }
        }
        else if (asInfo.shortNameHash == HashManager.animatorHashDict[AnimatorVariables.BracedHangHopRightState])
        {
            if (animatorState == AnimatorState.Enter)
            {
                animator.InterruptMatchTarget(false);
                startNormalizedTime = 0.05f;
                targetNormalizedTime = 0.5f;
            }
            else if (animatorState == AnimatorState.Update)
            {
                MatchRotation(matchTargetRotationHoping, asInfo.normalizedTime, startNormalizedTime, targetNormalizedTime);
                MatchRotation(matchTargetRotationHanging, asInfo.normalizedTime, targetNormalizedTime, 1);
                if (animator.IsInTransition(0)) return;
                if (animator.isMatchingTarget) return;
                animator.MatchTarget(matchTargetPositionHoping, matchTargetRotationHoping, AvatarTarget.LeftHand, noRotWeightMask, startNormalizedTime, targetNormalizedTime);

            }
            else if (animatorState == AnimatorState.Exit)
            {
                IsClimbMovementEnabled = true;
            }
        }
        else if (asInfo.shortNameHash == HashManager.animatorHashDict[AnimatorVariables.BracedHangHopLeftState])
        {
            if (animatorState == AnimatorState.Enter)
            {
                animator.InterruptMatchTarget(false);
                startNormalizedTime = 0.05f;
                targetNormalizedTime = 0.5f;
            }
            else if (animatorState == AnimatorState.Update)
            {
                MatchRotation(matchTargetRotationHoping, asInfo.normalizedTime, startNormalizedTime, targetNormalizedTime);
                MatchRotation(matchTargetRotationHanging, asInfo.normalizedTime, targetNormalizedTime, 1);
                if (animator.IsInTransition(0)) return;
                if (animator.isMatchingTarget) return;
                animator.MatchTarget(matchTargetPositionHoping, matchTargetRotationHoping, AvatarTarget.RightHand, noRotWeightMask, startNormalizedTime, targetNormalizedTime);

            }
            else if (animatorState == AnimatorState.Exit)
            {
                IsClimbMovementEnabled = true;
            }
        }
        else if (asInfo.shortNameHash == HashManager.animatorHashDict[AnimatorVariables.BracedHangHopUpState])
        {
            if (animatorState == AnimatorState.Enter)
            {
                animator.InterruptMatchTarget(false);
            }
            else if (animatorState == AnimatorState.Update)
            {
                if (animator.IsInTransition(0)) return;
                if (animator.isMatchingTarget) return;
                startNormalizedTime = 0.1f;
                targetNormalizedTime = 0.5f;
                animator.MatchTarget(matchTargetPositionHoping, matchTargetRotationHoping, AvatarTarget.RightHand, noRotWeightMask, startNormalizedTime, targetNormalizedTime);

            }
            else if (animatorState == AnimatorState.Exit)
            {
                IsClimbMovementEnabled = true;
            }
        }
        else if (asInfo.shortNameHash == HashManager.animatorHashDict[AnimatorVariables.BracedHangHopDownState])
        {
            if (animatorState == AnimatorState.Enter)
            {
                animator.InterruptMatchTarget(false);
            }
            else if (animatorState == AnimatorState.Update)
            {
                if (animator.IsInTransition(0)) return;
                if (animator.isMatchingTarget) return;
                startNormalizedTime = 0.3f;
                targetNormalizedTime = 0.7f;
                animator.MatchTarget(matchTargetPositionHoping, matchTargetRotationHoping, AvatarTarget.RightHand, noRotWeightMask, startNormalizedTime, targetNormalizedTime);

            }
            else if (animatorState == AnimatorState.Exit)
            {
                IsClimbMovementEnabled = true;
            }
        }
    }
    #endregion
}
