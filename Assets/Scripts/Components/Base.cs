using System;
using System.Diagnostics;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Physics;
using Unity.Transforms;
//using UnityEngine;
using UnityEngine.AI;

[Serializable]
[GenerateAuthoringComponent]
public struct Base : IComponentData {
  [GhostField] public float SpawnCooldown;
  public float NextSpawnTime;
  public Entity SpawnLocation;
  public Entity MinionPrefab;
}

[UpdateInGroup(typeof(ServerSimulationSystemGroup))]
public class BaseSystem : ComponentSystem {
  BlobAssetReference<Collider>[] colliderForTeam;
  const int MaxMinions = 100;

  protected override void OnCreate() {
    colliderForTeam = new BlobAssetReference<Collider>[] {
        CollisionLayer.CreateCollider(0, CollisionLayer.Minion),
        CollisionLayer.CreateCollider(1, CollisionLayer.Minion),
      };
  }

  protected override void OnDestroy() {
    colliderForTeam[0].Dispose();
    colliderForTeam[1].Dispose();
  }

  protected override void OnUpdate() {
    int[] minionsOnTeam = { 0, 0 };
    var query = GetEntityQuery(typeof(Minion), typeof(Team));
    using (var minionTeams = query.ToComponentDataArray<Team>(Allocator.Temp)) {
      for (int i = 0; i < minionTeams.Length; i++) {
        int team = minionTeams[i].Value;
        minionsOnTeam[team]++;
      }
    }

    Entities.ForEach((ref Base spawner, ref Team team) => {
      if (Time.ElapsedTime < spawner.NextSpawnTime)
        return;
      if (minionsOnTeam[team.Value] > MaxMinions)
        return;

      Entity minion = EntityManager.Instantiate(spawner.MinionPrefab);
      var transform = EntityManager.GetComponentData<LocalToWorld>(spawner.SpawnLocation);
      float3 position = transform.Position;
      if (NavMesh.SamplePosition(position, out NavMeshHit hit, 10f, 1))
        position = hit.position;

      EntityManager.SetComponentData(minion, new Translation { Value = position });
      EntityManager.SetComponentData(minion, new Rotation { Value = transform.Rotation });
      EntityManager.AddComponentData(minion, new Heading { Value = transform.Forward });
      EntityManager.AddComponentData(minion, new Team { Value = team.Value });
      EntityManager.SetComponentData(minion, new PhysicsCollider { Value = colliderForTeam[team.Value] });
      EntityManager.RemoveComponent<NavTarget>(minion);
      EntityManager.RemoveComponent<AttackTarget>(minion);
      spawner.NextSpawnTime = (float)Time.ElapsedTime + spawner.SpawnCooldown;
    });
  }
}
