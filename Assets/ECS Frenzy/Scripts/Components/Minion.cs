using System;
using Unity.Entities;
using Unity.NetCode;
using Unity.Transforms;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Collections;
using UnityEngine;
using Unity.Physics.Systems;

namespace ECSFrenzy {
  [Serializable]
  [GenerateAuthoringComponent]
  public struct Minion : IComponentData {
    public enum Behavior { Idle, OnBanner, Fighting }
    public Behavior CurrentBehavior;
  }

  [UpdateInGroup(typeof(ServerSimulationSystemGroup))]
  public class MinionAISystem : SystemBase {
    BeginSimulationEntityCommandBufferSystem commandBufferSystem;
    BuildPhysicsWorld physicsWorldSystem;
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
      physicsWorldSystem = World.GetExistingSystem<BuildPhysicsWorld>();
    }

    protected override void OnUpdate() {
      var ecb = commandBufferSystem.CreateCommandBuffer().AsParallelWriter();
      var collisionWorld = physicsWorldSystem.PhysicsWorld.CollisionWorld;

      // Minions with no nav target, head to the banner.
      var query = GetEntityQuery(typeof(Banner), typeof(Team));
      var banners = query.ToEntityArray(Allocator.TempJob);
      var bannerTeams = query.ToComponentDataArray<Team>(Allocator.TempJob);
      Entities
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
          ecb.AddComponent(nativeThreadIndex, e, new MinionState { TargetFindCooldown = .1f });
        }
      }).ScheduleParallel();

      // Minions with no attack target, try to find one and path to it.
      var entityTransforms = GetComponentDataFromEntity<LocalToWorld>(true);
      var entityHealth = GetComponentDataFromEntity<Health>(false);
      float dt = Time.DeltaTime;
      Entities
      .WithAll<NavTarget>()
      .WithNone<AttackTarget>()
      .WithReadOnly(entityHealth)
      .ForEach((Entity e, int nativeThreadIndex, ref MinionState minionState, ref Team team, ref LocalToWorld transform) => {
        minionState.TargetFindCooldown -= dt;
        if (minionState.TargetFindCooldown > 0f)
          return;
        minionState.TargetFindCooldown = .1f;
        Entity target = FindTarget(collisionWorld, transform.Position, team, entityHealth);
        if (target != Entity.Null) {
          ecb.SetComponent(nativeThreadIndex, e, new NavTarget { Value = target });
          ecb.AddComponent(nativeThreadIndex, e, new AttackTarget { Value = target });
          ecb.AddComponent(nativeThreadIndex, e, new AttackState { TimeRemaining = 0f });
        }
      }).ScheduleParallel();

      // Minions with an attack target, try to attack it.
      Entities
      .WithAll<AttackTarget>()
      .WithReadOnly(entityTransforms)
      .ForEach((Entity e, int nativeThreadIndex, ref Minion minion, ref Team team, ref AttackTarget target, ref AttackState state) => {
        if (math.distancesq(entityTransforms[target.Value].Position, entityTransforms[e].Position) < MinionAttackRange*MinionAttackRange) {
          // attack
          ecb.SetComponent(nativeThreadIndex, e, new NavTarget { Value = e });
          if (state.TimeRemaining <= 0f) {
            state.TimeRemaining = MinionAttackCooldown;
            entityHealth[target.Value] = new Health { Value = entityHealth[target.Value].Value - MinionAttackDamage };
            //Debug.Log($"Dealing dmg {target.Value}; hp = {entityHealth[target.Value].Value}");
          }
          state.TimeRemaining -= dt;
        } else {
          ecb.SetComponent(nativeThreadIndex, e, new NavTarget { Value = target.Value });
        }
        if (entityHealth[target.Value].Value <= 0f) {
          ecb.RemoveComponent<NavTarget>(nativeThreadIndex, e);
          ecb.RemoveComponent<AttackTarget>(nativeThreadIndex, e);
        }
      }).Schedule();
    }

    static Entity FindTarget(in CollisionWorld collisionWorld, float3 pos, in Team team, in ComponentDataFromEntity<Health> entityHealth) {
      (float distsq, Entity e) closest = (float.MaxValue, Entity.Null);
      var hits = new NativeList<DistanceHit>(8, Allocator.Temp);
      // Collide with opposite team.
      uint layer = team.Value == 0 ? CollisionLayer.Team2 : CollisionLayer.Team1;
      var fromPoint = new PointDistanceInput() { Filter = new CollisionFilter { BelongsTo = layer, CollidesWith = layer }, Position = pos, MaxDistance = MinionAggroRange };
      if (collisionWorld.CalculateDistance(fromPoint, ref hits)) {
        for (int i = 0; i < hits.Length; i++) {
          var hit = hits[i];  // TODO: isn't random-access on a list slow?
          Entity e = hit.Entity;
          float distsq = math.distancesq(hit.Position, pos);
          //Debug.Assert(EntityManager.HasComponent<Team>(e), $"Minion target collided invalid object: {EntityManager.GetName(e)}.");
          //Debug.Assert(EntityManager.GetComponentData<Team>(e).Value != team.Value, "Minion target collided with own team. CollisionFilters are incorrect.");
          if (distsq < closest.distsq && entityHealth.HasComponent(e) && entityHealth[e].Value > 0) {
            closest = (distsq, e);
          }
        }
      }
      if (hits.IsCreated)
        hits.Dispose();
      return closest.e;
    }
  }
}
