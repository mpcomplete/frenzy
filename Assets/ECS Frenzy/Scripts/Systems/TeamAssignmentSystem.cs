using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.NetCode;
using ECSFrenzy.Components;
using ECSFrenzy.SharedComponents;

[UpdateInGroup(typeof(ServerSimulationSystemGroup))]
public class TeamAssignmentSystem : ComponentSystem {
  public ushort currentTeamNumber = 0;

  static int? IndexOfValidSpawnLocationForTeam(
  in ushort currentTeam, 
  in NativeArray<SpawnLocation> spawnLocations, 
  in NativeArray<Team> teams) {
    for (int i = 0; i < spawnLocations.Length; i++) {
      if (teams[i].Value == currentTeam) 
        return i;
    }
    return null;
  }

  protected override void OnUpdate() {
    EntityQuery spawnQuery = Entities.WithAll<SpawnLocation, Team, LocalToWorld>().ToEntityQuery();
    NativeArray<SpawnLocation> spawnLocations = spawnQuery.ToComponentDataArray<SpawnLocation>(Allocator.Temp);
    NativeArray<Team> teams = spawnQuery.ToComponentDataArray<Team>(Allocator.Temp);
    NativeArray<LocalToWorld> spawnTransforms = spawnQuery.ToComponentDataArray<LocalToWorld>(Allocator.Temp);

    Entities
    .WithNone<SharedTeam>()
    .WithAll<NetworkPlayer>()
    .ForEach((Entity e, ref Translation translation, ref Rotation rotation) => {
      int? validSpawnIndex = IndexOfValidSpawnLocationForTeam(currentTeamNumber, spawnLocations, teams);

      if (validSpawnIndex.HasValue) {
        EntityManager.SetComponentData(e, new Translation { Value = spawnTransforms[validSpawnIndex.Value].Position });
        EntityManager.SetComponentData(e, new Rotation { Value = spawnTransforms[validSpawnIndex.Value].Rotation });
        EntityManager.AddSharedComponentData(e, new SharedTeam { Value = currentTeamNumber });
        currentTeamNumber = (currentTeamNumber == 0) ? (ushort)1 : (ushort)0;
      } else {
        UnityEngine.Debug.LogError($"No valid Spawn Location found for Team Number {currentTeamNumber}!");
      }
    });

    spawnLocations.Dispose();
    teams.Dispose();
    spawnTransforms.Dispose();
  }
}