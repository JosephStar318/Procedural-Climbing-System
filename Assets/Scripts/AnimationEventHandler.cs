using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimationEventHandler : MonoBehaviour
{
    public PlayerController playerController;

    public void OnJumpEvent()
    {
        playerController.Jump();
    }
}
