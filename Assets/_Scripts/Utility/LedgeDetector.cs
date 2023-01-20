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
    [SerializeField] private float wallAngleMax = 45f;
    [SerializeField] private float groundAngleMax = 45f;
    [SerializeField] private float minStepHeight = 0;

    [SerializeField] private Vector3 climbOriginDown = new Vector3(0, 2, 0.75f);
    [SerializeField] private Vector3 endOffset = new Vector3(0,0.1f,0);
    [SerializeField] private Vector3 hangOffset = new Vector3(0, -1.75f, -0.3f);
    [SerializeField] private float penetrationOffset = 1.1f;

    [SerializeField] float maxCornerAngle = 20;
    [SerializeField] private Vector3 leftSideRayOffset;
    [SerializeField] private Vector3 rightSideRayOffset;

    Vector3 leftSideLedgeRayOrigin;
    Vector3 rightSideLedgeRayOrigin;

    private RaycastHit downCastHit;
    private RaycastHit forwardCastHit;
    private RaycastHit sideCastHit;

    private Vector3 endPosition;
    private Quaternion forwardNormalXZRotation;

    public List<GameObject> ledgeList = new List<GameObject>();

    private void Start()
    {
        playerController = GetComponent<PlayerController>();
        capsuleCollider = GetComponent<CapsuleCollider>();
    }
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawSphere(leftSideLedgeRayOrigin, 0.1f);
        Gizmos.DrawSphere(rightSideLedgeRayOrigin, 0.1f);

        Gizmos.color = Color.blue;
        Gizmos.DrawSphere(endPosition, 0.1f);
        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(downCastHit.point, 0.1f);
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(forwardCastHit.point, 0.1f);
    }
    private void FixedUpdate()
    {
        if (GlobalSettings.Instance.debugMode == true)
        {
            //Debug.DrawRay(headRay.position, headRayDirection, Color.red);
            //Debug.DrawRay(sideRay.position, sideRayDirection, Color.red);
            //Debug.DrawLine(headRay.position, ledgeGrabbingPoint, Color.blue);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if(other.gameObject.layer == climbableLayers)
        {
            if(ledgeList.Contains(other.gameObject) == false)
            {
                if(GlobalSettings.Instance.debugMode) Debug.Log("Ledge Detected and added to the list.");
                ledgeList.Add(other.gameObject);
            }
        }
    }
    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.layer == climbableLayers)
        {
            if (ledgeList.Contains(other.gameObject) == true)
            {
                if (GlobalSettings.Instance.debugMode) Debug.Log("Ledge removed from the list.");
                ledgeList.Remove(other.gameObject);
            }
        }
    }

    /// <summary>
    /// Detects the if the ledge is climbable
    /// </summary>
    /// <returns>true if ledge is detected</returns>
    public bool CanClimb()
    {
        bool downHit;
        bool forwardHit;
        float groundAngle;
        float wallAngle;

        Vector3 forwardDirectionXZ;
        Vector3 forwardNormalXZ;

        Vector3 downDirection = Vector3.down;
        Vector3 downOrigin = transform.TransformPoint(climbOriginDown);

        Debug.DrawLine(downOrigin, downOrigin + Vector3.down, Color.red);

        downHit = Physics.Raycast(downOrigin, downDirection, out downCastHit, climbOriginDown.y - minStepHeight, climbableLayers);

        if (downHit)
        {
            float forwardDistance = climbOriginDown.z;
            Vector3 forwardOrigin = new Vector3(transform.position.x, downCastHit.point.y - 0.1f, transform.position.z);
            Debug.DrawLine(forwardOrigin, forwardOrigin + transform.forward, Color.red);

            forwardDirectionXZ = Vector3.ProjectOnPlane(transform.forward, Vector3.up);
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
                            rotationA: transform.rotation,
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
    public bool CheckLedgeInMoveDirection(float moveDir, out float cornerAngle, out RaycastHit sideHit)
    {
        bool isRightSideHit = false;
        bool isLeftSideHit = false;
        cornerAngle = 0;
        sideHit = sideCastHit;

        leftSideLedgeRayOrigin = transform.TransformPoint(transform.InverseTransformPoint(forwardCastHit.point) + leftSideRayOffset);
        rightSideLedgeRayOrigin = transform.TransformPoint(transform.InverseTransformPoint(forwardCastHit.point) + rightSideRayOffset);

        if(moveDir == 1)
        {
            isRightSideHit = Physics.Raycast(rightSideLedgeRayOrigin, -transform.right, out sideCastHit, 0.5f, climbableLayers);
            if (isRightSideHit)
            {
                cornerAngle = Vector3.Angle(forwardCastHit.normal, sideCastHit.normal);
                if (cornerAngle < maxCornerAngle)
                {
                    return true;
                }
                else if(Physics.OverlapSphere(rightSideLedgeRayOrigin, 0.1f,climbableLayers).Length != 0)
                {
                    return true;
                }
            }
        }
        else if(moveDir == -1)
        {
            isLeftSideHit = Physics.Raycast(leftSideLedgeRayOrigin, transform.right, out sideCastHit, 0.5f, climbableLayers);
            if (isLeftSideHit)
            {
                cornerAngle = Vector3.Angle(forwardCastHit.normal, sideCastHit.normal);
                if (cornerAngle < maxCornerAngle)
                {
                    return true;
                }
                else if (Physics.OverlapSphere(leftSideLedgeRayOrigin, 0.1f, climbableLayers).Length != 0)
                {
                    return true;
                }
            }
        }
        if(moveDir == -1 && !isLeftSideHit)
        {
            return true;
        }
        else if (moveDir == 1 && !isRightSideHit)
        {
            return true;
        }

        return false;
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
        pos = hit.point + forwardNormalXZRotation * hangOffset;
        rot = forwardNormalXZRotation;
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
        pos = forwardCastHit.point + forwardNormalXZRotation * hangOffset;
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

    public bool CheckLedgeInLookDirection()
    {
        return true;
    }
}
