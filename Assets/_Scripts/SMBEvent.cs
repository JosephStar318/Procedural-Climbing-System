using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SMBEvent : StateMachineBehaviour
{
    public static event Action<AnimatorStateInfo, AnimatorState> OnSMBEvent;

    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        OnSMBEvent.Invoke(stateInfo, AnimatorState.Enter);
    }
    public override void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        OnSMBEvent.Invoke(stateInfo, AnimatorState.Update);
    }

    public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        OnSMBEvent.Invoke(stateInfo, AnimatorState.Exit);
    }
}
public enum AnimatorState
{
    Enter,
    Update,
    Exit
}
