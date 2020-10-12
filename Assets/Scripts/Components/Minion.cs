using System;
using Unity.Entities;
using Unity.NetCode;
using Unity.Transforms;
using Unity.Mathematics;
using Unity.Collections;

[Serializable]
[GenerateAuthoringComponent]
public struct Minion : IComponentData {
  public enum Behavior { Idle, OnBanner, Fighting }
  public Behavior CurrentBehavior;
}

[UpdateInGroup(typeof(ServerSimulationSystemGroup))]
public class MinionAISystem : SystemBase {
  BeginSimulationEntityCommandBufferSystem commandBufferSystem;
  public struct MinionState : IComponentData {
    public float TargetFindCooldown;  // shouldn't need this, but target finding is slow AF.
  }

  // TODO: move all these
  const float MinionAggroRange = 5f;
  const float MinionAttackRange = 2f;
  const float MinionAttackCooldown = .5f;
  const float MinionAttackDamage = 1f;

  protected override void OnCreate() {
    commandBufferSystem = World.GetExistingSystem<BeginSimulationEntityCommandBufferSystem>();
  }

  protected override void OnUpdate() {
    var ecb = commandBufferSystem.CreateCommandBuffer().AsParallelWriter();

    // Minions with no nav target, head to the banner.
    var query = GetEntityQuery(typeof(Banner), typeof(Team));
    var banners = query.ToEntityArray(Allocator.TempJob);
    var bannerTeams = query.ToComponentDataArray<Team>(Allocator.TempJob);
    Entities
    .WithAll<Minion>()
    .WithNone<NavTarget>()
    .WithNone<AttackTarget>()
    .WithDisposeOnCompletion(banners)
    .WithDisposeOnCompletion(bannerTeams)
    .ForEach((Entity e, int nativeThreadIndex, ref Minion minion, ref Team team) => {
      Entity target = Entity.Null;
      for (int i = 0; i < bannerTeams.Length; i++) {
        if (bannerTeams[i].Value == team.Value)
          target = banners[i];
      }
      if (target != Entity.Null) {
        ecb.AddComponent(nativeThreadIndex, e, new NavTarget { Value = target });
      }
    }).ScheduleParallel();

    // Minions with no attack target, try to find one and path to it.
    var query2 = GetEntityQuery(typeof(Team), typeof(Health), typeof(LocalToWorld));
    var entities = query2.ToEntityArray(Allocator.TempJob);
    var entityTeam = query2.ToComponentDataArray<Team>(Allocator.TempJob);
    var entityHealth = query2.ToComponentDataArray<Health>(Allocator.TempJob);
    var entityTransform = query2.ToComponentDataArray<LocalToWorld>(Allocator.TempJob);
    float dt = Time.DeltaTime;

    Entity FindTarget(float3 pos, in Team team) {
      (float distsq, Entity e) closest = (MinionAggroRange*MinionAggroRange, Entity.Null);
      for (int i = 0; i < entities.Length; i++) {
        if (entityTeam[i].Value == team.Value || entityHealth[i].Value <= 0)
          continue;
        float distsq = math.distancesq(entityTransform[i].Position, pos);
        if (distsq < closest.distsq)
          closest = (distsq, entities[i]);
      }
      return closest.e;
    }

    Entities
    .WithAll<Minion>()
    .WithAll<NavTarget>()
    .WithNone<AttackTarget>()
    .WithDisposeOnCompletion(entities)
    .WithDisposeOnCompletion(entityTeam)
    .WithDisposeOnCompletion(entityHealth)
    .WithDisposeOnCompletion(entityTransform)
    .ForEach((Entity e, int nativeThreadIndex, ref Team team, ref LocalToWorld transform) => {
      Entity target = FindTarget(transform.Position, team);
      if (target != Entity.Null) {
        ecb.SetComponent(nativeThreadIndex, e, new NavTarget { Value = target });
        ecb.AddComponent(nativeThreadIndex, e, new AttackTarget { Value = target });
        ecb.AddComponent(nativeThreadIndex, e, new AttackState { TimeRemaining = 0f });
      }
    }).ScheduleParallel();

    // Minions with an attack target, try to attack it.
    var entityTransforms = GetComponentDataFromEntity<LocalToWorld>(true);
    var entityHealths = GetComponentDataFromEntity<Health>(false);
    Entities
    .WithAll<AttackTarget>()
    .WithReadOnly(entityTransforms)
    .ForEach((Entity e, int nativeThreadIndex, ref Minion minion, ref Team team, ref AttackTarget target, ref AttackState state) => {
      if (math.distancesq(entityTransforms[target.Value].Position, entityTransforms[e].Position) < MinionAttackRange*MinionAttackRange) {
          // attack
          ecb.SetComponent(nativeThreadIndex, e, new NavTarget { Value = e });
        if (state.TimeRemaining <= 0f) {
          state.TimeRemaining = MinionAttackCooldown;
          entityHealths[target.Value] = new Health { Value = entityHealths[target.Value].Value - MinionAttackDamage };
            //Debug.Log($"Dealing dmg {target.Value}; hp = {entityHealth[target.Value].Value}");
          }
        state.TimeRemaining -= dt;
      } else {
        ecb.SetComponent(nativeThreadIndex, e, new NavTarget { Value = target.Value });
      }
      if (entityHealths[target.Value].Value <= 0f) {
        ecb.RemoveComponent<NavTarget>(nativeThreadIndex, e);
        ecb.RemoveComponent<AttackTarget>(nativeThreadIndex, e);
      }
    }).Schedule();
  }
}
