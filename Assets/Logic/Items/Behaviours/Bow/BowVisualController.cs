using UnityEngine;

public class BowArrowVisualEffect : MonoBehaviour
{
    [Header("Arrow Visual")]
    [SerializeField] private GameObject visualArrow;
    
    [Header("Arrow Transform")]
    [SerializeField] private Transform arrowTransform;
    [SerializeField] private Transform stringTransform;

    [Tooltip("How far back the arrow moves on full draw (in local units).")]
    [SerializeField] private float drawDistance = 0.2f;

    private Vector3 initialLocalPosition;
    private Vector3 initialLocalPosition2;
    private Vector3 initialLocalPosition3;
    private bool isArrowVisible = false;

    private void Awake()
    {
        if (arrowTransform != null)
            initialLocalPosition = arrowTransform.localPosition;

        if (stringTransform != null)
            initialLocalPosition2 = stringTransform.localPosition;
    }

    public void SetArrowVisibility(bool visible)
    {
        isArrowVisible = visible;
        if (visualArrow != null)
            visualArrow.SetActive(visible);
    }

    public void UpdateDraw(float normalizedPull)
    {
        if (arrowTransform == null) return;

        float offset = drawDistance * normalizedPull;
        arrowTransform.localPosition = initialLocalPosition - Vector3.forward * offset;

        stringTransform.localPosition = initialLocalPosition2 - Vector3.forward * offset;
    }
}