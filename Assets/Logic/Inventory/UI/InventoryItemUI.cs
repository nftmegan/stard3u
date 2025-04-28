using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class InventoryItemUI : MonoBehaviour,
                              IBeginDragHandler,
                              IDragHandler,
                              IEndDragHandler
{
    [SerializeField] private Image iconImage;
    [SerializeField] private Text  quantityText;

    private RectTransform rect;
    private CanvasGroup   grp;
    private Canvas        rootCanvas;
    private int           sourceSlot;
    private Transform     originalParent;
    private bool          wasDropped;

    private void Awake()
    {
        rect = GetComponent<RectTransform>();
        grp  = GetComponent<CanvasGroup>();
        rootCanvas = GetComponentInParent<Canvas>();
    }

    public void Initialize(Sprite icon, int qty, int slotIdx)
    {
        iconImage.sprite = icon;
        iconImage.enabled = true;
        quantityText.text = qty > 1 ? qty.ToString() : "";
        sourceSlot = slotIdx;
    }

    public int GetSlotIndex() => sourceSlot;

    public void OnBeginDrag(PointerEventData e)
    {
        wasDropped    = false;
        originalParent = transform.parent;
        grp.blocksRaycasts = false;
        transform.SetParent(rootCanvas.transform, false);
    }

    public void OnDrag(PointerEventData e)
    {
        rect.position = Input.mousePosition;
    }

    public void OnEndDrag(PointerEventData e)
    {
        grp.blocksRaycasts = true;
        if (wasDropped)
        {
            Destroy(gameObject);
        }
        else
        {
            transform.SetParent(originalParent, false);
            rect.anchoredPosition = Vector2.zero;
        }
    }

    public void MarkAsDropped() => wasDropped = true;
}