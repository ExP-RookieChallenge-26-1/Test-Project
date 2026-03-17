using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public partial class ClickSpawnSystem : SystemBase
{
    float timer = 0;

    protected override void OnUpdate()
    {
        if (SSALManager.Instance == null || !SSALManager.Instance.Active) return;
        if (!SystemAPI.HasSingleton<SpawnerData>() || !SystemAPI.HasSingleton<SSALCupData>()) return;

        var mouse = Mouse.current;
        if (mouse == null || !mouse.leftButton.isPressed) return;
        if (Camera.main == null) return;

        var spawnerData = SystemAPI.GetSingleton<SpawnerData>();
        var zoneData = SystemAPI.GetSingleton<SSALCupData>();

        timer += SystemAPI.Time.DeltaTime;
        float spawnInterval = 1f / spawnerData.SpawnAmount;

        var ecb = new EntityCommandBuffer(Unity.Collections.Allocator.Temp);

        while (timer >= spawnInterval)
        {
            timer -= spawnInterval;

            Vector2 mouseScreenPos = mouse.position.ReadValue();
            Vector3 screenPos3 = new Vector3(mouseScreenPos.x, mouseScreenPos.y, -Camera.main.transform.position.z);
            Vector3 worldCenterPos = Camera.main.ScreenToWorldPoint(screenPos3);

            float xMin = zoneData.SpawnCenter.x - (zoneData.SpawnSize.x / 2);
            float xMax = zoneData.SpawnCenter.x + (zoneData.SpawnSize.x / 2);
            float yMin = zoneData.SpawnCenter.y - (zoneData.SpawnSize.y / 2);
            float yMax = zoneData.SpawnCenter.y + (zoneData.SpawnSize.y / 2);

            if (worldCenterPos.x < xMin || worldCenterPos.x > xMax || worldCenterPos.y < yMin || worldCenterPos.y > yMax)
                continue;

            Vector2 randomOffset = UnityEngine.Random.insideUnitCircle * spawnerData.Range;
            float3 finalPos = new float3(worldCenterPos.x + randomOffset.x, worldCenterPos.y + randomOffset.y, 0);
            quaternion randomRot = quaternion.Euler(0, 0, math.radians(UnityEngine.Random.Range(0f, 360f)));

            Entity prefab = GetRandomPrefab(spawnerData);
            if (prefab != Entity.Null)
            {
                Entity newEntity = ecb.Instantiate(prefab);
                ecb.SetComponent(newEntity, LocalTransform.FromPositionRotation(finalPos, randomRot));
                ecb.AddComponent(newEntity, new LifeTimeData { Value = 5.0f });
            }
        }

        ecb.Playback(EntityManager);
        ecb.Dispose();
    }

    private Entity GetRandomPrefab(SpawnerData data)
    {
        int ssalNum = UnityEngine.Random.Range(0, 3);
        switch (data.CurrentMode)
        {
            case SSALMode.Rice:
                if (SSALManager.Instance.Rice <= 0) return Entity.Null;
                SSALManager.Instance.Rice--;
                return ssalNum == 0 ? data.Rice1Entity : (ssalNum == 1 ? data.Rice2Entity : data.Rice3Entity);

            case SSALMode.Sand:
                return ssalNum == 0 ? data.Sand1Entity : (ssalNum == 1 ? data.Sand2Entity : data.Sand3Entity);

            case SSALMode.Grain:
                if (SSALManager.Instance.Grain <= 0) return Entity.Null;
                SSALManager.Instance.Grain--;
                return ssalNum == 0 ? data.Grain1Entity : (ssalNum == 1 ? data.Grain2Entity : data.Grain3Entity);
        }
        return Entity.Null;
    }
}
