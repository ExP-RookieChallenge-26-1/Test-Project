using Sirenix.OdinInspector;
using Unity.Entities;
using UnityEngine;

public class SpawnerManager : Singleton<SpawnerManager>
{
    public SpawnerSetting RiceSetting;
    public SpawnerSetting SandSetting;
    public SpawnerSetting GrainSetting;

    [ReadOnly]
    public SSALMode CurrentMode;
    private EntityManager _entityManager;

    void Start()
    {
        CurrentMode = SSALMode.Default;
        _entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
    }

#if UNITY_EDITOR
    [ContextMenu("Rice")]
#endif
    public void SetRiceMode() => SetMode(SSALMode.Rice);
#if UNITY_EDITOR
    [ContextMenu("Sand")]
#endif
    public void SetSandMode() => SetMode(SSALMode.Sand);
#if UNITY_EDITOR
    [ContextMenu("Grain")]
#endif
    public void SetGrainMode() => SetMode(SSALMode.Grain);

    public void SetMode(SSALMode ssalMode)
    {
        CurrentMode = ssalMode;

        EntityQuery query = _entityManager.CreateEntityQuery(typeof(SpawnerData));
        if (query.CalculateEntityCount() == 0) return;

        Entity entity = query.GetSingletonEntity();
        SpawnerData data = _entityManager.GetComponentData<SpawnerData>(entity);

        SpawnerSetting setting;
        int spawnAmount = 0;
        switch (ssalMode)
        {
            case SSALMode.Rice:
                setting = RiceSetting;
                spawnAmount = StageData.ETC[0].RiceAmount;
                break;
            case SSALMode.Sand:
                setting = SandSetting;
                spawnAmount = StageData.ETC[0].SandAmount;
                break;
            case SSALMode.Grain:
                setting = GrainSetting;
                spawnAmount = StageData.ETC[0].GrainAmount;
                break;
            default:
                setting = RiceSetting;
                break;
        }

        data.CurrentMode = ssalMode;
        data.Range = setting.Range;
        data.SpawnAmount = spawnAmount;

        _entityManager.SetComponentData(entity, data);
    }
}
