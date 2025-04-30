// ========================
// ToolItemData.cs
// ========================
using UnityEngine;

[CreateAssetMenu(fileName = "NewToolItem", menuName = "Items/Tool")]
public class ToolItemData : ItemData
{
    private void OnEnable()
    {
        category = ItemCategory.Tool;
    }
}