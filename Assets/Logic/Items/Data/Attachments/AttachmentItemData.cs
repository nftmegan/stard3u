// AttachmentItemData.cs (Relevant parts shown)
using UnityEngine;

[CreateAssetMenu(menuName = "Items/Attachment")]
public class AttachmentItemData : ItemData
{
    [Header("Attachment Specific")]
    public AttachmentType attachmentType = AttachmentType.None;

    [Tooltip("The visual prefab for this attachment.")]
    public GameObject attachmentPrefab; // The model to instantiate on the weapon

    [Tooltip("The tag of the attachment point Transform on the weapon prefab this attaches to (e.g., 'SightMount', 'MuzzleMount').")]
    public string mountPointTag = "Untagged";

    // --- Sight Specific Data ---
    [Header("Sight Specific (If Type is Sight)")]
    // REMOVED: public string sightAimPointName = "SightAimPoint";

    [Tooltip("Does this sight override the default camera anchor offset?")]
    public bool overrideCameraAnchorOffset = false;

    [Tooltip("The camera anchor offset to use IF overriding the default.")]
    public Vector3 customCameraAnchorOffset = new Vector3(0, 0, 0.1f);
}