using UnityEngine;
using Unity.Entities;

public class SSALTypeAuthoring : MonoBehaviour
{
    public SSALMode Type;

    class Baker : Baker<SSALTypeAuthoring>
    {
        public override void Bake(SSALTypeAuthoring authoring)
        {
// 주석입니다.
            var entity = GetEntity(TransformUsageFlags.None);
            AddComponent(entity, new SSALType { Type = authoring.Type });
        }
    }
}