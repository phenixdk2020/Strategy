using UnityEngine;

public enum PrototypeAttachmentType09F30B
{
    Organic,
    Attached,
    Detached,
    Reserve
}

// v00.00.09f30b
// Generic command-parent metadata for higher-command assets.
// OrganicParent is the permanent OOB parent; CurrentCommandParent is the HQ that currently
// exercises tactical command. This is deliberately independent from movement ownership.
public sealed class PrototypeCommandAttachment09F30B : MonoBehaviour
{
    public string OrganicParent { get; private set; } = string.Empty;
    public string CurrentCommandParent { get; private set; } = string.Empty;
    public PrototypeAttachmentType09F30B AttachmentType { get; private set; } = PrototypeAttachmentType09F30B.Organic;

    public void Configure(string organicParent, string currentCommandParent, PrototypeAttachmentType09F30B type)
    {
        OrganicParent = organicParent ?? string.Empty;
        CurrentCommandParent = currentCommandParent ?? string.Empty;
        AttachmentType = type;
    }

    public void SetCurrentCommandParent(string currentCommandParent, PrototypeAttachmentType09F30B type)
    {
        CurrentCommandParent = currentCommandParent ?? string.Empty;
        AttachmentType = type;
    }

    public string GetShortLabel()
    {
        string prefix = AttachmentType == PrototypeAttachmentType09F30B.Organic ? "ORG" :
            AttachmentType == PrototypeAttachmentType09F30B.Reserve ? "RES" :
            AttachmentType == PrototypeAttachmentType09F30B.Detached ? "DET" : "ATT";
        return prefix + " " + CurrentCommandParent;
    }
}
