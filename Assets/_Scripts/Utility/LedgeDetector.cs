using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(PlayerController))]
public class LedgeDetector : MonoBehaviour
{
    private CapsuleCollider capsuleCollider;

    [Header("Ledge Settings")]
    [SerializeField] private LayerMask climbableLayers;
    [SerializeField] private LayerMask obstacleLayers;
    [Space]
    [SerializeField] private float wallAngleMax = 45f;
    [SerializeField] private float groundAngleMax = 45f;
    [SerializeField] private float minStepHeight = 0;
    [SerializeField] float maxCornerAngle = 90;
    [SerializeField] float minCornerAngle = 25;
    [Space]
    [SerializeField] private Vector3 climbOriginDown = new Vector3(0, 2, 0.75f);
    [SerializeField] private Vector3 endOffset = new Vector3(0,0.1f,0);
    [SerializeField] private Vector3 bracedRightHandOffset = new Vector3(0, 0.03f, -0.04f);
    [SerializeField] private Vector3 bracedLeftHandOffset = new Vector3(0, 0.03f, -0.04f);
    [SerializeField] private Vector3 freeRightHandOffset = new Vector3(0, 0.05f, 0.05f);
    [SerializeField] private Vector3 freeLeftHandOffset = new Vector3(0, 0.05f, 0.05f);
    [SerializeField] private float penetrationOffset = 1.1f;
    [Space]
    [SerializeField] private Vector3 leftSideRayOffset;
    [SerializeField] private Vector3 rightSideRayOffset;
    [SerializeField] private Vector3 braceDetectionRayOffset;
    [Space]
    [SerializeField] private Transform jumpCheckerTransform;

    private Vector3 leftSideLedgeRayOrigin;
    private Vector3 rightSideLedgeRayOrigin;
    private Vector3 bracedRayOrigin;

    private Vector3 rightCornerRayOrigin;
    private Vector3 leftCornerRayOrigin;
    private float maxSideHitDistance = 1f;
    private bool isBraced = false;

    private RaycastHit downCastHit;
    private RaycastHit forwardCastHit;
    private RaycastHit sideCastHit;
    private RaycastHit bracedCastHit;

    private Vector3 endPosition;
    private Quaternion forwardNormalXZRotation;

    private List<GameObject> anchorList = new List<GameObject>();
    public bool IsBraced { get => isBraced; set => isBraced = value; }
    public Vector3 ForwardCastHitPoint { get; private set; }
    public Vector3 ForwardCastHitNormal { get; private set; }
    public Vector3 DownCastHitPoint { get; private set; }
    public Vector3 DownCastHitNormal { get; private set; }
    public LayerMask ObstacleLayers { get => obstacleLayers; set => obstacleLayers = value; }
    public LayerMask ClimbableLayers { get => climbableLayers; set => climbableLayers = value; }

    private void Start()
    {
        capsuleCollider = GetComponent<CapsuleCollider>();
        jumpCheckerTransform.position = transform.position;
    }
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawSphere(leftSideLedgeRayOrigin, 0.01f);
        Gizmos.DrawSphere(rightSideLedgeRayOrigin, 0.01f);
        Gizmos.color = Color.magenta;
        Gizmos.DrawSphere(sideCastHit.point, 0.1f);

        Gizmos.color = Color.blue;
        Gizmos.DrawSphere(endPosition, 0.1f);
      
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(forwardCastHit.point, 0.1f);
        Gizmos.DrawSphere(leftCornerRayOrigin, 0.1f);

        Gizmos.color = Color.black;
        Gizmos.DrawSphere(jumpCheckerTransform.position, 0.1f);

        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(bracedRayOrigin, 0.2f);
        Gizmos.DrawSphere(downCastHit.point, 0.1f);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Anchor"))
        {
            if(anchorList.Contains(other.gameObject) == false)
            {
                anchorList.Add(other.gameObject);
            }
        }
    }
    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.CompareTag("Anchor"))
        {
            if (anchorList.Contains(other.gameObject) == true)
            {
                anchorList.Remove(other.gameObject);
            }
        }
    }

    /// <summary>
    /// Detects the if the ledge is climbable
    /// </summary>
    /// <returns>true if ledge is detected</returns>
    public bool CanClimb(Transform originTransform)
    {
        bool downHit;
        bool forwardHit;
        float groundAngle;
        float wallAngle;

        Vector3 forwardDirectionXZ;
        Vector3 forwardNormalXZ;

        Vector3 downDirection = Vector3.down;
        Vector3 downOrigin = originTransform.TransformPoint(climbOriginDown);

        Debug.DrawLine(downOrigin, downOrigin + Vector3.down, Color.red);

        downHit = Physics.SphereCast(downOrigin, 0.2f, downDirection, out downCastHit, climbOriginDown.y - minStepHeight, ClimbableLayers);

        if (downHit)
        {
            DownCastHitPoint = downCastHit.point;
            DownCastHitNormal = downCastHit.normal;
            Vector3 forwardOrigin = new Vector3(originTransform.position.x, downCastHit.point.y - 0.1f, originTransform.position.z) + Quaternion.LookRotation(originTransform.forward) * Vector3.back * 0.5f;
            Debug.DrawLine(forwardOrigin, forwardOrigin + originTransform.forward, Color.red);

            forwardDirectionXZ = Vector3.ProjectOnPlane(originTransform.forward, Vector3.up);
            forwardHit = Physics.Raycast(forwardOrigin, forwardDirectionXZ, out forwardCastHit, 1f, ObstacleLayers);
            if (forwardHit)
            {
                ForwardCastHitPoint = forwardCastHit.point;
                ForwardCastHitNormal = forwardCastHit.normal;
                forwardNormalXZ = Vector3.ProjectOnPlane(forwardCastHit.normal, Vector3.up);
                groundAngle = Vector3.Angle(downCastHit.normal, Vector3.up);
                wallAngle = Vector3.Angle(-forwardNormalXZ, forwardDirectionXZ);

                if (wallAngle <= wallAngleMax)
                {
                    if (groundAngle <= groundAngleMax)
                    {
                        Vector3 vectSurface = Vector3.ProjectOnPlane(forwardDirectionXZ, downCastHit.normal);
                        endPosition = downCastHit.point + Quaternion.LookRotation(vectSurface, Vector3.up) * endOffset;

                        Collider colliderLedge = downCastHit.collider;
                        bool penetrationOverlap = Physics.ComputePenetration(
                            colliderA: capsuleCollider,
                            positionA: endPosition,
                            rotationA: originTransform.rotation,
                            colliderB: colliderLedge,
                            positionB: colliderLedge.transform.position,
                            rotationB: colliderLedge.transform.rotation,
                            direction: out Vector3 penetrationDirection,
                            distance: out float penetrationDistance
                            );
                        if (penetrationOverlap)
                            endPosition += penetrationDirection * (penetrationDistance + penetrationOffset);
                        return true;
                    }
                }
            }
        }
        return false;
    }

    /// <summary>
    /// Detects if the ledge can be climb over
    /// </summary>
    /// <returns>true if there is enough space over the ledge</returns>
    public bool CanClimbOver()
    {
        float inflate = -0.05f;
        float upsweepDistance = downCastHit.point.y - transform.position.y;
        Vector3 upSweepDirection = transform.up;
        Vector3 upSweepOrigin = transform.position - transform.forward;
        bool upSweepHit = CharacterSweep(
            position: upSweepOrigin,
            rotation: transform.rotation,
            direction: upSweepDirection,
            distance: upsweepDistance,
            layerMask: ObstacleLayers,
            inflate: inflate
            );

        Vector3 forwardSweepOrigin = upSweepOrigin + upSweepDirection * upsweepDistance;
        Vector3 forwardSweepVector = endPosition - forwardSweepOrigin;
        bool forwardSweepHit = CharacterSweep(
                position: forwardSweepOrigin,
                rotation: transform.rotation,
                direction: forwardSweepVector.normalized,
                distance: forwardSweepVector.magnitude,
                layerMask: ObstacleLayers,
                inflate: inflate
            );
        if (!upSweepHit && !forwardSweepHit)
        {
            return true;
        }
        return false;
    }

    /// <summary>
    /// Checks the route to the climbover end point
    /// </summary>
    /// <param name="position">Start position of the sweep</param>
    /// <param name="rotation">Rotation of the sweep</param>
    /// <param name="direction">Sweep direction</param>
    /// <param name="distance">Sweep distance</param>
    /// <param name="layerMask">Obstacle layers</param>
    /// <param name="inflate">Inflation</param>
    /// <returns>false if there is obstacle in the way</returns>
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

    /// <summary>
    /// Checks if the ledge has finished in the move direction
    /// </summary>
    /// <param name="moveDir">Moving direction of the player. -1 left, 1 right</param>
    /// <param name="cornerAngle">Angle of the corner ledge</param>
    /// <param name="sideHit">Player's holding point of the ledge</param>
    /// <returns>false if there is no ledge to continue</returns>
    public bool CheckLedgeInMoveDirection(float moveDir, out bool isCorner, out RaycastHit sideHit)
    {
        bool isRightSideHit = false;
        bool isLeftSideHit = false;
        float cornerAngle = 0;
        sideHit = sideCastHit;
        isCorner = false;
        
        leftCornerRayOrigin = forwardCastHit.point + Quaternion.LookRotation(transform.right) * (leftSideRayOffset + new Vector3(-0.3f,0,0));
        rightCornerRayOrigin = forwardCastHit.point + Quaternion.LookRotation(transform.right) * (rightSideRayOffset + new Vector3(-0.3f, 0, 0));
        leftSideLedgeRayOrigin = forwardCastHit.point + Quaternion.LookRotation(transform.right) * leftSideRayOffset;
        rightSideLedgeRayOrigin = forwardCastHit.point + Quaternion.LookRotation(transform.right) * rightSideRayOffset;


        if(moveDir > 0.5f)
        {
            isRightSideHit = Physics.Raycast(rightSideLedgeRayOrigin, -transform.right, out sideCastHit, maxSideHitDistance, ClimbableLayers);
            if (isRightSideHit)
            {
                //if there is a hit but if some other platform colliding to near edge 
                if(Physics.OverlapSphere(rightSideLedgeRayOrigin, 0.01f, ObstacleLayers).Length != 0)
                {
                    return true;
                }
                else
                {
                    //corner check
                    if (Physics.Raycast(rightCornerRayOrigin, -transform.right, out sideCastHit, maxSideHitDistance, ObstacleLayers))
                    {
                        cornerAngle = Vector3.Angle(forwardCastHit.normal, sideCastHit.normal);
                        if (minCornerAngle <= cornerAngle && cornerAngle <= maxCornerAngle)
                        {
                            isCorner = true;
                            sideHit = sideCastHit;
                            return false;
                        }
                        else if (cornerAngle < minCornerAngle)
                        {
                            return true;
                        }
                    }
                    else if (Physics.OverlapSphere(rightCornerRayOrigin, 0.2f, ClimbableLayers).Length != 0)
                    {
                        //if there is any collision near corner it means player is moving at the edge of a circle
                        return true;
                    }
                }
            }
            else
            {
                return true;
            }
        }
        else if(moveDir < -0.5f)
        {
            isLeftSideHit = Physics.Raycast(leftSideLedgeRayOrigin, transform.right, out sideCastHit, maxSideHitDistance, ClimbableLayers);
            if (isLeftSideHit)
            {
                //if there is a hit but if some other platform colliding to near edge 
                if (Physics.OverlapSphere(leftSideLedgeRayOrigin, 0.01f, ObstacleLayers).Length != 0)
                {
                    return true;
                }
                else
                {
                    //corner check
                    if (Physics.Raycast(leftCornerRayOrigin, transform.right, out sideCastHit, maxSideHitDistance, ObstacleLayers))
                    {
                        cornerAngle = Vector3.Angle(forwardCastHit.normal, sideCastHit.normal);
                        Debug.Log(cornerAngle);
                        if (minCornerAngle <= cornerAngle && cornerAngle <= maxCornerAngle)
                        {
                            isCorner = true;
                            sideHit = sideCastHit;
                            return false;
                        }
                        else if(cornerAngle < minCornerAngle)
                        {
                            return true;
                        }
                    }
                    else if (Physics.OverlapSphere(leftCornerRayOrigin, 0.2f, ClimbableLayers).Length != 0)
                    {
                        //if there is any collision near corner it means player is moving at the edge of a circle
                        return true;
                    }
                }
            }
            else
            {
                return true;
            }
        }
        sideHit = sideCastHit;

        if (moveDir == 0)
            return false;

        return false;
    }

    /// <summary>
    /// Checks the for ledge þn front underneath
    /// </summary>
    /// <returns>true if there is a ledge</returns>
    public bool CheckDropLedge()
    {
        Vector3 dropLedgeOrigin = transform.position + transform.forward - transform.up;

        if(Physics.OverlapSphere(dropLedgeOrigin, 0.1f).Length == 0)
        {
            GameObject obj = new GameObject();
            obj.transform.position = dropLedgeOrigin;
            obj.transform.rotation = Quaternion.LookRotation(-transform.forward);

            if(CanClimb(obj.transform))
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        else
        {
            return false;
        }
    }

    /// <summary>
    /// Checks ledges in the moveVector direction
    /// </summary>
    /// <param name="moveVector">move direction</param>
    /// <param name="targetPos">If there is a ledge the match target position of the ledge</param>
    /// <param name="targetRot">If there is a ledge the match target rotation of the ledge</param>
    /// <param name="avatarTarget">Avatar target that is used to determine the target position</param>
    /// <returns>true if there is a ledge</returns>
    public bool CheckJumpableLedgeInMoveDirection(Vector2 moveVector, out Vector3 targetPos, out Quaternion targetRot, AvatarTarget avatarTarget)
    {
        targetPos = Vector3.zero;
        targetRot = Quaternion.identity;

        if (moveVector.magnitude == 0) return false;

        Vector3 moveVector3d = Quaternion.LookRotation(transform.forward) * new Vector3(moveVector.x, moveVector.y, 0);

        foreach (GameObject anchors in anchorList)
        {
            //skip the closest ones
            float dist = Vector3.Distance(anchors.transform.position, ForwardCastHitPoint);
            if (dist < 0.5f) continue;

            float angle = Vector3.Angle(moveVector3d, (anchors.transform.position - ForwardCastHitPoint));
            if(Mathf.Abs(angle) < 10)
            {
                if(avatarTarget == AvatarTarget.LeftHand)
                    targetPos = anchors.transform.position + Quaternion.LookRotation(-anchors.transform.forward) * (IsBraced ? bracedRightHandOffset : freeRightHandOffset);
                else
                    targetPos = anchors.transform.position + Quaternion.LookRotation(-anchors.transform.forward) * (IsBraced ? bracedLeftHandOffset : freeLeftHandOffset);

                targetRot = anchors.transform.rotation;
                return true;
            }
        }
        return false;
    }


    /// <summary>
    /// WIP
    /// </summary>
    /// <returns></returns>
    public bool CheckLedgeInLookDirection()
    {
        return true;
    }

    /// <summary>
    /// Checks if there is a brace to lean upon
    /// </summary>
    /// <returns>true is it is</returns>
    public bool IsLedgeBraced()
    {
        bracedRayOrigin = ForwardCastHitPoint + forwardNormalXZRotation * braceDetectionRayOffset;
        if(Physics.SphereCast(bracedRayOrigin, 0.2f, -ForwardCastHitNormal, out bracedCastHit, 1f, ObstacleLayers))
        {
            IsBraced = true;
            return true;
        }
        else
        {
            IsBraced = false;
            return false;
        }
    }

    /// <summary>
    /// Sets target matching position and rotation for ledge
    /// </summary>
    /// <param name="pos">Matched position</param>
    /// <param name="rot">Matched rotation</param>
    public void SetTargetMatchingToLedge(out Vector3 pos, out Quaternion rot, AvatarTarget avatarTarget)
    {
        if (avatarTarget == AvatarTarget.RightHand)
        {
            Vector3 forwardNormalXZ = Vector3.ProjectOnPlane(ForwardCastHitNormal, Vector3.up);
            forwardNormalXZRotation = Quaternion.LookRotation(-forwardNormalXZ, Vector3.up);
            pos = ForwardCastHitPoint + forwardNormalXZRotation * (IsBraced ? bracedRightHandOffset : freeRightHandOffset);
            rot = forwardNormalXZRotation;
        }
        else
        {
            Vector3 forwardNormalXZ = Vector3.ProjectOnPlane(ForwardCastHitNormal, Vector3.up);
            forwardNormalXZRotation = Quaternion.LookRotation(-forwardNormalXZ, Vector3.up);
            pos = ForwardCastHitPoint + forwardNormalXZRotation * (IsBraced ? bracedLeftHandOffset : freeLeftHandOffset);
            rot = forwardNormalXZRotation;
        }
       
    }

    /// <summary>
    /// Sets target matching position and rotation for hitpoint
    /// </summary>
    /// <param name="hit">Hit point</param>
    /// <param name="pos">Matched position</param>
    /// <param name="rot">Matched rotation</param>
    public void SetTargetMatchingToHitPoint(RaycastHit hit, out Vector3 pos, out Quaternion rot, AvatarTarget avatarTarget)
    {
        if (avatarTarget == AvatarTarget.RightHand)
        {
            Vector3 forwardNormalXZ = Vector3.ProjectOnPlane(hit.normal, Vector3.up);
            forwardNormalXZRotation = Quaternion.LookRotation(-forwardNormalXZ, Vector3.up);
            pos = hit.point + forwardNormalXZRotation * (IsBraced ? bracedRightHandOffset : freeRightHandOffset);
            rot = forwardNormalXZRotation;
        }
        else
        {
            Vector3 forwardNormalXZ = Vector3.ProjectOnPlane(hit.normal, Vector3.up);
            forwardNormalXZRotation = Quaternion.LookRotation(-forwardNormalXZ, Vector3.up);
            pos = hit.point + forwardNormalXZRotation* (IsBraced? bracedLeftHandOffset : freeLeftHandOffset);
            rot = forwardNormalXZRotation;
        }
    }
        

    /// <summary>
    /// Sets target matching position and rotation for endpoint
    /// </summary>
    /// <param name="pos">Matched position</param>
    /// <param name="rot">Matched rotation</param>
    public void SetTargetMatchingToEndpoint(out Vector3 pos, out Quaternion rot)
    {
        pos = endPosition;
        rot = forwardNormalXZRotation;
    }

    /// <summary>
    /// </summary>
    /// <returns>downCastHit</returns>
    public RaycastHit ReturnDownCastHit()
    {
        return downCastHit;
    }

}
