using System.Collections.Generic;
using UnityEngine;

public class EquipmentController : MonoBehaviour
{
    [Header("Equippable Item Holder")]
    [SerializeField] private Transform itemHolder;

    [Header("References")]
    [SerializeField] private EquipTransitionAnimator equipAnimator;
    [SerializeField] private RuntimeEquippable fallbackHandsPrefab;

    private PlayerManager playerManager;
    private PlayerInventory playerInventory;

    private readonly Dictionary<string, GameObject> itemPrefabByCode = new();
    private GameObject currentItemObject;
    private IItemInputReceiver currentInputReceiver;
    private InventoryItem equippedInventoryItem;
    private string currentlyEquippingItemCode = "";

    public InventoryItem EquippedItem => equippedInventoryItem;

    private void Awake()
    {
        playerManager = GetComponentInParent<PlayerManager>();
        playerInventory = playerManager?.GetInventory();

        RegisterAllEquippableObjects();
    }

    private void Start()
    {
        if (playerInventory != null)
        {
            playerInventory.SubscribeToSelectionChanges(TryEquipItem);
        }
    }

    private void OnDestroy()
    {
        if (playerInventory != null)
        {
            playerInventory.UnsubscribeFromSelectionChanges(TryEquipItem);
        }
    }

    private void RegisterAllEquippableObjects()
    {
        itemPrefabByCode.Clear();

        if (itemHolder == null)
        {
            Debug.LogError("[EquipmentController] itemHolder is not assigned.");
            return;
        }

        foreach (Transform child in itemHolder)
        {
            if (child.TryGetComponent(out RuntimeEquippable runtime))
            {
                string code = runtime.GetItemCode();
                if (!string.IsNullOrEmpty(code))
                {
                    itemPrefabByCode[code] = child.gameObject;
                    child.gameObject.SetActive(false);
                    Debug.Log($"[EquipmentController] Registered equippable: {code}");
                }
            }
        }

        if (fallbackHandsPrefab != null && !itemPrefabByCode.ContainsKey(fallbackHandsPrefab.GetItemCode()))
        {
            itemPrefabByCode[fallbackHandsPrefab.GetItemCode()] = fallbackHandsPrefab.gameObject;
            fallbackHandsPrefab.gameObject.SetActive(false);
            Debug.Log($"[EquipmentController] Registered fallback hands prefab: {fallbackHandsPrefab.GetItemCode()}");
        }
    }

    public void HandleInput(IPlayerInput input)
    {
        if (equipAnimator.IsPlaying || currentInputReceiver == null) return;

        if (input.Fire1Down) currentInputReceiver.OnFire1Down();
        if (input.Fire1Hold) currentInputReceiver.OnFire1Hold();
        if (input.Fire1Up) currentInputReceiver.OnFire1Up();

        if (input.Fire2Down) currentInputReceiver.OnFire2Down();
        if (input.Fire2Hold) currentInputReceiver.OnFire2Hold();
        if (input.Fire2Up) currentInputReceiver.OnFire2Up();

        if (input.UtilityDown) currentInputReceiver.OnUtilityDown();
        if (input.UtilityUp) currentInputReceiver.OnUtilityUp();

        if (input.ReloadDown) currentInputReceiver.OnReloadDown();
    }

    private void TryEquipItem(InventoryItem item)
    {
        if (item == equippedInventoryItem)
            return; // Same instance, skip

        EquipSelectedItem(item);
    }

    private void EquipSelectedItem(InventoryItem item)
    {
        string itemCode = item?.data?.itemCode ?? fallbackHandsPrefab?.GetItemCode();

        if (string.IsNullOrEmpty(itemCode) || !itemPrefabByCode.TryGetValue(itemCode, out var prefab))
        {
            Debug.LogWarning($"[EquipmentController] No prefab found for '{itemCode}', falling back to hands.");
            prefab = fallbackHandsPrefab?.gameObject;
        }

        if (currentItemObject != null)
            currentItemObject.SetActive(false);

        currentItemObject = prefab;
        currentInputReceiver = prefab?.GetComponent<IItemInputReceiver>();
        equippedInventoryItem = item;
        currentlyEquippingItemCode = itemCode;

        prefab?.SetActive(true);
        Debug.Log($"[EquipmentController] Equipped '{itemCode}'.");

        if (equipAnimator != null)
        {
            equipAnimator.Play(() =>
            {
                Debug.Log($"[EquipmentController] Equip animation finished for '{itemCode}'.");
            });
        }
    }

    public void SetForceSlowWalk(bool value)
    {
        playerManager?.SetSlowWalk(value);
    }

    public PlayerInventory GetPlayerInventory() => playerInventory;
    public GameObject GetCurrentItemObject() => currentItemObject;
    public IItemInputReceiver GetCurrentInputReceiver() => currentInputReceiver;
    
}
