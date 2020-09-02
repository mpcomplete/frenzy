using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.NetCode;

[UpdateInGroup(typeof(ServerSimulationSystemGroup))]
public class TeamAssignmentSystem : ComponentSystem {
  public ushort currentTeamNumber = 0;

  static int? IndexOfValidSpawnLocationForTeam(in ushort currentTeam, in NativeArray<SpawnLocation> spawnLocations) {
    for (int i = 0; i < spawnLocations.Length; i++) {
      if (spawnLocations[i].TeamNumber == currentTeam) 
        return i;
    }
    return null;
  }

  protected override void OnUpdate() {
    EntityQuery spawnQuery = Entities.WithAll<SpawnLocation, Translation, Rotation>().ToEntityQuery();
    NativeArray<SpawnLocation> spawnLocations = spawnQuery.ToComponentDataArray<SpawnLocation>(Allocator.Temp);
    NativeArray<Translation> spawnTranslations = spawnQuery.ToComponentDataArray<Translation>(Allocator.Temp);
    NativeArray<Rotation> spawnRotations = spawnQuery.ToComponentDataArray<Rotation>(Allocator.Temp);

    Entities
    .WithNone<SharedTeam>()
    .WithAll<NetworkPlayer>()
    .ForEach((Entity e, ref Translation translation, ref Rotation rotation) => {
      int? validSpawnIndex = IndexOfValidSpawnLocationForTeam(currentTeamNumber, spawnLocations);

      if (validSpawnIndex.HasValue) {
        EntityManager.SetComponentData(e, spawnTranslations[validSpawnIndex.Value]);
        EntityManager.SetComponentData(e, spawnRotations[validSpawnIndex.Value]);
        EntityManager.AddSharedComponentData(e, new SharedTeam { Value = currentTeamNumber });
        currentTeamNumber = (currentTeamNumber == 0) ? (ushort)1 : (ushort)0;
      } else {
        UnityEngine.Debug.LogError($"No valid Spawn Location found for Team Number {currentTeamNumber}!");
      }
    });

    spawnLocations.Dispose();
    spawnTranslations.Dispose();
    spawnRotations.Dispose();
  }
}