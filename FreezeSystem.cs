using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;
using static UnityEditor.PlayerSettings;

public struct LifeTimeData : IComponentData
{
    public float Value;
}
public struct TriggerUITag : IComponentData { }

[BurstCompile]
public partial struct TimeFreezeSystem : ISystem
{
    const float TargetHeightPercent = 50f;//50.0f;

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        if (SSALManager.Instance == null || !SSALManager.Instance.Active) return;
        float dt = SystemAPI.Time.DeltaTime;

        var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
        var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);
        float targetY = 9999f;
        float cupTopY = 9999f;
        Entity cupEntity = Entity.Null;

        float xMin = 0;
        float xMax = 0;

        if (SystemAPI.HasSingleton<SSALCupData>())
        {
            cupEntity = SystemAPI.GetSingletonEntity<SSALCupData>();
            var cupData = SystemAPI.GetComponent<SSALCupData>(cupEntity);
            var cupTransform = SystemAPI.GetComponent<LocalTransform>(cupEntity);
            float cupBottomY = cupTransform.Position.y - (cupData.CupSize.y * 0.5f);
            targetY = cupBottomY + (cupData.CupSize.y * (TargetHeightPercent / 100f));
            cupTopY = cupBottomY + (cupData.CupSize.y * ((TargetHeightPercent + 5) / 100f));
            float xPadding = 1f;
            xMin = cupData.SpawnCenter.x - (cupData.CountSize.x / 2f) - xPadding;
            xMax = cupData.SpawnCenter.x + (cupData.CountSize.x / 2f) + xPadding;
        }

        foreach (var (timer, transform, entity) in SystemAPI.Query<RefRW<LifeTimeData>, RefRO<LocalTransform>>()
                                            .WithAll<PhysicsVelocity>()
                                            .WithEntityAccess())
        {
            if (transform.ValueRO.Position.x >= xMin && transform.ValueRO.Position.x <= xMax)
            {
                timer.ValueRW.Value -= dt;
            }
            else
                timer.ValueRW.Value -= dt * 0.3f;

            if (timer.ValueRW.Value <= 0)
            {
                ecb.RemoveComponent<PhysicsVelocity>(entity);
                ecb.RemoveComponent<PhysicsMass>(entity);

                ecb.RemoveComponent<LifeTimeData>(entity);
                if (transform.ValueRO.Position.y >= targetY && transform.ValueRO.Position.y <= cupTopY)
                {
                    ecb.AddComponent<TriggerUITag>(cupEntity);
                }
            }
        }
    }
}