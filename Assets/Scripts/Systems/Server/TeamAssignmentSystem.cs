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
    EntityQuery bannerQuery = Entities.WithAll<Banner, Team, LocalToWorld>().ToEntityQuery();
    EntityQuery spawnQuery = Entities.WithAll<SpawnLocation, Team, LocalToWorld>().ToEntityQuery();
    using (var banners = bannerQuery.ToEntityArray(Allocator.TempJob))
    using (var spawnLocations = spawnQuery.ToComponentDataArray<SpawnLocation>(Allocator.TempJob))
    using (var teams = spawnQuery.ToComponentDataArray<Team>(Allocator.TempJob))
    using (var spawnTransforms = spawnQuery.ToComponentDataArray<LocalToWorld>(Allocator.TempJob)) {
      Entities
      .WithNone<SharedTeam>()
      .WithAll<NetworkPlayer>()
      .ForEach((Entity e, ref Translation translation, ref Rotation rotation) => {
        int? spawnIndex = IndexOfMatchingTeam(teams);

        if (spawnIndex.HasValue) {
          var transform = spawnTransforms[spawnIndex.Value];
          EntityManager.SetComponentData(e, new Translation { Value = transform.Position });
          EntityManager.SetComponentData(e, new Rotation { Value = transform.Rotation });
          EntityManager.SetComponentData(e, new PhysicsCollider { Value = colliderForTeam[currentTeamNumber] });
          EntityManager.SetComponentData(e, new Team { Value = currentTeamNumber });
          EntityManager.AddSharedComponentData(e, new SharedTeam { Value = currentTeamNumber });

          // TODO: Maybe we should just spawn the banner here? (Instead of with the Player.)
          if (currentTeamNumber < banners.Length) {
            EntityManager.SetComponentData(banners[currentTeamNumber], new Team { Value = currentTeamNumber });
            EntityManager.SetComponentData(banners[currentTeamNumber], new Translation { Value = transform.Position + 3*transform.Forward });
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
