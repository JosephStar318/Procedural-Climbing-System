using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class HashManager
{
    public static Dictionary<AnimatorVariables, int> animatorHashDict = new Dictionary<AnimatorVariables, int>();

    static HashManager()
    {
        AddToAnimatorHash(AnimatorVariables.ApplyRootMotionTag, "ApplyRootMotion");
        AddToAnimatorHash(AnimatorVariables.Speed, "Speed");
        AddToAnimatorHash(AnimatorVariables.SpeedX, "SpeedX");
        AddToAnimatorHash(AnimatorVariables.SpeedZ, "SpeedZ");
        AddToAnimatorHash(AnimatorVariables.Jump, "Jump");
        AddToAnimatorHash(AnimatorVariables.FallingToBracedHang, "Falling To Braced Hang");
        AddToAnimatorHash(AnimatorVariables.FallingToFreeHang, "Falling To Free Hang");
        AddToAnimatorHash(AnimatorVariables.Grounded, "Grounded");
        AddToAnimatorHash(AnimatorVariables.Falling, "Falling");
        AddToAnimatorHash(AnimatorVariables.Braced, "Braced");
        AddToAnimatorHash(AnimatorVariables.Drop, "Drop");
        AddToAnimatorHash(AnimatorVariables.Vault, "Vault");
        AddToAnimatorHash(AnimatorVariables.ClimbOver, "Climb Over");
        AddToAnimatorHash(AnimatorVariables.ClimbingOverState, "Climbing Over");
        AddToAnimatorHash(AnimatorVariables.BracedHangingBlendState, "Braced Hanging Blend Tree");
        AddToAnimatorHash(AnimatorVariables.FreedHangingBlendState, "Free Hanging Blend Tree");
        AddToAnimatorHash(AnimatorVariables.FallingToBracedHangState, "Falling To Braced Hang");
        AddToAnimatorHash(AnimatorVariables.FallingToFreeHangState, "Falling To Free Hang");
        AddToAnimatorHash(AnimatorVariables.VaultingState, "Vaulting");
        AddToAnimatorHash(AnimatorVariables.SteppingState, "Stepping");
        AddToAnimatorHash(AnimatorVariables.LandingState, "Landing");
        AddToAnimatorHash(AnimatorVariables.BracedHangToDropState, "Braced Hang To Drop");
        AddToAnimatorHash(AnimatorVariables.FreeHangToDropState, "Free Hang To Drop");
        AddToAnimatorHash(AnimatorVariables.BracedHangHopUpState, "Braced Hang Hop Up");
        AddToAnimatorHash(AnimatorVariables.BracedHangHopDownState, "Braced Hang Hop Down");
        AddToAnimatorHash(AnimatorVariables.BracedHangHopRightState, "Braced Hang Hop Right");
        AddToAnimatorHash(AnimatorVariables.BracedHangHopLeftState, "Braced Hang Hop Left");
        AddToAnimatorHash(AnimatorVariables.FreeHangHopRightState, "Free Hang Hop Right");
        AddToAnimatorHash(AnimatorVariables.FreeHangHopLeftState, "Free Hang Hop Left");
        AddToAnimatorHash(AnimatorVariables.BracedToFreeState, "Braced To Free");
        AddToAnimatorHash(AnimatorVariables.FreeToBracedHangState, "Free To Braced");
        AddToAnimatorHash(AnimatorVariables.FreeHangClimbOverState, "Free Hang Climb Over");
        AddToAnimatorHash(AnimatorVariables.DropToFreeHangState, "Drop To Free Hang");
        AddToAnimatorHash(AnimatorVariables.RightFootIKWeight, "Right Foot IK Weight");
        AddToAnimatorHash(AnimatorVariables.LeftFootIKWeight, "Left Foot IK Weight");
        AddToAnimatorHash(AnimatorVariables.RightHandWeight, "Right Hand IK Weight");
        AddToAnimatorHash(AnimatorVariables.LeftHandWeight, "Left Hand IK Weight");
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
    ApplyRootMotionTag,
    Speed,
    SpeedX,
    SpeedZ,
    Jump,
    FallingToBracedHang,
    FallingToFreeHang,
    Grounded,
    Falling,
    Braced,
    ClimbOver,
    Drop,
    Vault,
    BracedHangingBlendState,
    FreedHangingBlendState,
    ClimbingOverState,
    FallingToBracedHangState,
    FallingToFreeHangState,
    VaultingState,
    SteppingState,
    LandingState,
    BracedHangToDropState,
    FreeHangToDropState,
    BracedHangHopUpState,
    BracedHangHopDownState,
    BracedHangHopRightState,
    BracedHangHopLeftState,
    FreeHangHopRightState,
    FreeHangHopLeftState,
    BracedToFreeState,
    FreeToBracedHangState,
    FreeHangClimbOverState,
    DropToFreeHangState,
    RightFootIKWeight,
    LeftFootIKWeight,
    RightHandWeight,
    LeftHandWeight
}
