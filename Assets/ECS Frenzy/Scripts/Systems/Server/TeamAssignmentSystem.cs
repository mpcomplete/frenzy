using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
using Unity.NetCode;

namespace ECSFrenzy {
  [UpdateInGroup(typeof(ServerSimulationSystemGroup))]
  public class TeamAssignmentSystem : ComponentSystem {
    public int currentTeamNumber = 0;

    int? IndexOfMatchingTeam(in NativeArray<Team> teams) {
      for (int i = 0; i < teams.Length; i++) {
        if (teams[i].Value == currentTeamNumber)
          return i;
      }
      return null;
    }

    protected override void OnUpdate() {
      EntityQuery stanchionQuery = Entities.WithAll<Stanchion, Team, LocalToWorld>().ToEntityQuery();
      EntityQuery spawnQuery = Entities.WithAll<SpawnLocation, Team, LocalToWorld>().ToEntityQuery();
      using (var stanchions = stanchionQuery.ToEntityArray(Allocator.TempJob))
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
            EntityManager.SetComponentData(e, new Team { Value = currentTeamNumber });
            EntityManager.AddSharedComponentData(e, new SharedTeam { Value = currentTeamNumber });

            // TODO: Maybe we should just spawn the stanchion here? (Instead of with the Player.)
            EntityManager.SetComponentData(stanchions[spawnIndex.Value], new Team { Value = currentTeamNumber });
            EntityManager.SetComponentData(stanchions[spawnIndex.Value], new Translation { Value = transform.Position + 3*transform.Forward });

            currentTeamNumber = (currentTeamNumber + 1) % 2;
          } else {
            UnityEngine.Debug.LogError($"No valid Spawn Location found for Team Number {currentTeamNumber}!");
          }
        });
      }
    }
  }
}