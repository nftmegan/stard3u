using UnityEngine;
using Game.InventoryLogic;

[DisallowMultipleComponent]
public class InventoryComponent : MonoBehaviour
{
    [SerializeField] private int initialSize = 30;

    public ItemContainer Container { get; private set; }

    private void Awake()
    {
        Container = new ItemContainer(initialSize);
    }
}