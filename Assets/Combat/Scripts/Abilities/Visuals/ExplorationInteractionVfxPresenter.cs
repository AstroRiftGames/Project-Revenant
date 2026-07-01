using UnityEngine;

[DisallowMultipleComponent]
public class ExplorationInteractionVfxPresenter : MonoBehaviour
{
    [SerializeField] private GameObject _recruitmentVfxPrefab;
    [SerializeField] private GameObject _essenceConsumeVfxPrefab;
    [SerializeField] private float _vfxLifetimeSeconds = 5f;
    [SerializeField] private bool _enableDebugLogs = false;

    private void OnEnable()
    {
        RecruitableCorpseHandler.AnyCorpseRecruited += HandleCorpseRecruited;
        RecruitableCorpseHandler.AnyCorpseEssenceAbsorbed += HandleCorpseEssenceAbsorbed;
        if (_enableDebugLogs)
        {
            Debug.Log("[ExplorationInteractionVfxPresenter] OnEnable subscribed.", this);
        }
    }

    private void OnDisable()
    {
        RecruitableCorpseHandler.AnyCorpseRecruited -= HandleCorpseRecruited;
        RecruitableCorpseHandler.AnyCorpseEssenceAbsorbed -= HandleCorpseEssenceAbsorbed;
        if (_enableDebugLogs)
        {
            Debug.Log("[ExplorationInteractionVfxPresenter] OnDisable unsubscribed.", this);
        }
    }

    private void HandleCorpseRecruited(CorpseInteractionVfxContext context)
    {
        if (_enableDebugLogs)
        {
            Debug.Log($"[ExplorationInteractionVfxPresenter] Recruitment event received at corpse={context.CorpsePosition}, necro={context.NecromancerPosition}", this);
        }
        SpawnVfx(_recruitmentVfxPrefab, context, "Recruitment", "DEBUG_Recruitment_VFX_Instance");
    }

    private void HandleCorpseEssenceAbsorbed(CorpseInteractionVfxContext context)
    {
        if (_enableDebugLogs)
        {
            Debug.Log($"[ExplorationInteractionVfxPresenter] Essence event received at corpse={context.CorpsePosition}, necro={context.NecromancerPosition}", this);
        }
        SpawnVfx(_essenceConsumeVfxPrefab, context, "Essence Consume", "DEBUG_EssenceConsume_VFX_Instance");
    }

    private void SpawnVfx(GameObject prefab, CorpseInteractionVfxContext context, string vfxName, string instanceName)
    {
        if (prefab == null)
        {
            if (_enableDebugLogs)
            {
                Debug.LogWarning($"[ExplorationInteractionVfxPresenter] Cannot spawn {vfxName} VFX: Prefab is missing.", this);
            }
            return;
        }

        GameObject instance = Instantiate(prefab, context.CorpsePosition, Quaternion.identity, transform);
        if (instance != null)
        {
            instance.name = instanceName;
            if (instance.TryGetComponent<TestVfxPlaceholder>(out var testVfx))
            {
                testVfx.ConfigureContext(context);
            }
            if (_enableDebugLogs)
            {
                Debug.Log($"[ExplorationInteractionVfxPresenter] Prefab spawned: name={instance.name}, position={instance.transform.position}", this);
            }
            Destroy(instance, _vfxLifetimeSeconds);
        }
    }

    private void LogDebug(string message)
    {
        if (_enableDebugLogs)
        {
            Debug.Log($"[ExplorationInteractionVfxPresenter] {message}", this);
        }
    }
}
