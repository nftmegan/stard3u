// ────────────────────────────────────────────────────────────────────────────
//  PlayerGrabController.cs   (drop in Assets/Logic/Player/Grab/)
// ────────────────────────────────────────────────────────────────────────────
using UnityEngine;

/// <summary>
/// One-file grab / move / rotate / drop controller.
/// Keeps the original public API so existing code compiles.
/// </summary>
[RequireComponent(typeof(PlayerManager))]
public sealed class PlayerGrabController : MonoBehaviour
{
    // ────────────────────────────────────────────────────  inspector ────────
    [Header("Raycast layers & reach")]
    [SerializeField] float     interactionReach    = 3f;
    [SerializeField] LayerMask grabbableMask       = 0;
    [SerializeField] LayerMask mountMask           = 0;

    [Header("Hold distance (scroll)")]
    [SerializeField] float holdDefault = 1.6f;
    [SerializeField] float holdMin     = 0.4f;
    [SerializeField] float holdMax     = 3.5f;
    [SerializeField] float scrollSpeed = 0.3f;      // wheel units → metres

    [Header("Movement & collision")]
    [SerializeField] bool  smoothFollow      = true;
    [SerializeField] float smoothSpeed       = 15f;
    [SerializeField] LayerMask worldMask     = ~0;   // collide with everything by default
    [SerializeField] float sweepSkin         = 0.02f;
    [SerializeField] float teleportDistance  = 1.0f; // “stuck” threshold

    [Header("Rotation (MMB)")]
    [SerializeField] float rotationDegPerPx  = 110f;

    // ────────────────────────────────────────────────────  public API ───────
    public  bool IsGrabbing                 => _grabbed != null;
    public  IGrabbable CurrentGrabbedItem   => _grabbed;
    public  event System.Action<bool,IGrabbable> OnGrabStateChanged;

    //  (public methods below, exactly like the old file) ─────────────────────
    public void InitializeController(PlayerManager mgr) => Initialise(mgr);
    public bool TryGrabOrDetachWorldObject()            => GrabOrDetach();
    public void DropGrabbedItemWithLMB()                => DropHeld();
    public void AdjustGrabbedItemDistance(float d)      => ScrollDistance(d);
    public void HandleStoreAction()                     => StoreOrPull();
    public bool TryAttachGrabbedPart()                  => TryAttachPart();
    public void StartGrabRotation()                     => _rotating = IsGrabbing;
    public void EndGrabRotation()                       => _rotating = false;
    public void ApplyGrabbedItemRotationInput(Vector2 d)=> RotateHeld(d);

    // ────────────────────────────────────────────────────  private state ────
    PlayerManager     _pm;
    IAimProvider      _aim;
    PlayerInventory   _inv;
    InteractionController _interact;

    IGrabbable  _grabbed;
    Transform   _grabTf;
    Rigidbody   _grabRb;
    Collider[]  _grabCols;

    bool        _rbWasKinematic, _rbHadGravity;
    RigidbodyInterpolation _rbInterp;
    CollisionDetectionMode _rbCCD;

    bool        _rotating;
    float       _holdDist;
    Quaternion  _heldRot;

    Transform   _anchor;            // child of camera

    // ────────────────────────────────────────────────────  unity plumbing ───
    void Awake()
    {
        if (TryGetComponent(out PlayerManager pm)) Initialise(pm);
    }

    void FixedUpdate()
    {
        if (!IsGrabbing || _grabRb == null || !_grabRb.isKinematic) return;

        // 1) anchor stays in front of camera
        _anchor.localPosition = new Vector3(0, 0, _holdDist);

        // 2) where we want to be
        Vector3 wantPos = _anchor.position;
        Vector3 curPos  = _grabRb.position;
        Vector3 delta   = wantPos - curPos;
        Vector3 nextPos = wantPos;

        // Sweep-test for blocking geometry
        if (delta.sqrMagnitude > 1e-6f &&
            _grabRb.SweepTest(delta.normalized, out var hit, delta.magnitude, QueryTriggerInteraction.Ignore))
        {
            nextPos = curPos + delta.normalized * Mathf.Max(0, hit.distance - sweepSkin);
        }

        // Teleport if hopelessly far
        if (Vector3.Distance(nextPos, wantPos) > teleportDistance)
            nextPos = wantPos;

        // 3) move & rotate
        if (smoothFollow)
        {
            _grabRb.MovePosition(Vector3.Lerp(curPos, nextPos, smoothSpeed * Time.fixedDeltaTime));
            _grabRb.MoveRotation(Quaternion.Slerp(_grabRb.rotation, _heldRot,  smoothSpeed * Time.fixedDeltaTime));
        }
        else
        {
            _grabRb.MovePosition(nextPos);
            _grabRb.MoveRotation(_heldRot);
        }
    }

    // ───────────────────────────────────── initialisation helper ────────────
    void Initialise(PlayerManager pm)
    {
        _pm  = pm;
        _aim = pm.Look;
        _inv = pm.Inventory;
        _interact = FindFirstObjectByType<InteractionController>();

        _holdDist = holdDefault;

        if (!_anchor)
        {
            _anchor = new GameObject("HoldAnchor").transform;
            _anchor.SetParent(_aim.GetLookTransform(), false);
            _anchor.localPosition = new Vector3(0, 0, _holdDist);
        }
    }

    // ─────────────────────────────────────────── grab / detach  ─────────────
    bool GrabOrDetach()
    {
        if (IsGrabbing)
        {
            if (TryAttachPart()) return true;
            DropHeld();
            return true;
        }
        return TryGrabFromScene();
    }

    bool TryGrabFromScene()
    {
        Ray ray = _aim.GetLookRay();

        // a) detach from MountPoint
        if (mountMask.value != 0 &&
            Physics.Raycast(ray, out var mHit, interactionReach, mountMask,
                            QueryTriggerInteraction.Collide))
        {
            var m = mHit.collider.GetComponentInParent<MountPoint>();
            if (m && m.CurrentlyAttachedPart)
            {
                BeginGrab(m.Detach(), mHit.distance);
                return true;
            }
        }

        // b) generic grabbable
        if (grabbableMask.value != 0 &&
            Physics.Raycast(ray, out var gHit, interactionReach, grabbableMask,
                            QueryTriggerInteraction.Collide))
        {
            var g = gHit.collider.GetComponentInParent<IGrabbable>();
            if (g != null && g.CanGrab())
            {
                BeginGrab(g, gHit.distance);
                return true;
            }
        }
        return false;
    }

    void BeginGrab(IGrabbable g, float hitDist)
    {
        // cache refs
        _grabbed = g;
        _grabTf  = g.GetTransform();
        _grabRb  = _grabTf.GetComponent<Rigidbody>();
        _grabCols= _grabTf.GetComponentsInChildren<Collider>(true);

        // cache & override RB
        if (_grabRb)
        {
            _rbWasKinematic = _grabRb.isKinematic;
            _rbHadGravity   = _grabRb.useGravity;
            _rbInterp       = _grabRb.interpolation;
            _rbCCD          = _grabRb.collisionDetectionMode;

            _grabRb.isKinematic            = true;
            _grabRb.useGravity             = false;
            _grabRb.interpolation          = RigidbodyInterpolation.Interpolate;
            _grabRb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
        }

        // ignore player collider
        if (_pm.CharacterController)
        {
            var pc = _pm.CharacterController.GetComponent<Collider>();
            foreach (var c in _grabCols) if (c) Physics.IgnoreCollision(pc, c, true);
        }

        _holdDist   = Mathf.Clamp(hitDist, holdMin, holdMax);
        _anchor.localPosition = new Vector3(0, 0, _holdDist);
        _heldRot    = _grabTf.rotation;
        _rotating   = false;

        _grabbed.OnGrabbed(_aim.GetLookTransform());
        OnGrabStateChanged?.Invoke(true, _grabbed);
    }

    void DropHeld()
    {
        if (!IsGrabbing) return;

        if (_grabRb)
        {
            _grabRb.isKinematic            = _rbWasKinematic;
            _grabRb.useGravity             = _rbHadGravity;
            _grabRb.interpolation          = _rbInterp;
            _grabRb.collisionDetectionMode = _rbCCD;

            if (!_grabRb.isKinematic)
            {
                var vel = _aim.GetLookRay().direction * 2f;
                _grabRb.linearVelocity  = vel;
                _grabRb.angularVelocity = Random.insideUnitSphere * 0.5f;
            }
        }

        // restore player collisions
        if (_pm.CharacterController)
        {
            var pc = _pm.CharacterController.GetComponent<Collider>();
            foreach (var c in _grabCols) if (c) Physics.IgnoreCollision(pc, c, false);
        }

        _grabbed.OnDropped(_grabRb ? _grabRb.linearVelocity : Vector3.zero);
        ClearGrabState(invokeEvent:true);
    }

    void ClearGrabState(bool invokeEvent)
    {
        var old = _grabbed;

        _grabbed = null;
        _grabTf  = null;
        _grabRb  = null;
        _grabCols= null;
        _rotating= false;
        _holdDist= holdDefault;
        _anchor.localPosition = new Vector3(0, 0, _holdDist);
        _heldRot = Quaternion.identity;

        if (invokeEvent && old != null) OnGrabStateChanged?.Invoke(false, old);
    }

    // ─────────────────────────────────────────── rotation / scroll ──────────
    void RotateHeld(Vector2 delta)
    {
        if (!IsGrabbing || !_rotating) return;
        var cam = _aim.GetLookTransform();
        _heldRot =
            Quaternion.AngleAxis(-delta.x * rotationDegPerPx * Time.deltaTime, Vector3.up) *
            Quaternion.AngleAxis( delta.y * rotationDegPerPx * Time.deltaTime, cam.right) *
            _heldRot;
    }

    void ScrollDistance(float scroll)
    {
        if (!IsGrabbing || _rotating) return;
        _holdDist = Mathf.Clamp(_holdDist + scroll * scrollSpeed, holdMin, holdMax);
        _anchor.localPosition = new Vector3(0, 0, _holdDist);
    }

    // ───────────────────────────────────────────── attach part ──────────────
    bool TryAttachPart()
    {
        if (!IsGrabbing || !(_grabbed is PartInstance part) || _interact == null) return false;

        var info = _interact.CurrentLookTargetInfo;
        if (info.HasTarget && info.Mount && info.Mount.IsCompatible(part))
        {
            if (info.Mount.TryAttach(part)) { ClearGrabState(invokeEvent:true); return true; }
        }
        return false;
    }

    // ───────────────────────────────────────── store / pull from inv ────────
    void StoreOrPull()
    {
        if (IsGrabbing) TryStoreHeld();
        else            TryPullAndGrab();
    }

    void TryStoreHeld()
    {
        if (!IsGrabbing || _inv == null) { DropHeld(); return; }

        var item = _grabbed.GetInventoryItemData();
        if (item == null || item.data == null) { DropHeld(); return; }

        bool ok = false;
        int sel = _inv.GetSelectedToolbarIndex();

        if (sel >= 0 && _inv.TryStoreItemInSpecificSlot(item, sel)) ok = true;
        else if (!item.data.isBulky) ok = _inv.RequestAddItemToInventory(item);

        if (ok)
        {
            _grabbed.OnStored();
            Destroy(_grabTf.gameObject);
            ClearGrabState(invokeEvent:true);
        }
        else DropHeld();
    }

    bool TryPullAndGrab()
    {
        if (_inv == null) return false;

        int sel = _inv.GetSelectedToolbarIndex();
        if (sel < 0) return false;

        if (!_inv.TryPullItemFromSlot(sel, out var pulled) ||
            pulled?.data?.worldPrefab == null) return false;

        var ray = _aim.GetLookRay();
        _holdDist = holdDefault;
        Vector3 pos = ray.GetPoint(_holdDist);
        Quaternion rot = _aim.GetLookTransform().rotation;

        var go = Instantiate(pulled.data.worldPrefab, pos, rot);
        if (!go) return false;

        var g = go.GetComponentInChildren<IGrabbable>();
        if (g == null) { Destroy(go); return false; }

        if      (g is ItemInstance ii) ii.Initialize(pulled);
        else if (g is WorldItem  wi) wi.Initialize(pulled);

        BeginGrab(g, _holdDist);
        return true;
    }
}
