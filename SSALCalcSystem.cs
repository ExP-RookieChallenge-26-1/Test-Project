using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using UnityEngine;

public partial class SSALCalcSystem : SystemBase
{
    protected override void OnUpdate()
    {
    }

    public float[,] CalculateResult(float topPercent)
    {
        float[,] result = new float[2, 3];

        if (!SystemAPI.HasSingleton<SSALCupData>()) return result;

        var zoneEntity = SystemAPI.GetSingletonEntity<SSALCupData>();
        var zoneData = SystemAPI.GetComponent<SSALCupData>(zoneEntity);
        var zoneTransform = SystemAPI.GetComponent<LocalTransform>(zoneEntity);

        float yMin = zoneTransform.Position.y - (zoneData.CountSize.y / 2);
        float xMin = zoneTransform.Position.x - (zoneData.CountSize.x / 2);
        float xMax = zoneTransform.Position.x + (zoneData.CountSize.x / 2);

        float highestY = yMin;

        foreach (var transform in SystemAPI.Query<RefRO<LocalTransform>>().WithAll<SSALType>())
        {
            float3 pos = transform.ValueRO.Position;
            if (pos.x >= xMin && pos.x <= xMax && pos.y >= yMin)
            {
                if (pos.y > highestY) highestY = pos.y;
            }
        }

        float pileHeight = highestY - yMin;

        float ratio = (100f - topPercent) / 100f;
        float cutLine = yMin + (pileHeight * ratio);

        foreach (var (transform, typeData) in SystemAPI.Query<RefRO<LocalTransform>, RefRO<SSALType>>())
        {
            float3 pos = transform.ValueRO.Position;

            if (pos.x >= xMin && pos.x <= xMax && pos.y >= yMin)
            {
                int tierIdx = 0;

                if (pos.y >= cutLine)
                {
                    tierIdx = 0;
                }
                else
                {
                    tierIdx = 1;
                }

                int typeIdx = 0;
                switch (typeData.ValueRO.Type)
                {
                    case SSALMode.Rice: typeIdx = 0; break;
                    case SSALMode.Grain: typeIdx = 1; break;
                    case SSALMode.Sand: typeIdx = 2; break;
                }
                result[tierIdx, typeIdx]++;
            }
        }

        var spawnerData = SystemAPI.GetSingletonEntity<SpawnerData>();
        result[0, 0] /= SpawnerManager.Instance.RiceSetting.SpawnAmount;
        result[0, 1] /= SpawnerManager.Instance.GrainSetting.SpawnAmount;
        result[0, 2] /= SpawnerManager.Instance.SandSetting.SpawnAmount;
        result[1, 0] /= SpawnerManager.Instance.RiceSetting.SpawnAmount;
        result[1, 1] /= SpawnerManager.Instance.GrainSetting.SpawnAmount;
        result[1, 2] /= SpawnerManager.Instance.SandSetting.SpawnAmount;

        return result;
    }
}