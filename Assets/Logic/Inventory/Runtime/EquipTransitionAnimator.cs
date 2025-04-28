using System.Collections;
using UnityEngine;

public class EquipTransitionAnimator : MonoBehaviour
{
    public float duration = 0.3f;
    public Vector3 offset = new Vector3(0f, -0.3f, -0.2f);
    public AnimationCurve curve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    private Vector3 originalPosition;
    private bool isAnimating;

    public bool IsPlaying => isAnimating;

    private void Awake()
    {
        originalPosition = transform.localPosition;
    }

    public void Play(System.Action onComplete)
    {
        if (isAnimating)
            StopAllCoroutines(); // 🚨 Cancel previous animation safely

        StartCoroutine(Animate(onComplete));
    }

    private IEnumerator Animate(System.Action onComplete)
    {
        isAnimating = true;

        Transform target = transform;
        Vector3 start = originalPosition + offset;
        Vector3 end = originalPosition;

        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float eased = curve.Evaluate(t / duration);
            target.localPosition = Vector3.Lerp(start, end, eased);
            yield return null;
        }

        target.localPosition = end;
        isAnimating = false;
        onComplete?.Invoke();
    }
}
