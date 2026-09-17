using UnityEngine;

/// <summary>
/// Small n7a compatibility helper used while older runtime components are being
/// suppressed. Behaviour itself has no StopAllCoroutines method; all current
/// campaign behaviours are MonoBehaviours, so the extension safely forwards the
/// call when applicable.
/// </summary>
public static class CampaignBehaviourExtensionsV010N7A
{
    public static void StopAllCoroutines(this Behaviour behaviour)
    {
        MonoBehaviour mono = behaviour as MonoBehaviour;
        if (mono != null)
            mono.StopAllCoroutines();
    }
}
