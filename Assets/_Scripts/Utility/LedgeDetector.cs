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


    public List<GameObject> ledgeList = new List<GameObject>();

    private void Start()
    {
        playerController = GetComponent<PlayerController>();
        
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
        

        //headRayDirection = playerController.transform.position - headRay.position;
        //sideRayDirection = sideRay.forward;
        //if (playerController.isGrounded == false)
        //{
        //    if(Physics.Raycast(headRay.position, headRayDirection, out RaycastHit hit, 1f, climbableLayers))
        //    {
        //        if(Vector3.Dot(hit.normal,Vector3.up) > 0)
        //        {
        //            if (Physics.Raycast(sideRay.position, sideRayDirection, out RaycastHit sidehit, 1f, climbableLayers))
        //            {
        //                lastLedge = hit.transform;
        //                ledgeGrabbingPoint = new Vector3(sidehit.point.x, hit.point.y, sidehit.point.z);
        //                OnClimbingStart.Invoke(
        //                    transform.TransformPoint(transform.InverseTransformPoint(ledgeGrabbingPoint) - ledgeOffset),
        //                    -sidehit.normal
        //                    );
        //            }
        //        }
        //    }
        //}
    }

   
}
