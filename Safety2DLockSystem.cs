using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Transforms;

[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
[UpdateAfter(typeof(PhysicsSystemGroup))]
[BurstCompile]
public partial struct Safety2DLockSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        // IJobEntity를 사용하여 워커 스레드에서 병렬 처리
        var lockJob = new SafetyLockJob();
        lockJob.ScheduleParallel(); // Schedule 대신 ScheduleParallel 사용
    }
}

[BurstCompile]
public partial struct SafetyLockJob : IJobEntity
{
    // 쿼리 조건을 한 번에 처리 (LocalTransform, PhysicsVelocity, PhysicsMass를 모두 가진 엔티티)
    public void Execute(ref LocalTransform transform, ref PhysicsVelocity velocity, ref PhysicsMass mass)
    {
        // 1. 위치 및 회전 잠금
        if (math.abs(transform.Position.z) > 0.001f)
        {
            transform.Position.z = 0;
        }

        float4 q = transform.Rotation.value;
        if (math.abs(q.x) > 0.001f || math.abs(q.y) > 0.001f)
        {
            quaternion newRot = new quaternion(0, 0, q.z, q.w);
            transform.Rotation = math.lengthsq(newRot.value) > float.Epsilon
                ? math.normalize(newRot)
                : quaternion.identity;
        }

        // 2. 속도 잠금
        velocity.Linear.z = 0;
        velocity.Angular.x = 0;
        velocity.Angular.y = 0;

        // 3. 관성 모멘트 잠금
        mass.InverseInertia.x = 0;
        mass.InverseInertia.y = 0;
        if (mass.InverseInertia.z == 0)
        {
            mass.InverseInertia.z = 1.0f;
        }
    }
}