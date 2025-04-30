using System.Collections.Generic;
using UnityEngine;
using Game.InventoryLogic;          // ItemContainer
using UI.Inventory;                // InventoryItem

/// <summary>
/// Activates / deactivates weapon prefabs according to the player’s
/// equipped hot-bar item and forwards input to them.
/// </summary>
[DisallowMultipleComponent]
public class EquipmentController : MonoBehaviour
{
    /* ─────── Serialized fields ─────── */
    [Header("Equippable Item Holder")]
    [SerializeField] private Transform itemHolder;           // children = prefabs

    [Header("Animation")]
    [SerializeField] private EquipTransitionAnimator equipAnimator;

    [Header("Fallback")]
    [SerializeField] private RuntimeEquippable fallbackHandsPrefab;

    /* ─────── runtime refs ─────── */
    private PlayerManager   playerManager;
    private PlayerInventory playerInventory;

    private readonly Dictionary<string, RuntimeEquippable> prefabsByCode = new();

    private RuntimeEquippable currentRuntime;
    private IItemInputReceiver currentInput;
    private InventoryItem      equippedItem;

    public InventoryItem EquippedItem => equippedItem;

    /* ────────── Awake ────────── */
    private void Awake()
    {
        playerManager   = GetComponentInParent<PlayerManager>();
        playerInventory = playerManager?.GetInventory();

        RegisterPrefabs();
    }

    private void OnEnable()
    {
        if (playerInventory != null)
            playerInventory.OnEquippedItemChanged += OnEquipRequest;
    }

    private void OnDisable()
    {
        if (playerInventory != null)
            playerInventory.OnEquippedItemChanged -= OnEquipRequest;
    }

    /* ────────── Prefab registry ────────── */
    private void RegisterPrefabs()
    {
        prefabsByCode.Clear();

        foreach (Transform child in itemHolder)
        {
            if (child.TryGetComponent(out RuntimeEquippable eq))
            {
                prefabsByCode[eq.ItemCode] = eq;
                child.gameObject.SetActive(false);
            }
        }

        if (fallbackHandsPrefab != null &&
            !prefabsByCode.ContainsKey(fallbackHandsPrefab.ItemCode))
        {
            prefabsByCode[fallbackHandsPrefab.ItemCode] = fallbackHandsPrefab;
            fallbackHandsPrefab.gameObject.SetActive(false);
        }
    }

    /* ────────── Input passthrough ────────── */
    public void HandleInput(IPlayerInput input)
    {
        if (equipAnimator.IsPlaying || currentInput == null) return;

        if (input.Fire1Down)   currentInput.OnFire1Down();
        if (input.Fire1Hold)   currentInput.OnFire1Hold();
        if (input.Fire1Up)     currentInput.OnFire1Up();

        if (input.Fire2Down)   currentInput.OnFire2Down();
        if (input.Fire2Hold)   currentInput.OnFire2Hold();
        if (input.Fire2Up)     currentInput.OnFire2Up();

        if (input.UtilityDown) currentInput.OnUtilityDown();
        if (input.UtilityUp)   currentInput.OnUtilityUp();

        if (input.ReloadDown)  currentInput.OnReloadDown();
    }

    /* ────────── Equip flow ────────── */
    private void OnEquipRequest(InventoryItem item)
    {
        if (item == equippedItem) return;   // already equipped
        Equip(item);
    }

    private void Equip(InventoryItem item)
    {
        string code = item?.data?.itemCode ?? fallbackHandsPrefab.ItemCode;

        if (!prefabsByCode.TryGetValue(code, out var runtime))
            runtime = fallbackHandsPrefab;

        /* deactivate previous */
        if (currentRuntime) currentRuntime.gameObject.SetActive(false);

        currentRuntime = runtime;
        currentRuntime.gameObject.SetActive(true);

        /* inject runtime data */
        if (runtime.TryGetComponent(out IEquippableInstance eq))
            eq.Initialize(item, playerInventory.Container);

        /* cache input receiver */
        currentInput = runtime.GetComponent<IItemInputReceiver>();

        equippedItem = item;

        /* optional animation */
        if (equipAnimator) equipAnimator.Play(null);
    }

    /* ────────── helpers ────────── */
    public void SetForceSlowWalk(bool enabled) =>
        playerManager?.SetSlowWalk(enabled);

    public PlayerInventory GetPlayerInventory() => playerInventory;
}
