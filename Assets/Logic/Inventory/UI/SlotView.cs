using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UI.Inventory
{
    [RequireComponent(typeof(CanvasGroup))]   // still useful for blocking raycasts
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

        private InventoryUIManager ui;
        private Canvas             rootCanvas;
        private CanvasGroup        cg;

        /* ─────────────────────── wiring ─────────────────────────────── */
        internal void Setup(int idx, InventoryUIManager m)
        {
            slotIndex = idx;
            ui        = m;
        }

        private void Awake()
        {
            rootCanvas = GetComponentInParent<Canvas>();
            cg         = GetComponent<CanvasGroup>();
        }

        /* ─────────────────────── drawing helpers ────────────────────── */
        internal void DrawEmpty()
        {
            iconImage.enabled        = false;
            iconImage.raycastTarget  = false;
            quantityText.enabled     = false;
            quantityText.text        = "";
        }

        internal void DrawStack(Sprite icon, int qty)
        {
            iconImage.sprite        = icon;
            iconImage.enabled       = true;
            iconImage.raycastTarget = true;

            quantityText.enabled    = qty > 1;
            quantityText.text       = qty > 1 ? qty.ToString() : "";
        }

        internal void SetSelected(bool sel, Color selCol, Color normCol) =>
            backgroundImage.color = sel ? selCol : normCol;

        /* ─────────────────────── drag / drop ────────────────────────── */
        private GameObject dragGhost;
        private Color      iconOrig;
        private Color      txtOrig;

        public static event Action<bool> DraggingChanged;

        public void OnBeginDrag(PointerEventData e)
        {
            if (!iconImage.enabled) return;          // nothing to drag

            // --- ghost ---
            dragGhost = new GameObject("DragGhost",
                                       typeof(RectTransform),
                                       typeof(CanvasGroup),
                                       typeof(Image));
            var rt  = dragGhost.GetComponent<RectTransform>();
            var img = dragGhost.GetComponent<Image>();
            var g   = dragGhost.GetComponent<CanvasGroup>();

            img.sprite        = iconImage.sprite;
            img.raycastTarget = false;

            rt.SetParent(rootCanvas.transform, false);
            rt.sizeDelta = iconImage.rectTransform.sizeDelta;
            rt.position  = Input.mousePosition;
            g.blocksRaycasts = false;

            // --- fade only item visuals ---
            iconOrig = iconImage.color;
            txtOrig  = quantityText.color;

            var halfIcon = iconOrig; halfIcon.a *= 0.5f;
            var halfTxt  = txtOrig;  halfTxt .a *= 0.5f;

            iconImage.color    = halfIcon;
            quantityText.color = halfTxt;

            DraggingChanged?.Invoke(true); 
        }

        public void OnDrag(PointerEventData e)
        {
            if (dragGhost)
                dragGhost.transform.position = Input.mousePosition;
        }

        public void OnEndDrag(PointerEventData e)
        {
            if (dragGhost) Destroy(dragGhost);
            dragGhost = null;

            // restore full opacity
            iconImage.color    = iconOrig;
            quantityText.color = txtOrig;
            
            DraggingChanged?.Invoke(false); 
        }

        public void OnDrop(PointerEventData e)
        {
            var src = e.pointerDrag?.GetComponent<SlotView>();
            if (src == null || src.slotIndex == slotIndex) return;

            ui.RequestMergeOrSwap(src.slotIndex, slotIndex);
        }

        /* ─────────────────────── click inspect ──────────────────────── */
        public void OnPointerClick(PointerEventData e)
        {
            if (e.button == PointerEventData.InputButton.Left)
                ui.RequestInspect(slotIndex);
        }
    }
}