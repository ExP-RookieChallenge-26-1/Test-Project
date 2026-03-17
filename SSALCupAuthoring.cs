using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;

public class SSALCupAuthoring : MonoBehaviour
{
    class Baker : Baker<SSALCupAuthoring>
    {
        public override void Bake(SSALCupAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);

            float2 countSize = new float2(
                authoring.transform.GetChild(0).lossyScale.x,
                authoring.transform.GetChild(0).lossyScale.y
            );
            float2 spawnSize = new float2(
                authoring.transform.GetChild(2).lossyScale.x,
                authoring.transform.GetChild(2).lossyScale.y
            );
            float2 cupSize = new float2(
                authoring.transform.GetChild(1).lossyScale.x,
                authoring.transform.GetChild(1).lossyScale.y
            );

            AddComponent(entity, new SSALCupData
            {
                CountSize = countSize,
                SpawnSize = spawnSize,
                CupSize = cupSize,
                SpawnCenter = authoring.transform.GetChild(2).position,

                RiceCount = 0,
                SandCount = 0,
                GrainCount = 0
            });
        }
    }
}

public struct SSALCupData : IComponentData
{
    public float2 CupSize;
    public float2 CountSize;
    public float2 SpawnSize;
    public float3 SpawnCenter;

    public int RiceCount;
    public int SandCount;
    public int GrainCount;

    public int TopTierCount;
    public int BottomTierCount;
    public float CutlineY;
}
