using UnityEngine;
using Unity.Entities;

public class SSALTypeAuthoring : MonoBehaviour
{
    public SSALMode Type;

    class Baker : Baker<SSALTypeAuthoring>
    {
        public override void Bake(SSALTypeAuthoring authoring)
        {
// �ּ��Դϴ�.
            var entity = GetEntity(TransformUsageFlags.None);
            AddComponent(entity, new SSALType { Type = authoring.Type });
            // 머지할까요?
        }
    }
}
