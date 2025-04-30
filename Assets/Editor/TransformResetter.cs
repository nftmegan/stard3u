using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class TransformResetter : MonoBehaviour
{
    [MenuItem("GameObject/Reset Transform Without Affecting Children", false, 0)]
    private static void ResetTransformWithoutAffectingChildren()
    {
        if (Selection.activeTransform == null) return;

        Transform parent = Selection.activeTransform;

        // Store world transforms of children
        List<Transform> children = new List<Transform>();
        List<Vector3> childPositions = new List<Vector3>();
        List<Quaternion> childRotations = new List<Quaternion>();
        List<Vector3> childScales = new List<Vector3>();

        foreach (Transform child in parent)
        {
            children.Add(child);
            childPositions.Add(child.position);
            childRotations.Add(child.rotation);
            childScales.Add(child.lossyScale);
        }

        Undo.RecordObject(parent, "Reset Parent Transform");

        // Reset parent
        parent.position = Vector3.zero;
        parent.rotation = Quaternion.identity;
        parent.localScale = Vector3.one;

        // Restore children's world transforms
        for (int i = 0; i < children.Count; i++)
        {
            Undo.RecordObject(children[i], "Restore Child Transform");
            children[i].position = childPositions[i];
            children[i].rotation = childRotations[i];

            // Adjust localScale to match previous world scale
            Vector3 parentLossy = parent.lossyScale;
            children[i].localScale = new Vector3(
                childScales[i].x / parentLossy.x,
                childScales[i].y / parentLossy.y,
                childScales[i].z / parentLossy.z
            );
        }
    }
}