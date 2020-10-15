using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
using Unity.NetCode;
using Unity.Physics;
using Unity.Mathematics;

[UpdateInGroup(typeof(ServerSimulationSystemGroup))]
public class TeamAssignmentSystem : ComponentSystem {
  int currentTeamNumber = 0;
  BlobAssetReference<Collider>[] colliderForTeam;

  protected override void OnCreate() {
    colliderForTeam = new BlobAssetReference<Collider>[] {
      CollisionLayer.CreateCollider(0, CollisionLayer.Player),
      CollisionLayer.CreateCollider(1, CollisionLayer.Player),
    };
  }

  protected override void OnDestroy() {
    colliderForTeam[0].Dispose();
    colliderForTeam[1].Dispose();
  }

  protected override void OnUpdate() {
    EntityQuery bannerQuery = Entities.WithAll<Banner, Team>().ToEntityQuery();
    EntityQuery spawnerQuery = Entities.WithAll<Base, Team>().ToEntityQuery();
    var entityTransform = GetComponentDataFromEntity<LocalToWorld>(true);
    using (var banners = bannerQuery.ToEntityArray(Allocator.TempJob))
    using (var spawners = spawnerQuery.ToComponentDataArray<Base>(Allocator.TempJob))
    using (var teams = spawnerQuery.ToComponentDataArray<Team>(Allocator.TempJob)) {
      Entities
      .WithNone<SharedTeam>()
      .WithAll<NetworkPlayer>()
      .ForEach((Entity e) => {
        int? spawnIndex = IndexOfMatchingTeam(teams);

        if (spawnIndex.HasValue) {
          var spawner = spawners[spawnIndex.Value];
          var transform = entityTransform[spawner.SpawnLocation];
          PostUpdateCommands.SetComponent(e, new Translation { Value = transform.Position });
          PostUpdateCommands.SetComponent(e, new Rotation { Value = transform.Rotation });
          PostUpdateCommands.SetComponent(e, new PhysicsCollider { Value = colliderForTeam[currentTeamNumber] });
          PostUpdateCommands.SetComponent(e, new Team { Value = currentTeamNumber });
          PostUpdateCommands.AddSharedComponent(e, new SharedTeam { Value = currentTeamNumber });

          // TODO: Maybe we should just spawn the banner here? (Instead of with the Player.)
          if (currentTeamNumber < banners.Length) {
            PostUpdateCommands.SetComponent(banners[currentTeamNumber], new Team { Value = currentTeamNumber });
            PostUpdateCommands.SetComponent(banners[currentTeamNumber], new Translation { Value = transform.Position + 3*transform.Forward });
          }

          currentTeamNumber = (currentTeamNumber + 1) % 2;
        } else {
          UnityEngine.Debug.LogError($"No valid Spawn Location found for Team Number {currentTeamNumber}!");
        }
      });
    }
  }

  int? IndexOfMatchingTeam(in NativeArray<Team> teams) {
    for (int i = 0; i < teams.Length; i++) {
      if (teams[i].Value == currentTeamNumber)
        return i;
    }
    return null;
  }
}
