using UnityEngine;

[RequireComponent(typeof(Inventory))]
public class InventoryDebugUI : MonoBehaviour
{
    private Inventory inventory;
    private Vector2 scrollPos;

    [Header("Debug UI Settings")]
    public bool showInventory = true;
    public string windowTitle = "Inventory Debug";
    public Rect windowRect = new Rect(10, 10, 300, 400);

    [Header("Test Items")]
    public ItemData bowItem;
    public ItemData toolItem;
    public ItemData ammoItem;

    private void Awake()
    {
        inventory = GetComponent<Inventory>();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.B) && bowItem != null)
            inventory.AddItem(bowItem, 1);

        if (Input.GetKeyDown(KeyCode.N) && toolItem != null)
            inventory.AddItem(toolItem, 1);

        if (Input.GetKeyDown(KeyCode.M) && ammoItem != null)
            inventory.AddItem(ammoItem, 5);

        if (Input.GetKeyDown(KeyCode.I))
            showInventory = !showInventory;
    }

    private void OnGUI()
    {
        if (showInventory)
            windowRect = GUI.Window(0, windowRect, DrawInventoryWindow, windowTitle);
    }

    private void DrawInventoryWindow(int windowID)
    {
        scrollPos = GUILayout.BeginScrollView(scrollPos);

        for (int i = 0; i < inventory.GetSlotCount(); i++)
        {
            var item = inventory.GetItemAt(i);
            string label = item != null ? $"{item.data.itemName} x{item.quantity}" : "[Empty]";
            GUILayout.Label($"Slot {i}: {label}");
        }

        GUILayout.EndScrollView();
        GUI.DragWindow();
    }
}