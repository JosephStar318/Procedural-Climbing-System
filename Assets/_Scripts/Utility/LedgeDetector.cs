using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(PlayerController))]
public class LedgeDetector : MonoBehaviour
{
    [SerializeField] private LayerMask climbableLayers;
    [SerializeField] private Vector3 ledgeOffset;

    private PlayerController playerController;

    [SerializeField] float maxAngleBetweenLedges = 30;
    [SerializeField] private Vector3 leftSideRayOffset;
    [SerializeField] private Vector3 rightSideRayOffset;

    Vector3 leftSideLedgeRayOrigin;
    Vector3 rightSideLedgeRayOrigin;
    RaycastHit leftSideHit;
    RaycastHit rightSideHit;


    public List<GameObject> ledgeList = new List<GameObject>();

    private void Start()
    {
        playerController = GetComponent<PlayerController>();
        
    }
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawSphere(leftSideLedgeRayOrigin, 0.1f);
        Gizmos.DrawSphere(rightSideLedgeRayOrigin, 0.1f);
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

    private void HeadLedgeDetect()
    {
        

    }
    /// <summary>
    /// Checks if the ledge has finished in the move direction
    /// </summary>
    /// <param name="forwardHit"> player's holding point of the ledge</param>
    /// <param name="moveDir"> Moving direction of the player. -1 left, 1 right</param>
    /// <returns> false if there is no ledge to continue</returns>
    public bool CheckLedgeInMoveDirection(RaycastHit forwardHit, float moveDir)
    {
        bool isRightSideHit = false;
        bool isLeftSideHit = false;

        leftSideLedgeRayOrigin = transform.TransformPoint(transform.InverseTransformPoint(forwardHit.point) + leftSideRayOffset);
        rightSideLedgeRayOrigin = transform.TransformPoint(transform.InverseTransformPoint(forwardHit.point) + rightSideRayOffset);

        if(moveDir == 1)
        {
            isRightSideHit = Physics.Raycast(rightSideLedgeRayOrigin, -transform.right, out rightSideHit, 1, climbableLayers);
            if (isRightSideHit)
            {
                if (Vector3.Angle(forwardHit.normal, rightSideHit.normal) < maxAngleBetweenLedges)
                {
                    return true;
                }
            }
        }
        else if(moveDir == -1)
        {
            isLeftSideHit = Physics.Raycast(leftSideLedgeRayOrigin, transform.right, out leftSideHit, 1, climbableLayers);
            if (isLeftSideHit)
            {
                if (Vector3.Angle(forwardHit.normal, leftSideHit.normal) < maxAngleBetweenLedges)
                {
                    return true;
                }
            }
        }
        if(!isRightSideHit && !isLeftSideHit)
        {
            return true;
        }
       
        return false;
    }

    public bool CheckLedgeInLookDirection()
    {
        return true;
    }

   
}
