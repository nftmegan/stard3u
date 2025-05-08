using UnityEngine;
using System.Collections.Generic;
using System.Linq;

[RequireComponent(typeof(Rigidbody))]
public class VehicleRoot : MonoBehaviour {
    [Tooltip("The ScriptableObject defining the chassis of this vehicle.")]
    public CarPartData chassisDataDefinition; // Assign your Chassis CarPartData SO here

    private PartInstance _chassisInstance;
    private List<PartInstance> _allVehicleParts = new List<PartInstance>();
    public IReadOnlyList<PartInstance> AllVehicleParts => _allVehicleParts.AsReadOnly();
    public Rigidbody Rigidbody { get; private set; }

    public float ThrottleIntent { get; private set; }
    public float BrakeIntent { get; private set; }
    public float SteeringIntent { get; private set; }
    public int DesiredGear { get; private set; }
    public bool IgnitionEngaged { get; private set; }

    public EngineInstance PrimaryEngine { get; private set; }
    public GearboxInstance PrimaryGearbox { get; private set; }

    void Awake() {
        Rigidbody = GetComponent<Rigidbody>();
        if (Rigidbody == null) {
            Debug.LogError("VehicleRoot on " + gameObject.name + " requires a Rigidbody component!", this);
            enabled = false; return;
        }

        if (chassisDataDefinition != null && chassisDataDefinition.worldPrefab != null) {
            GameObject chassisObject = Instantiate(chassisDataDefinition.worldPrefab, transform.position, transform.rotation, transform);
            chassisObject.name = $"{chassisDataDefinition.itemName}_Instance_RootChassis";
            _chassisInstance = chassisObject.GetComponent<PartInstance>();

            if (_chassisInstance != null) {
                // Create the InventoryItem for the chassis.
                // Chassis typically doesn't have complex runtime state beyond durability/wear from its PartInstance.
                IPartRuntimeState chassisRuntimeState = chassisDataDefinition.CreateDefaultRuntimeState();
                InventoryItem chassisInvItem = new InventoryItem(chassisDataDefinition, chassisRuntimeState);
                // RegisterPart now takes InventoryItem
                RegisterPart(_chassisInstance, chassisInvItem);
            } else {
                Debug.LogError("Chassis prefab '" + chassisDataDefinition.worldPrefab.name + "' is missing a PartInstance derived component!", this);
            }
        } else {
            Debug.LogError("ChassisDataDefinition or its worldPrefab not assigned to VehicleRoot on " + gameObject.name, this);
        }
    }

    void Start() {
        DiscoverPrimaryComponents();
    }

    public void UpdateDriverIntentions(float throttle, float brake, float steer, int gear, bool ignition) {
        this.ThrottleIntent = Mathf.Clamp01(throttle);
        this.BrakeIntent = Mathf.Clamp01(brake);
        this.SteeringIntent = Mathf.Clamp(steer, -1f, 1f);
        this.DesiredGear = gear;
        this.IgnitionEngaged = ignition;
    }

    /// <summary>
    /// Registers a PartInstance with this vehicle.
    /// The PartInstance should already have its GameObject instantiated.
    /// </summary>
    /// <param name="partInstance">The PartInstance component on the part's GameObject.</param>
    /// <param name="itemInstanceData">The InventoryItem containing the CarPartData and IPartRuntimeState for this part.</param>
    public void RegisterPart(PartInstance partInstance, InventoryItem itemInstanceData) {
        if (partInstance == null) { Debug.LogWarning("Attempted to register a null partInstance.", this); return; }
        if (itemInstanceData == null || itemInstanceData.data == null || itemInstanceData.runtime == null) {
            Debug.LogError($"Attempted to register part '{partInstance.name}' with invalid InventoryItem data.", this);
            return;
        }
        if (!_allVehicleParts.Contains(partInstance)) {
            _allVehicleParts.Add(partInstance);
            partInstance.Initialize(itemInstanceData, this); // Initialize with its data and this VehicleRoot
            // If a new part is added at runtime, primary components might need rediscovery
            if (Application.isPlaying && Time.timeSinceLevelLoad > 0.1f) DiscoverPrimaryComponents();
        }
    }

    public void UnregisterPart(PartInstance partInstance) {
        if (partInstance == null) return;
        if (_allVehicleParts.Remove(partInstance)) {
            partInstance.SetOwningVehicle(null); // Notify part it's no longer attached
            if (Application.isPlaying) DiscoverPrimaryComponents();
        }
    }

    private void DiscoverPrimaryComponents() {
        PrimaryEngine = _allVehicleParts.OfType<EngineInstance>().FirstOrDefault();
        PrimaryGearbox = _allVehicleParts.OfType<GearboxInstance>().FirstOrDefault();
    }

    void FixedUpdate() {
        if (_allVehicleParts.Count == 0 && _chassisInstance == null) return;

        foreach (PartInstance part in _allVehicleParts) {
            if (part != null && part.gameObject.activeInHierarchy) // Ensure part still exists
                part.PrePhysicsSimulateTick(Time.fixedDeltaTime);
        }
        // UNITY PHYSICS SIMULATION
        foreach (PartInstance part in _allVehicleParts) {
            if (part != null && part.gameObject.activeInHierarchy)
                part.PostPhysicsSimulateTick(Time.fixedDeltaTime);
        }
    }
}