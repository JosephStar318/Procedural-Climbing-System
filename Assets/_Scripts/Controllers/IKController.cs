using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Animator), typeof(LedgeDetector))]
public class IKController : MonoBehaviour
{
    [SerializeField] private bool handIKEnabled;
    [SerializeField] private bool footIKEnabled;

    [Header("Hand")]
    [Tooltip("Offset of the ray releative to hands")]
    [SerializeField] private Vector3 handRayOffset;
    [Space]
    [Tooltip("Offset of the final IK position of the right hand")]
    [SerializeField] private Vector3 rightHandIKOffset;
    [Tooltip("Offset of the final IK position of the left hand")]
    [SerializeField] private Vector3 leftHandIKOffset;

    [Header("Foot")]
    [Tooltip("Offset of the ray releative to right foot")]
    [SerializeField] private Vector3 rightFootRayOffset;
    [Tooltip("Offset of the ray releative to left foot")]
    [SerializeField] private Vector3 leftFootRayOffset;
    [Space]
    [Tooltip("Offset of the final IK position of the right foot")]
    [SerializeField] private Vector3 rightFootIKOffset;
    [Tooltip("Offset of the final IK position of the left foot")]
    [SerializeField] private Vector3 leftFootIKOffset;
    [Space]
    [Tooltip("Offset of the feet from the ground while not hanging")]
    [SerializeField] private float groundDistance;


    private Vector3 rightHandRayOrigin = Vector3.zero;
    private Vector3 leftHandRayOrigin = Vector3.zero;
    private Vector3 rightFootRayOrigin = Vector3.zero;
    private Vector3 leftFootRayOrigin = Vector3.zero;

    private Vector3 rightHandPositionIK;
    private Vector3 leftHandPositionIK;
    private Vector3 rightFootPositionIK;
    private Vector3 leftFootPositionIK;

    private float handRayDistance;
    private float rightFootRayDistance;
    private float leftFootRayDistance;

    private Quaternion rightFootRotationIK;
    private Quaternion leftFootRotationIK;

    private RaycastHit rightHandRayHit;
    private RaycastHit leftHandRayHit;
    private RaycastHit leftFootRayHit;
    private RaycastHit rightFootRayHit;

    private Transform rightHand;
    private Transform leftHand;
    private Transform rightFoot;
    private Transform leftFoot;

    private float rightFootIKWeight;
    private float leftFootIKWeight;

    private Animator animator;
    private LedgeDetector ledgeDetector;
    private PlayerController playerController;

    
    private void Start()
    {
        animator = GetComponent<Animator>();
        ledgeDetector = GetComponent<LedgeDetector>();
        playerController = GetComponent<PlayerController>();

        rightHand = animator.GetBoneTransform(HumanBodyBones.RightHand);
        leftHand = animator.GetBoneTransform(HumanBodyBones.LeftHand);
        rightFoot = animator.GetBoneTransform(HumanBodyBones.RightFoot);
        leftFoot = animator.GetBoneTransform(HumanBodyBones.LeftFoot);

        handRayDistance = handRayOffset.magnitude + 0.2f;
        rightFootRayDistance = rightFootRayOffset.magnitude + 0.2f;
        leftFootRayDistance = leftFootRayOffset.magnitude + 0.2f;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.black;
        Gizmos.DrawSphere(rightHandRayOrigin,0.05f);
        Gizmos.DrawSphere(leftHandRayOrigin,0.05f);
        Gizmos.DrawSphere(rightFootRayOrigin,0.05f);
        Gizmos.DrawSphere(leftFootRayOrigin,0.05f);

        if(rightHand != null)
        {
            Gizmos.DrawRay(rightHandRayOrigin, Vector3.down);
            Gizmos.DrawRay(leftHandRayOrigin, Vector3.down);
            Gizmos.DrawRay(rightFootRayOrigin, rightFoot.up);
            Gizmos.DrawRay(leftFootRayOrigin, leftFoot.up);
        }
    }
    private void Update()
    {
        Vector3 localOffsettedHit = transform.InverseTransformPoint(ledgeDetector.ForwardCastHit.point) + handRayOffset;
        Vector3 localright = transform.InverseTransformPoint(rightHand.position);
        Vector3 localleft = transform.InverseTransformPoint(leftHand.position);

        rightHandRayOrigin = transform.TransformPoint(new Vector3(
            localright.x,
            localOffsettedHit.y,
            localOffsettedHit.z
            ));
        leftHandRayOrigin = transform.TransformPoint(new Vector3(
            localleft.x,
            localOffsettedHit.y,
            localOffsettedHit.z
            ));

        rightFootRayOrigin = rightFoot.TransformPoint(rightFootRayOffset);
        leftFootRayOrigin = leftFoot.TransformPoint(leftFootRayOffset);
    }
    private void OnAnimatorIK(int layerIndex)
    {
        if(handIKEnabled && playerController.IsHanging)
        {
            HandIK();
        }

        if (footIKEnabled)
        {
            if (playerController.IsHanging)
                HangingFootIK();
            else
                FootIK();
        }
    }

    /// <summary>
    /// Inverse Kinematics implementaition for hands while hanging
    /// </summary>
    private void HandIK()
    {
        Vector3 localOffsettedHit = transform.InverseTransformPoint(ledgeDetector.ForwardCastHit.point) + handRayOffset;
        Vector3 localright = transform.InverseTransformPoint(rightHand.position);
        Vector3 localleft = transform.InverseTransformPoint(leftHand.position);

        rightHandRayOrigin = transform.TransformPoint(new Vector3(
            localright.x,
            localOffsettedHit.y,
            localOffsettedHit.z
            ));
        leftHandRayOrigin = transform.TransformPoint(new Vector3(
            localleft.x,
            localOffsettedHit.y,
            localOffsettedHit.z
            ));
        
        animator.SetIKPositionWeight(AvatarIKGoal.RightHand, 0);
        animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, 0);

        rightHandPositionIK = animator.GetIKPosition(AvatarIKGoal.RightHand);
        leftHandPositionIK = animator.GetIKPosition(AvatarIKGoal.LeftHand);

        if (Physics.Raycast(rightHandRayOrigin, Vector3.down, out rightHandRayHit, handRayDistance, ledgeDetector.ObstacleLayers))
        {
            Vector3 forwardRightHandOrigin = rightHandRayHit.point + Quaternion.LookRotation(transform.forward) * Vector3.back * 0.5f;
            forwardRightHandOrigin.y -= 0.1f;
            if (Physics.Raycast(forwardRightHandOrigin, transform.forward, out rightHandRayHit, handRayDistance, ledgeDetector.ObstacleLayers))
            {
                rightHandPositionIK = rightHandRayHit.point + Quaternion.LookRotation(transform.forward) * rightHandIKOffset;
                animator.SetIKPosition(AvatarIKGoal.RightHand, rightHandPositionIK);
            }
        }

        if (Physics.Raycast(leftHandRayOrigin, Vector3.down, out leftHandRayHit, handRayDistance, ledgeDetector.ObstacleLayers))
        {
            Vector3 forwardLeftHandOrigin = leftHandRayHit.point + Quaternion.LookRotation(transform.forward) * Vector3.back * 0.5f;
            forwardLeftHandOrigin.y -= 0.1f;
            if (Physics.Raycast(forwardLeftHandOrigin, transform.forward, out leftHandRayHit, handRayDistance, ledgeDetector.ObstacleLayers))
            {
                leftHandPositionIK = leftHandRayHit.point + Quaternion.LookRotation(transform.forward) * leftHandIKOffset;
                animator.SetIKPosition(AvatarIKGoal.LeftHand, leftHandPositionIK);
            }
        }

        animator.SetIKPositionWeight(AvatarIKGoal.RightHand, animator.GetFloat(HashManager.animatorHashDict[AnimatorVariables.RightHandWeight]));
        animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, animator.GetFloat(HashManager.animatorHashDict[AnimatorVariables.LeftHandWeight]));
    }

    /// <summary>
    /// Inverse Kinematics implementaition for feet while hanging
    /// </summary>
    private void HangingFootIK()
    {
        animator.SetIKPositionWeight(AvatarIKGoal.RightFoot, 0);
        animator.SetIKPositionWeight(AvatarIKGoal.LeftFoot, 0);

        rightFootRayOrigin = rightFoot.TransformPoint(rightFootRayOffset);
        leftFootRayOrigin = leftFoot.TransformPoint(leftFootRayOffset);

        rightFootPositionIK = animator.GetIKPosition(AvatarIKGoal.RightFoot);
        leftFootPositionIK = animator.GetIKPosition(AvatarIKGoal.LeftFoot);

        if (Physics.Raycast(rightFootRayOrigin,rightFoot.up, out rightFootRayHit, rightFootRayDistance, ledgeDetector.ObstacleLayers))
        {
            rightFootPositionIK = rightFootRayHit.point + Quaternion.LookRotation(rightFoot.up) * rightFootIKOffset;
        }
        if(Physics.Raycast(leftFootRayOrigin, leftFoot.up, out leftFootRayHit, leftFootRayDistance, ledgeDetector.ObstacleLayers))
        {
            leftFootPositionIK = leftFootRayHit.point + Quaternion.LookRotation(leftFoot.up) * leftFootIKOffset;
        }

        animator.SetIKPosition(AvatarIKGoal.RightFoot, rightFootPositionIK);
        animator.SetIKPosition(AvatarIKGoal.LeftFoot, leftFootPositionIK);

        animator.SetIKPositionWeight(AvatarIKGoal.RightFoot, 1);
        animator.SetIKPositionWeight(AvatarIKGoal.LeftFoot, 1);
    }

    /// <summary>
    /// Inverse Kinematics implementaition for feet while not hanging
    /// </summary>
    private void FootIK()
    {
        rightFootRayOrigin = rightFoot.TransformPoint(rightFootRayOffset);
        leftFootRayOrigin = leftFoot.TransformPoint(leftFootRayOffset);

        rightFootPositionIK = rightFoot.position;
        leftFootPositionIK = leftFoot.position;
        rightFootRotationIK = rightFoot.rotation;
        leftFootRotationIK = leftFoot.rotation;

        if (Physics.Raycast(rightFoot.position + Vector3.up, Vector3.down, out rightFootRayHit, 1.3f, ledgeDetector.ObstacleLayers))
        {
            rightFootPositionIK = rightFootRayHit.point;
            rightFootPositionIK.y += groundDistance;
            Vector3 forward = Vector3.ProjectOnPlane(transform.forward, rightFootRayHit.normal);
            rightFootRotationIK = Quaternion.LookRotation(forward);
            
            animator.SetIKPosition(AvatarIKGoal.RightFoot, rightFootPositionIK);
            animator.SetIKRotation(AvatarIKGoal.RightFoot, rightFootRotationIK);
        }
        if (Physics.Raycast(leftFoot.position + Vector3.up, Vector3.down, out leftFootRayHit, 1.3f, ledgeDetector.ObstacleLayers))
        {
            leftFootPositionIK = leftFootRayHit.point;
            leftFootPositionIK.y += groundDistance;
            Vector3 forward = Vector3.ProjectOnPlane(transform.forward, leftFootRayHit.normal);
            leftFootRotationIK = Quaternion.LookRotation(forward);
            animator.SetIKPosition(AvatarIKGoal.LeftFoot, leftFootPositionIK);
            animator.SetIKRotation(AvatarIKGoal.LeftFoot, leftFootRotationIK);
        }

        animator.SetIKPositionWeight(AvatarIKGoal.RightFoot, animator.GetFloat(HashManager.animatorHashDict[AnimatorVariables.RightFootIKWeight]));
        animator.SetIKPositionWeight(AvatarIKGoal.LeftFoot, animator.GetFloat(HashManager.animatorHashDict[AnimatorVariables.LeftFootIKWeight]));

        animator.SetIKRotationWeight(AvatarIKGoal.RightFoot, animator.GetFloat(HashManager.animatorHashDict[AnimatorVariables.RightFootIKWeight]));
        animator.SetIKRotationWeight(AvatarIKGoal.LeftFoot, animator.GetFloat(HashManager.animatorHashDict[AnimatorVariables.LeftFootIKWeight]));
    }
}
