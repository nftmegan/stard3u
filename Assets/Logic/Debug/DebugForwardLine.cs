using UnityEngine;

public class DebugForwardLine : MonoBehaviour
{
    [SerializeField] private float lineLength = 20f;
    private Color randomColor;

    private void Start()
    {
        randomColor = new Color(Random.value, Random.value, Random.value);
    }

    private void Update()
    {
        Debug.DrawLine(transform.position, transform.position + transform.forward * lineLength, randomColor);
    }
}