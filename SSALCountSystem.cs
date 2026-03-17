using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using Unity.Collections;

public partial class SSALCountSystem : SystemBase
{
    float timer = 0;

    protected override void OnUpdate()
    {
        if (SSALManager.Instance == null) return;
        if (!SSALManager.Instance.Active) return;
        timer += SystemAPI.Time.DeltaTime;
        if (timer < 0.1f) return;
        timer = 0;

        if (!SystemAPI.HasSingleton<SSALCupData>()) return;

        var zoneEntity = SystemAPI.GetSingletonEntity<SSALCupData>();
        var zoneData = SystemAPI.GetComponent<SSALCupData>(zoneEntity);
        var zoneTransform = SystemAPI.GetComponent<LocalTransform>(zoneEntity);

        float xMin = zoneTransform.Position.x - (zoneData.CountSize.x / 2);
        float xMax = zoneTransform.Position.x + (zoneData.CountSize.x / 2);
        float yMin = zoneTransform.Position.y - (zoneData.CountSize.y / 2);
        float yMax = zoneTransform.Position.y + (zoneData.CountSize.y / 2);

        int rice = 0, sand = 0, grain = 0;

        foreach (var (transform, type) in SystemAPI.Query<RefRO<LocalTransform>, RefRO<SSALType>>())
        {
            float3 pos = transform.ValueRO.Position;

            if (pos.x >= xMin && pos.x <= xMax &&
                pos.y >= yMin && pos.y <= yMax)
            {
                switch (type.ValueRO.Type)
                {
                    case SSALMode.Rice: rice++; break;
                    case SSALMode.Sand: sand++; break;
                    case SSALMode.Grain: grain++; break;
                }
            }
        }

        zoneData.RiceCount = rice;
        zoneData.SandCount = sand;
        zoneData.GrainCount = grain;

        Debug.Log($"rice {rice} sand {sand} grain {grain}");

        try
        {
            UIManager.Get<UIDefault>().SetSSALAmountTxt();
        }
        catch
        {
        }
        SystemAPI.SetComponent(zoneEntity, zoneData);
    }

    public void ClearZone()
    {
        if (!SystemAPI.HasSingleton<SSALCupData>()) return;

        var zoneEntity = SystemAPI.GetSingletonEntity<SSALCupData>();
        var zoneData = SystemAPI.GetComponent<SSALCupData>(zoneEntity);
        var zoneTransform = SystemAPI.GetComponent<LocalTransform>(zoneEntity);

        float xMin = zoneTransform.Position.x - (zoneData.CountSize.x / 2);
        float xMax = zoneTransform.Position.x + (zoneData.CountSize.x / 2);
        float yMin = zoneTransform.Position.y - (zoneData.CountSize.y / 2);
        float yMax = zoneTransform.Position.y + (zoneData.CountSize.y / 2);
        float padding = 0.07f;

        var ecb = new EntityCommandBuffer(Allocator.Temp);

        foreach (var (transform, entity) in SystemAPI.Query<RefRO<LocalTransform>>()
                                            .WithAll<SSALType>()
                                            .WithEntityAccess())
        {
            float3 pos = transform.ValueRO.Position;

            if (pos.x + padding >= xMin && pos.x - padding <= xMax &&
                pos.y + padding >= yMin && pos.y - padding <= yMax)
            {
                ecb.DestroyEntity(entity);
            }
        }

        ecb.Playback(EntityManager);
        ecb.Dispose();

        zoneData.RiceCount = 0;
        zoneData.SandCount = 0;
        zoneData.GrainCount = 0;

        SystemAPI.SetComponent(zoneEntity, zoneData);
    }
}