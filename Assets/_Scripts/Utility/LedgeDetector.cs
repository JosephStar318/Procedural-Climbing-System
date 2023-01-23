using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(PlayerController))]
public class LedgeDetector : MonoBehaviour
{
    private PlayerController playerController;
    private CapsuleCollider capsuleCollider;

    [Header("Ledge Settings")]
    [SerializeField] private LayerMask climbableLayers;
    [SerializeField] private LayerMask obstacleLayers;
    [SerializeField] private float wallAngleMax = 45f;
    [SerializeField] private float groundAngleMax = 45f;
    [SerializeField] private float minStepHeight = 0;

    [SerializeField] private Vector3 climbOriginDown = new Vector3(0, 2, 0.75f);
    [SerializeField] private Vector3 endOffset = new Vector3(0,0.1f,0);
    [SerializeField] private Vector3 handOffset = new Vector3(0, 0, 0);
    [SerializeField] private float penetrationOffset = 1.1f;

    [SerializeField] float maxCornerAngle = 90;
    [SerializeField] float minCornerAngle = 25;
    [SerializeField] private Vector3 leftSideRayOffset;
    [SerializeField] private Vector3 rightSideRayOffset;

    [SerializeField] private Transform jumpCheckerTransform;

    Vector3 leftSideLedgeRayOrigin;
    Vector3 rightSideLedgeRayOrigin;

    Vector3 rightCornerRayOrigin;
    Vector3 leftCornerRayOrigin;
    float maxSideHitDistance = 1f;

    Vector2 previousMoveVector;

    private RaycastHit downCastHit;
    private RaycastHit forwardCastHit;
    private RaycastHit sideCastHit;

    private Vector3 endPosition;
    private Quaternion forwardNormalXZRotation;

    private Animator animator;
    private Rigidbody rb;
    private void Start()
    {
        playerController = GetComponent<PlayerController>();
        capsuleCollider = GetComponent<CapsuleCollider>();
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody>();

        jumpCheckerTransform.position = transform.position;
    }
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawSphere(leftSideLedgeRayOrigin, 0.1f);
        Gizmos.DrawSphere(rightSideLedgeRayOrigin, 0.1f);
        Gizmos.color = Color.magenta;
        Gizmos.DrawSphere(sideCastHit.point, 0.1f);

        Gizmos.color = Color.blue;
        Gizmos.DrawSphere(endPosition, 0.1f);
        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(downCastHit.point, 0.1f);
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(forwardCastHit.point, 0.1f);
        Gizmos.DrawSphere(leftCornerRayOrigin, 0.1f);

        Gizmos.color = Color.black;
        Gizmos.DrawSphere(jumpCheckerTransform.position, 0.1f);
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

        downHit = Physics.SphereCast(downOrigin, 0.15f, downDirection, out downCastHit, climbOriginDown.y - minStepHeight, climbableLayers);

        if (downHit)
        {
            float forwardDistance = climbOriginDown.z;
            Vector3 forwardOrigin = new Vector3(originTransform.position.x, downCastHit.point.y - 0.1f, originTransform.position.z);
            Debug.DrawLine(forwardOrigin, forwardOrigin + originTransform.forward, Color.red);

            forwardDirectionXZ = Vector3.ProjectOnPlane(originTransform.forward, Vector3.up);
            forwardHit = Physics.Raycast(forwardOrigin, forwardDirectionXZ, out forwardCastHit, forwardDistance, climbableLayers);
            if (forwardHit)
            {
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
        Vector3 upSweepOrigin = transform.position;
        bool upSweepHit = CharacterSweep(
            position: upSweepOrigin,
            rotation: transform.rotation,
            direction: upSweepDirection,
            distance: upsweepDistance,
            layerMask: obstacleLayers,
            inflate: inflate
            );

        Vector3 forwardSweepOrigin = transform.position + upSweepDirection * upsweepDistance;
        Vector3 forwardSweepVector = endPosition - forwardSweepOrigin;
        bool forwardSweepHit = CharacterSweep(
                position: forwardSweepOrigin,
                rotation: transform.rotation,
                direction: forwardSweepVector.normalized,
                distance: forwardSweepVector.magnitude,
                layerMask: obstacleLayers,
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
    public bool CheckLedgeInMoveDirection(float moveDir, out bool isCorner, out float cornerAngle, out RaycastHit sideHit)
    {
        bool isRightSideHit = false;
        bool isLeftSideHit = false;
        cornerAngle = 0;
        sideHit = sideCastHit;
        isCorner = false;

        leftCornerRayOrigin = transform.TransformPoint(leftSideRayOffset + new Vector3(0, 0, 0.3f));
        rightCornerRayOrigin = transform.TransformPoint(rightSideRayOffset + new Vector3(0, 0, 0.3f));
        leftSideLedgeRayOrigin = transform.TransformPoint(leftSideRayOffset);
        rightSideLedgeRayOrigin = transform.TransformPoint(rightSideRayOffset);


        if(moveDir == 1)
        {
            isRightSideHit = Physics.Raycast(rightSideLedgeRayOrigin, -transform.right, out sideCastHit, maxSideHitDistance, climbableLayers);
            if (isRightSideHit)
            {
                //if there is a hit but if some other platform colliding to near edge 
                if(Physics.OverlapSphere(rightSideLedgeRayOrigin, 0.1f, climbableLayers).Length != 0)
                {
                    return true;
                }
                else
                {
                    //corner check
                    if (Physics.Raycast(rightCornerRayOrigin, -transform.right, out sideCastHit, maxSideHitDistance, obstacleLayers))
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
                    else if (Physics.OverlapSphere(rightCornerRayOrigin, 0.2f, climbableLayers).Length != 0)
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
        else if(moveDir == -1)
        {
            isLeftSideHit = Physics.Raycast(leftSideLedgeRayOrigin, transform.right, out sideCastHit, maxSideHitDistance, climbableLayers);
            if (isLeftSideHit)
            {
                //if there is a hit but if some other platform colliding to near edge 
                if (Physics.OverlapSphere(leftSideLedgeRayOrigin, 0.1f, climbableLayers).Length != 0)
                {
                    return true;
                }
                else
                {
                    //corner check
                    if (Physics.Raycast(leftCornerRayOrigin, transform.right, out sideCastHit, maxSideHitDistance, obstacleLayers))
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
                    else if (Physics.OverlapSphere(leftCornerRayOrigin, 0.2f, climbableLayers).Length != 0)
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


    public bool CheckJumpableLedgeInMoveDirection(Vector2 moveVector, float areaDiameter)
    {
        if(previousMoveVector == moveVector)
        {
            jumpCheckerTransform.position = Vector3.Lerp(
                jumpCheckerTransform.position,
                transform.TransformPoint(areaDiameter * new Vector3(moveVector.x, moveVector.y, 0)),
                Time.fixedDeltaTime * 2
                );
        }
        else
        {
            jumpCheckerTransform.position = transform.position;
        }
        previousMoveVector = moveVector;


        if (CanClimb(jumpCheckerTransform) == true)
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    public bool CheckLedgeInLookDirection()
    {
        return true;
    }

    /// <summary>
    /// Sets target matching position and rotation for ledge
    /// </summary>
    /// <param name="pos">Matched position</param>
    /// <param name="rot">Matched rotation</param>
    public void SetTargetMatchingToLedge(out Vector3 pos, out Quaternion rot)
    {
        Vector3 forwardNormalXZ = Vector3.ProjectOnPlane(forwardCastHit.normal, Vector3.up);
        forwardNormalXZRotation = Quaternion.LookRotation(-forwardNormalXZ, Vector3.up);
        pos = forwardCastHit.point + forwardNormalXZRotation * handOffset;
        rot = forwardNormalXZRotation;
    }

    /// <summary>
    /// Sets target matching position and rotation for hitpoint
    /// </summary>
    /// <param name="hit">Hit point</param>
    /// <param name="pos">Matched position</param>
    /// <param name="rot">Matched rotation</param>
    public void SetTargetMatchingToHitPoint(RaycastHit hit, out Vector3 pos, out Quaternion rot)
    {
        Vector3 forwardNormalXZ = Vector3.ProjectOnPlane(hit.normal, Vector3.up);
        forwardNormalXZRotation = Quaternion.LookRotation(-forwardNormalXZ, Vector3.up);
        pos = hit.point + forwardNormalXZRotation * handOffset;
        rot = forwardNormalXZRotation;
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
