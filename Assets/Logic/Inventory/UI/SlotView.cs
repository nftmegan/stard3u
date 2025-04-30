using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Removed namespace

[RequireComponent(typeof(CanvasGroup))]
internal class SlotView : MonoBehaviour,
                            IBeginDragHandler, IDragHandler, IEndDragHandler,
                            IDropHandler,    IPointerClickHandler
{
    [Header("Visuals")]
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Image iconImage;
    [SerializeField] private Text  quantityText;

    [Header("Runtime")]
    [SerializeField] private int slotIndex;

    // References
    private InventoryUIManager ui;
    private Canvas             rootCanvas;
    private CanvasGroup        cg;

    // Drag state
    private GameObject dragGhost;
    private Color      iconOrigColor;
    private Color      txtOrigColor;

    // REMOVED: Static DraggingChanged event

    internal void Setup(int idx, InventoryUIManager manager) { slotIndex = idx; ui = manager; }

    private void Awake()
    {
        rootCanvas = GetComponentInParent<Canvas>();
        cg = GetComponent<CanvasGroup>();
        if (cg == null) cg = gameObject.AddComponent<CanvasGroup>();
        if (iconImage == null) Debug.LogError($"SlotView ({gameObject.name}) missing Icon Image!", this);
        if (quantityText == null) Debug.LogError($"SlotView ({gameObject.name}) missing Quantity Text!", this);
        if (rootCanvas == null) Debug.LogError($"SlotView ({gameObject.name}) missing parent Canvas!", this);
    }

    // Drawing methods (DrawEmpty, DrawStack, SetSelected) remain the same...
    internal void DrawEmpty() { if (iconImage) { iconImage.sprite = null; iconImage.enabled = false; iconImage.raycastTarget = false; } if(quantityText) { quantityText.enabled = false; quantityText.text = ""; } }
    internal void DrawStack(Sprite icon, int qty) { if (iconImage) { iconImage.sprite = icon; iconImage.enabled = true; iconImage.raycastTarget = true; } else return; if(quantityText) { bool show = qty > 1; quantityText.enabled = show; quantityText.text = show ? qty.ToString() : ""; } }
    internal void SetSelected(bool isSelected, Color selectedColor, Color normalColor) { if(backgroundImage) backgroundImage.color = isSelected ? selectedColor : normalColor; }


    // --- Drag and Drop Handlers ---
    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!iconImage || !iconImage.enabled || !iconImage.sprite || rootCanvas == null) return;

        // Create Ghost (same as before)
        dragGhost = new GameObject("DragGhost", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
        var rt  = dragGhost.GetComponent<RectTransform>();
        var img = dragGhost.GetComponent<Image>();
        var ghostCg = dragGhost.GetComponent<CanvasGroup>();
        img.sprite = iconImage.sprite; img.raycastTarget = false; ghostCg.blocksRaycasts = false; ghostCg.ignoreParentGroups = true;
        rt.SetParent(rootCanvas.transform, false); rt.SetAsLastSibling(); rt.sizeDelta = this.GetComponent<RectTransform>().sizeDelta; rt.position = eventData.position;

        // Fade Original (same as before)
        iconOrigColor = iconImage.color; txtOrigColor = quantityText.color;
        var halfAlphaIcon = iconOrigColor; halfAlphaIcon.a *= 0.5f; var halfAlphaText = txtOrigColor; halfAlphaText.a *= 0.5f;
        iconImage.color = halfAlphaIcon; if (quantityText.enabled) quantityText.color = halfAlphaText;
        cg.blocksRaycasts = false;

        // --- Call CursorController Directly ---
        CursorController.Instance.SetDragging(true);
        // --- End Call ---
    }

    public void OnDrag(PointerEventData eventData) { if (dragGhost) dragGhost.transform.position = eventData.position; }

    public void OnEndDrag(PointerEventData eventData)
    {
        // Clean up Ghost (same as before)
        if (dragGhost) Destroy(dragGhost); dragGhost = null;

        // Restore Original (same as before)
        if(iconImage) iconImage.color = iconOrigColor; if(quantityText) quantityText.color = txtOrigColor; if (cg != null) cg.blocksRaycasts = true;

        // --- Call CursorController Directly ---
        CursorController.Instance.SetDragging(false);
        // --- End Call ---
    }

    public void OnDrop(PointerEventData eventData) // Logic remains the same
    {
        GameObject draggedObject = eventData.pointerDrag; if (draggedObject == null) return;
        SlotView sourceSlotView = draggedObject.GetComponent<SlotView>();
        if (sourceSlotView == null || sourceSlotView.slotIndex == this.slotIndex) return;
        if (ui != null) ui.RequestMergeOrSwap(sourceSlotView.slotIndex, this.slotIndex);
    }

    public void OnPointerClick(PointerEventData eventData) // Logic remains the same
    {
        if (eventData.button == PointerEventData.InputButton.Left) { if (ui != null) ui.RequestInspect(slotIndex); }
    }
}