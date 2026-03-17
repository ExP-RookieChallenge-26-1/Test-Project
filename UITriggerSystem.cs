using Unity.Entities;

public partial class UITriggerSystem : SystemBase
{
    protected override void OnUpdate()
    {
        var ecb = new EntityCommandBuffer(Unity.Collections.Allocator.Temp);

        foreach (var (tag, entity) in SystemAPI.Query<RefRO<TriggerUITag>>().WithEntityAccess())
        {
            SSALManager.Instance.SetSubmitUI();
            ecb.RemoveComponent<TriggerUITag>(entity);
        }

        ecb.Playback(EntityManager);
        ecb.Dispose();
    }
}