using UnityEngine;
using System.Collections;

[CreateAssetMenu(fileName = "RecoilPattern", menuName = "Weapons/Recoil Pattern")]
public class RecoilPattern : ScriptableObject {
    [Tooltip("Minimum upward recoil (degrees)")]
    public float verticalMin = 1f;
    [Tooltip("Maximum upward recoil (degrees)")]
    public float verticalMax = 2f;

    [Tooltip("Minimum sideways recoil (degrees)")]
    public float horizontalMin = -1f;
    [Tooltip("Maximum sideways recoil (degrees)")]
    public float horizontalMax = 1f;

    [Tooltip("Time to apply the recoil")]
    public float recoilDuration = 0.1f;
    [Tooltip("Time to recover back to original orientation")]
    public float recoveryDuration = 0.2f;

    [Tooltip("Curve controlling the easing of recoil and recovery")]
    public AnimationCurve recoilCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
}
