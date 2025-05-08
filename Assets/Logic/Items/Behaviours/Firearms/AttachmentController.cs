using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Manages firearm attachments: visuals, state listening, effect aggregation,
/// calculating combined STAT MULTIPLIERS, and updating Recoil/ADS/Spread handlers.
/// Handles default visuals (e.g., iron sights) and aim points via Default Attachments.
/// </summary>
public class AttachmentController : MonoBehaviour
{
    // --- Inspector Fields ---
    [Header("Required Scene References")]
    [SerializeField] private Transform attachmentsRoot;
    [Header("Default Values")]
    [SerializeField] private Vector3 defaultCameraAnchorOffset = new Vector3(0, 0, 0.15f);

    // --- References Set by FirearmBehavior ---
    private FirearmItemData def;
    private FirearmRuntimeState state;
    private RecoilHandler recoilHandler;
    private ADSController adsController;
    private SpreadHandler spreadController;

    // --- Internal State ---
    private bool _isInitialized = false;
    private readonly Dictionary<int, GameObject> _activeAttachmentInstances = new Dictionary<int, GameObject>();
    private readonly List<AttachmentStatModifier> _activeStatModifiers = new List<AttachmentStatModifier>();
    private readonly Dictionary<string, GameObject> _defaultAttachmentInstances = new Dictionary<string, GameObject>(); // Key: mountPointTag
    private readonly Dictionary<string, Transform> _mountPoints = new Dictionary<string, Transform>();
    private Transform _currentWeaponAimPoint;
    private Vector3 _currentCameraAnchorOffset;

    // --- Initialization & Lifecycle ---
    private void Awake() { CacheMountPoints(); }
    public void Initialize(FirearmRuntimeState firearmState, FirearmItemData firearmDef, RecoilHandler recoilH, ADSController adsCtrl, SpreadHandler spreadCtrl) { this.state = firearmState; this.def = firearmDef; this.recoilHandler = recoilH; this.adsController = adsCtrl; this.spreadController = spreadCtrl; _isInitialized = false; ValidateInitializationReferences(); ClearDefaultAttachmentVisuals(); ClearRealAttachmentsVisuals(); InstantiateAllDefaultAttachments(); UnsubscribeFromAttachmentChanges(); SubscribeToAttachmentChanges(); RefreshAllAttachments(); if (adsController != null) { adsController.ForceStopAiming(); if (_currentWeaponAimPoint != null) { adsController.SetWeaponAimPoint(_currentWeaponAimPoint); adsController.SetCameraAnchorOffset(_currentCameraAnchorOffset); } else { Debug.LogError($"[{GetType().Name} on '{this.gameObject.name}'] Post-Initialize: Cannot SetWeaponAimPoint - No aim point found!", this); } } _isInitialized = true; }
    private void OnEnable() { if (_isInitialized) { SetAllDefaultAttachmentsActive(true); RefreshAllAttachments(); } }
    private void OnDisable() { UnsubscribeFromAttachmentChanges(); ClearRealAttachmentsVisuals(); ClearDefaultAttachmentVisuals(); _isInitialized = false; }

    // --- Event Handlers ---
    private void HandleAttachmentSlotChanged(int slotIndex) { if (!_isInitialized) return; if (slotIndex < 0) RefreshAllAttachments(); else RefreshAttachment(slotIndex); }

    // --- Core Attachment Management ---
    private void RefreshAllAttachments() { ClearRealAttachmentsVisuals(); SetAllDefaultAttachmentsActive(true); HashSet<string> usedMountTags = new HashSet<string>(); if (state?.attachments != null) { for (int i = 0; i < state.attachments.Slots.Length; i++) { var slot = state.attachments.Slots[i]; if (slot == null || slot.IsEmpty()) continue; AttachmentItemData attachmentData = slot.item.data as AttachmentItemData; GameObject instantiatedGO = InstantiateSingleRealAttachment(attachmentData, i); if (instantiatedGO != null && attachmentData != null && !string.IsNullOrEmpty(attachmentData.mountPointTag)) { string mountTag = attachmentData.mountPointTag; if (!string.IsNullOrEmpty(mountTag)) { usedMountTags.Add(mountTag); HideDefaultAttachment(mountTag); } } } } UpdateHandlersWithCurrentState(); }
    private void RefreshAttachment(int slotIndex) { if (state?.attachments == null || slotIndex < 0 || slotIndex >= state.attachments.Slots.Length) return; string previousMountTag = GetMountTagForSlot(slotIndex); ClearSingleRealAttachmentVisuals(slotIndex); SetDefaultAttachmentActive(previousMountTag, true); var slot = state.attachments.Slots[slotIndex]; AttachmentItemData newAttachmentData = slot?.item?.data as AttachmentItemData; GameObject newInstanceGO = null; if (newAttachmentData != null) { newInstanceGO = InstantiateSingleRealAttachment(newAttachmentData, slotIndex); } if (newInstanceGO != null && newAttachmentData != null && !string.IsNullOrEmpty(newAttachmentData.mountPointTag)) { HideDefaultAttachment(newAttachmentData.mountPointTag); } UpdateHandlersWithCurrentState(); }

    // --- Visual Instantiation & Cleanup Helpers ---
    private GameObject InstantiateSingleRealAttachment(AttachmentItemData data, int slotIndex) { if (data == null || data.attachmentPrefab == null) return null; if (_mountPoints.TryGetValue(data.mountPointTag, out var mount)) { var go = Instantiate(data.attachmentPrefab, mount.position, mount.rotation, mount); go.transform.localPosition = Vector3.zero; go.transform.localRotation = Quaternion.identity; go.name = data.attachmentPrefab.name + $" (Slot {slotIndex})"; _activeAttachmentInstances[slotIndex] = go; var statModifier = go.GetComponentInChildren<AttachmentStatModifier>(true); if (statModifier != null) { if (!_activeStatModifiers.Contains(statModifier)) { _activeStatModifiers.Add(statModifier); } } return go; } else { Debug.LogWarning($"[{GetType().Name}] Mount point tag '{data.mountPointTag}' not found for attachment '{data.itemName}' slot {slotIndex}.", this); return null; } }
    private void InstantiateSingleDefaultAttachment(string mountTag, GameObject prefab) { if (string.IsNullOrEmpty(mountTag) || prefab == null) return; if (_mountPoints.TryGetValue(mountTag, out var mount)) { if (_defaultAttachmentInstances.TryGetValue(mountTag, out var oldDefaultGO) && oldDefaultGO != null) { Destroy(oldDefaultGO); } var go = Instantiate(prefab, mount.position, mount.rotation, mount); go.transform.localPosition = Vector3.zero; go.transform.localRotation = Quaternion.identity; go.name = prefab.name + " (Default)"; _defaultAttachmentInstances[mountTag] = go; go.SetActive(true); } else { Debug.LogWarning($"[{GetType().Name}] Default Attachment: Mount point tag '{mountTag}' not found.", this); } }
    private void InstantiateAllDefaultAttachments() { if (def != null && def.defaultAttachments != null) { foreach (var mapping in def.defaultAttachments) { InstantiateSingleDefaultAttachment(mapping.mountPointTag, mapping.defaultPrefab); } } }
    private void ClearRealAttachmentsVisuals() { foreach (var kvp in _activeAttachmentInstances.ToList()) { ClearSingleRealAttachmentVisuals(kvp.Key); } _activeAttachmentInstances.Clear(); _activeStatModifiers.Clear(); }
    private void ClearSingleRealAttachmentVisuals(int slotIndex) { if (_activeAttachmentInstances.TryGetValue(slotIndex, out var instanceGO)) { if (instanceGO != null) { var statModifier = instanceGO.GetComponentInChildren<AttachmentStatModifier>(true); if (statModifier != null) _activeStatModifiers.Remove(statModifier); Destroy(instanceGO); } _activeAttachmentInstances.Remove(slotIndex); } }
    private void ClearDefaultAttachmentVisuals() { foreach (var kvp in _defaultAttachmentInstances) { if (kvp.Value != null) { Destroy(kvp.Value); } } _defaultAttachmentInstances.Clear(); }
    private void HideDefaultAttachment(string mountTag) { SetDefaultAttachmentActive(mountTag, false); }
    private void SetDefaultAttachmentActive(string mountTag, bool isActive) { if (!string.IsNullOrEmpty(mountTag) && _defaultAttachmentInstances.TryGetValue(mountTag, out var defaultInstance) && defaultInstance != null) { if(defaultInstance.activeSelf != isActive) { defaultInstance.SetActive(isActive); } } }
    private void SetAllDefaultAttachmentsActive(bool isActive) { foreach(var defaultInstance in _defaultAttachmentInstances.Values) { if (defaultInstance != null && defaultInstance.activeSelf != isActive) { defaultInstance.SetActive(isActive); } } }

    // --- Calculation & Handler Updates ---
    private void UpdateHandlersWithCurrentState() { float combinedRecoilMagnitudeMultiplier = 1.0f; float combinedRecoverySpeedMultiplier = 1.0f; float combinedBaseSpreadMultiplier = 1.0f; float combinedMaxSpreadMultiplier = 1.0f; float combinedSpreadIncreaseMultiplier = 1.0f; float combinedSpreadRecoveryMultiplier = 1.0f; foreach (var modifier in _activeStatModifiers) { if (modifier == null) continue; combinedRecoilMagnitudeMultiplier *= modifier.recoilMagnitudeMultiplier; combinedRecoverySpeedMultiplier *= modifier.recoverySpeedMultiplier; combinedBaseSpreadMultiplier *= modifier.baseSpreadMultiplier; combinedMaxSpreadMultiplier *= modifier.maxSpreadMultiplier; combinedSpreadIncreaseMultiplier *= modifier.spreadIncreaseMultiplier; combinedSpreadRecoveryMultiplier *= modifier.spreadRecoveryMultiplier; } UpdateCurrentAimPointAndOffset(); if (recoilHandler != null) { RecoilPattern baseRecoilPattern = (def?.baseRecoilPattern != null) ? def.baseRecoilPattern : new RecoilPattern(); recoilHandler.SetBaseRecoilPattern(baseRecoilPattern); recoilHandler.SetRecoilModifiers(combinedRecoilMagnitudeMultiplier, combinedRecoverySpeedMultiplier, def?.adsVisualRecoilMultiplier ?? 1.0f); } if (spreadController != null) { SpreadPattern baseSpreadPattern = (def?.baseSpreadPattern != null) ? def.baseSpreadPattern : new SpreadPattern(); spreadController.Initialize(baseSpreadPattern, def?.adsSpreadMultiplier ?? 1.0f); spreadController.SetSpreadModifiers(combinedBaseSpreadMultiplier, combinedMaxSpreadMultiplier, combinedSpreadIncreaseMultiplier, combinedSpreadRecoveryMultiplier); } if (adsController != null) { if (_currentWeaponAimPoint != null) { adsController.SetWeaponAimPoint(_currentWeaponAimPoint); adsController.SetCameraAnchorOffset(_currentCameraAnchorOffset); } else { Debug.LogError($"[{GetType().Name} on '{this.gameObject.name}'] UpdateHandlers: Cannot SetWeaponAimPoint - No aim point found!", this); } } }

    // UPDATED Aim Point Logic - Removed final fallback search
    private void UpdateCurrentAimPointAndOffset()
    {
        Transform finalAimPoint = null;
        AttachmentItemData sightItemData = null;
        bool usingRealSight = false;

        // 1. Check REAL attachments first
        if (state?.attachments != null) {
            foreach (var kvp in _activeAttachmentInstances) {
                GameObject instance = kvp.Value; int slotIdx = kvp.Key;
                if (instance == null || slotIdx >= state.attachments.Slots.Length) continue;
                var slot = state.attachments.Slots[slotIdx]; if (slot == null || slot.IsEmpty()) continue;
                var data = slot.item.data as AttachmentItemData;
                if (data?.attachmentType == AttachmentType.Sight) {
                    var aimPointComponent = instance.GetComponentInChildren<AttachmentAimPoint>(true);
                    if (aimPointComponent != null) {
                        finalAimPoint = aimPointComponent.transform; sightItemData = data; usingRealSight = true; break;
                    } else { Debug.LogWarning($"Real Sight '{data.itemName}' missing AttachmentAimPoint component!", instance); }
                }
            }
        }

        // 2. If NO real sight found, check ACTIVE DEFAULT attachment prefabs (on "SightMount")
        if (finalAimPoint == null) {
            if (_defaultAttachmentInstances.TryGetValue("SightMount", out var defaultInstance) && defaultInstance != null && defaultInstance.activeSelf) {
                 var defaultAimPointComponent = defaultInstance.GetComponentInChildren<AttachmentAimPoint>(true);
                 if (defaultAimPointComponent != null) {
                     finalAimPoint = defaultAimPointComponent.transform; // Use default aim point
                 } else { Debug.LogWarning($"Default attachment on 'SightMount' ({defaultInstance.name}) is missing AttachmentAimPoint component!", defaultInstance); }
            }
        }

        // 3. REMOVED Fallback search on base weapon hierarchy

        // --- Assign the determined Aim Point ---
        _currentWeaponAimPoint = finalAimPoint;
        // Debug.Log($"[{gameObject.name}] Updated Aim Point via '{foundMethod}': {(_currentWeaponAimPoint != null ? _currentWeaponAimPoint.name : "NULL")}");

        // --- Determine Camera Offset ---
        _currentCameraAnchorOffset = (usingRealSight && sightItemData != null && sightItemData.overrideCameraAnchorOffset)
            ? sightItemData.customCameraAnchorOffset
            : ((def != null) ? def.defaultCameraAnchorOffset : Vector3.zero);
    }

    // --- Utility Methods ---
    private void ValidateInitializationReferences() { if (recoilHandler == null) Debug.LogError($"[{GetType().Name}] Init Check Failed: RecoilHandler null!", this); if (adsController == null) Debug.LogError($"[{GetType().Name}] Init Check Failed: ADSController null!", this); if (spreadController == null) Debug.LogError($"[{GetType().Name}] Init Check Failed: SpreadController null!", this); }
    private void CacheMountPoints() { _mountPoints.Clear(); if (attachmentsRoot != null) { foreach (Transform t in attachmentsRoot) { if (t != null && !string.IsNullOrEmpty(t.gameObject.tag)) { _mountPoints[t.gameObject.tag] = t; } } if(_mountPoints.Count == 0) { Debug.LogWarning($"[{GetType().Name} on {gameObject.name}] No child Transforms with Tags found under Attachments Root '{attachmentsRoot.name}'.", attachmentsRoot); } } else { Debug.LogWarning($"[{GetType().Name} on {gameObject.name}] Attachments Root not assigned.", this); } }
    private void SubscribeToAttachmentChanges() { if (state?.attachments != null) { state.attachments.OnSlotChanged -= HandleAttachmentSlotChanged; state.attachments.OnSlotChanged += HandleAttachmentSlotChanged; } }
    private void UnsubscribeFromAttachmentChanges() { if (state?.attachments != null) { state.attachments.OnSlotChanged -= HandleAttachmentSlotChanged; } }
    private string GetMountTagForSlot(int slotIndex) { if (_activeAttachmentInstances.TryGetValue(slotIndex, out var instanceGO) && instanceGO != null) { if(instanceGO.transform.parent != null) { return instanceGO.transform.parent.tag; } } return null; }

} // End of AttachmentController class