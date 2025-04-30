using UnityEngine;

[CreateAssetMenu(fileName = "NewHandsItem", menuName = "Items/Hands")]
public class HandsItemData : ItemData
{
    // Optional: define special stats for hands
    public float punchCooldown = 0.5f;
    public float grabReach = 2.0f;
}