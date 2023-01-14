using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public static class Extensions
{
    public static float RemapClamped(this float aValue, float aIn1, float aIn2, float aOut1, float aOut2)
    {
        float t = (aValue - aIn1) / (aIn2 - aIn1);
        t = Mathf.Clamp01(t);
        return aOut1 + (aOut2 - aOut1) * t;
    }
    public static IEnumerator ChangePositionUntil(this Transform source, Vector3 targetPos, float time)
    {
        float delta = (source.position - targetPos).magnitude;
        while (delta > 0.2f)
        {
            delta = (source.position - targetPos).magnitude;
            source.position = Vector3.Lerp(source.position, targetPos, Time.fixedDeltaTime / time);
            yield return null;
        }
        yield return null;
    }
    public static IEnumerator ChangeRbPositionUntil(this Rigidbody source, Vector3 targetPos, float time)
    {
        float delta = (source.position - targetPos).magnitude;
        while (delta > 0.2f)
        {
            delta = (source.position - targetPos).magnitude;
            source.position = Vector3.Lerp(source.position, targetPos, Time.fixedDeltaTime / time);
            yield return null;
        }
        yield return null;
    }
    public static IEnumerator ChangePositionUntil(this Transform source, Vector3 targetPos, float time, Action AfterMethod)
    {
        float delta = (source.position - targetPos).magnitude;
        while (delta > 0.4f)
        {
            delta = (source.position - targetPos).magnitude;
            source.position = Vector3.Lerp(source.position, targetPos, Time.fixedDeltaTime / time);
            yield return null;
        }
        AfterMethod();
        yield return null;
    }
    public static IEnumerator ChangeRotationUntil(this Transform source, Quaternion targetRotation, float time)
    {
        while (source.rotation != targetRotation)
        {
            source.rotation = Quaternion.RotateTowards(source.rotation, targetRotation, time);
            yield return null;
        }

        yield return null;
    }
    public static IEnumerator ChangeRotationUntil(this Transform source, Quaternion targetRotation, float time, Action AfterMethod)
    {
        while (source.rotation != targetRotation)
        {
            source.rotation = Quaternion.RotateTowards(source.rotation, targetRotation, time);
            yield return null;
        }
        AfterMethod();
        yield return null;
    }
}
