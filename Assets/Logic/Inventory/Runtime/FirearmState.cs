using UnityEngine;

[System.Serializable]
public class FirearmState : IRuntimeState
{
    public ItemContainer magazine;
    public ItemContainer attachments;
    public int durability;

    public FirearmState(int magazineSize, int attachmentSlots, int initialDurability = 100)
    {
        magazine    = new ItemContainer(1);              // single slot
        attachments = new ItemContainer(attachmentSlots);
        durability  = initialDurability;

        // magazine[0] initially empty
    }
}