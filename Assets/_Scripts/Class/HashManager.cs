using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class HashManager
{
    public static Dictionary<AnimatorVariables, int> animatorHashDict = new Dictionary<AnimatorVariables, int>();

    public static void AddToAnimatorHash(AnimatorVariables av, string hashName)
    {
        if(animatorHashDict.ContainsKey(av) == false)
        {
            animatorHashDict.Add(av, Animator.StringToHash(hashName));
        }
    }
}

public enum AnimatorVariables
{
    Speed,
    SpeedX,
    SpeedZ,
    Jump,
    FallingHang,
    Grounded,
    Falling,
    Braced,
    ClimbOver,
    Drop,
    HangingIdleState,
    ClimbingOverState,
    FallingToBracedHangState
}
