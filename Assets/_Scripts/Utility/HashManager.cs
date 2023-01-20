using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class HashManager
{
    public static Dictionary<AnimatorVariables, int> animatorHashDict = new Dictionary<AnimatorVariables, int>();

    static HashManager()
    {
        AddToAnimatorHash(AnimatorVariables.Speed, "Speed");
        AddToAnimatorHash(AnimatorVariables.SpeedX, "SpeedX");
        AddToAnimatorHash(AnimatorVariables.SpeedZ, "SpeedZ");
        AddToAnimatorHash(AnimatorVariables.Jump, "Jump");
        AddToAnimatorHash(AnimatorVariables.FallingHang, "Falling Hang");
        AddToAnimatorHash(AnimatorVariables.Grounded, "Grounded");
        AddToAnimatorHash(AnimatorVariables.Falling, "Falling");
        AddToAnimatorHash(AnimatorVariables.Braced, "Braced");
        AddToAnimatorHash(AnimatorVariables.Drop, "Drop");
        AddToAnimatorHash(AnimatorVariables.Vault, "Vault");
        AddToAnimatorHash(AnimatorVariables.ClimbOver, "Climb Over");
        AddToAnimatorHash(AnimatorVariables.ClimbingOverState, "Climbing Over");
        AddToAnimatorHash(AnimatorVariables.HangingBlendState, "Hanging Blend Tree");
        AddToAnimatorHash(AnimatorVariables.FallingToBracedHangState, "Falling To Braced Hang");
        AddToAnimatorHash(AnimatorVariables.VaultingState, "Vaulting");
        AddToAnimatorHash(AnimatorVariables.LandingState, "Landing");
    }

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
    Vault,
    HangingBlendState,
    ClimbingOverState,
    FallingToBracedHangState,
    VaultingState,
    LandingState
}
