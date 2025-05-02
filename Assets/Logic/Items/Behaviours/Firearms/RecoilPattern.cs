using UnityEngine;
using System.Collections;

[CreateAssetMenu(fileName = "RecoilPattern", menuName = "Weapons/Recoil Pattern")]
public class RecoilPattern : ScriptableObject {
    [Header("Rotation (°)")]
    public float verticalMin    = 1f;
    public float verticalMax    = 2f;
    public float horizontalMin  = -1f;
    public float horizontalMax  =  1f;
    public float rollMin        = -0.5f;
    public float rollMax        =  0.5f;

    [Header("Translation (units)")]
    [Tooltip("Minimum backward kick (local Z)")]
    public float kickbackMin    = 0.02f;
    [Tooltip("Maximum backward kick (local Z)")]
    public float kickbackMax    = 0.05f;

    [Header("Timing (s)")]
    public float recoilDuration   = 0.1f;
    public float recoveryDuration = 0.2f;

    [Tooltip("Easing curve for both recoil & recovery")]
    public AnimationCurve recoilCurve = AnimationCurve.EaseInOut(0,0,1,1);
}