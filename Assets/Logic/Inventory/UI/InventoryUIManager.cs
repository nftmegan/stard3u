using UnityEngine;

namespace UI.Inventory
{
    public class InventoryUIManager : MonoBehaviour
    {
        [Header("Scene references")]
        [SerializeField] private PlayerInventory playerInv;
        [SerializeField] private Transform       toolbarParent;
        [SerializeField] private Transform       bagParent;
        [SerializeField] private Color           selectedColor = Color.yellow;
        [SerializeField] private Color           normalColor   = Color.white;
        [SerializeField] private ItemDetailUI    detailPanel;

        [Header("Prefabs")]
        [SerializeField] private SlotView slotPrefab;

        private SlotView[] toolbarViews;
        private SlotView[] bagViews;

        /* ───────── initialise ───────── */
        public void Initialize(PlayerInventory pi)
        {
            playerInv = pi;

            toolbarViews = toolbarParent.GetComponentsInChildren<SlotView>(true);
            bagViews     = bagParent   .GetComponentsInChildren<SlotView>(true);

            int idx = 0;
            foreach (var v in toolbarViews) v.Setup(idx++, this);
            foreach (var v in bagViews)     v.Setup(idx++, this);

            playerInv.Resize(idx);

            playerInv.Container.OnSlotChanged += UpdateSingleSlot;
            playerInv.Toolbar.OnIndexChanged  += UpdateHighlight;

            RedrawAll();
        }

        /* ───────── external requests ───────── */
        public void RequestMergeOrSwap(int from,int to) =>
            playerInv.MergeOrSwap(from,to);

        public void RequestInspect(int slot)
        {
            var s = playerInv.GetSlotAt(slot);
            if (s?.item == null) return;
            detailPanel.ShowFor(s.item, s.quantity, slot);
        }

        /* ───────── drawing helpers ───────── */
        private void RedrawAll()
        {
            int toolLen = toolbarViews.Length;
            for (int i=0;i<toolLen;i++)
                Draw(toolbarViews[i], playerInv.GetSlotAt(i));
            for (int i=0;i<bagViews.Length;i++)
                Draw(bagViews[i], playerInv.GetSlotAt(i+toolLen));

            UpdateHighlight(playerInv.GetEquippedIndex());               // new
        }

        private void UpdateSingleSlot(int idx)
        {
            int toolLen = toolbarViews.Length;
            if (idx < toolLen)
                Draw(toolbarViews[idx], playerInv.GetSlotAt(idx));
            else
                Draw(bagViews[idx-toolLen], playerInv.GetSlotAt(idx));
        }

        private void Draw(SlotView v, InventorySlot slot)
        {
            if (slot == null || slot.item == null || slot.quantity == 0)
                v.DrawEmpty();
            else
                v.DrawStack(slot.item.data.icon, slot.quantity);
        }

        private void UpdateHighlight(int idx)
        {
            for (int i=0;i<toolbarViews.Length;i++)
                toolbarViews[i].SetSelected(i==idx, selectedColor, normalColor);
        }
    }
}