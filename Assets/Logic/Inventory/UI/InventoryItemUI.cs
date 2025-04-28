using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class InventoryItemUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [SerializeField] private Image iconImage;
    [SerializeField] private Text quantityText;

    private RectTransform rectTransform;
    private Canvas canvas;
    private CanvasGroup canvasGroup;
    private int sourceSlotIndex;

    private Vector2 originalPosition;
    private Transform originalParent;

    private bool wasDropped = false;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        canvas = GetComponentInParent<Canvas>();
    }

    public void Initialize(Sprite icon, int quantity, int slotIndex)
    {
        iconImage.sprite = icon;
        iconImage.enabled = true;
        quantityText.text = quantity > 1 ? quantity.ToString() : "";
        sourceSlotIndex = slotIndex;
    }

    public int GetSlotIndex() => sourceSlotIndex;

    public void OnBeginDrag(PointerEventData eventData)
    {
        originalPosition = rectTransform.anchoredPosition;
        originalParent = transform.parent;

        canvasGroup.blocksRaycasts = false;
        transform.SetParent(canvas.transform);
        wasDropped = false;
    }

    public void OnDrag(PointerEventData eventData)
    {
        rectTransform.position = Input.mousePosition;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        canvasGroup.blocksRaycasts = true;

        if (wasDropped)
        {
            Debug.Log("[ItemUI] Item dropped successfully, destroying dragged visual.");
            Destroy(gameObject); // ✅ DESTROY immediately without resetting parent
        }
        else
        {
            Debug.Log("[ItemUI] Drag canceled, returning to original slot.");
            transform.SetParent(originalParent);
            rectTransform.anchoredPosition = Vector2.zero;
        }
    }

    public void MarkAsDropped()
    {
        wasDropped = true;
    }
}
