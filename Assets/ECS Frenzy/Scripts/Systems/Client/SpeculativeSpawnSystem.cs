using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;

namespace ECSFrenzy {
  public struct SpeculativeBuffers : IComponentData {
    public uint OldestTickSimulated;
    public uint NewestTickSimulated;
    public SpeculativeBuffers (uint oldest, uint newest) {
      OldestTickSimulated = oldest;
      NewestTickSimulated = newest;
    }
    public static bool Matches(in SpeculativeSpawnBufferEntry spawn, in ExistingSpeculativeSpawnBufferEntry existing) {
      return spawn.SpawnTick == existing.SpawnTick && spawn.Identifier == existing.Identifier;
    }
  }

  public struct SpeculativeSpawnBufferEntry : IBufferElementData {
    public Entity Entity;
    public uint SpawnTick;
    public uint Identifier;
  }

  public struct ExistingSpeculativeSpawnBufferEntry : IBufferElementData {
    public Entity Entity;
    public uint SpawnTick;
    public uint Identifier;
    public static implicit operator ExistingSpeculativeSpawnBufferEntry(SpeculativeSpawnBufferEntry spec) {
      return new ExistingSpeculativeSpawnBufferEntry { SpawnTick = spec.SpawnTick, Identifier = spec.Identifier, Entity = spec.Entity };
    }
  }

  // TODO: This really only should run on a server... goddamn Ghosts are annoying
  [UpdateInGroup(typeof(GhostSimulationSystemGroup))]
  [UpdateAfter(typeof(GhostPredictionSystemGroup))]
  public class SpeculativeSpawnSystem : SystemBase {
    GhostPredictionSystemGroup PredictionSystemGroup;

    protected override void OnCreate() {
      var e = EntityManager.CreateEntity();

      EntityManager.SetName(e, "SPECULATIVE BUFFER SINGLETON");
      EntityManager.AddComponent<SpeculativeBuffers>(e);
      EntityManager.SetComponentData(e, new SpeculativeBuffers(uint.MaxValue, 0)); // initial value since we use min function each frame
      EntityManager.AddBuffer<SpeculativeSpawnBufferEntry>(e);
      EntityManager.AddBuffer<ExistingSpeculativeSpawnBufferEntry>(e);
      PredictionSystemGroup = World.GetExistingSystem<GhostPredictionSystemGroup>();
    }

    protected override void OnUpdate() {
      if (World.GetExistingSystem<ServerSimulationSystemGroup>() != null)
        return;

      var predictingTick = PredictionSystemGroup.PredictingTick;  
      var singletonEntity = GetSingletonEntity<SpeculativeBuffers>();
      var speculativeBuffers = GetComponent<SpeculativeBuffers>(singletonEntity);
      var existing = GetBuffer<ExistingSpeculativeSpawnBufferEntry>(singletonEntity);
      var speculative = GetBuffer<SpeculativeSpawnBufferEntry>(singletonEntity);
      var oldestTickSimulated = speculativeBuffers.OldestTickSimulated;
      var newestTickSimulated = speculativeBuffers.NewestTickSimulated;
      var ecb = new EntityCommandBuffer(Allocator.TempJob, PlaybackPolicy.SinglePlayback);

      // UnityEngine.Debug.Log($"{ oldestTickSimulated }..{newestTickSimulated}");
      Job
      .WithCode(()=> {
        // Remove existing entities that are old enough to not be re-simulated
        for (int i = existing.Length - 1; i >= 0; i--) {
          if (existing[i].SpawnTick < oldestTickSimulated) {
            // UnityEngine.Debug.Log("Removed existing entity from tracking");
            existing.RemoveAt(i);
          }
        }

        // Loop over existing entities and remove/destroy them if they are not found in the speculative spawns
        for (int i = existing.Length - 1; i >= 0; i--) {
          bool foundMatch = false;
          for (int j = 0; j < speculative.Length; j++) {
            foundMatch = foundMatch || SpeculativeBuffers.Matches(speculative[j], existing[i]);
          }

          if (!foundMatch) {
            // UnityEngine.Debug.Log("Destroyed erroneous existing entity");
            ecb.DestroyEntity(existing[i].Entity);
            existing.RemoveAt(i);
          }
        }

        // Loop over speculativeSpawns and move to existing if they are not already there otherwise destroy
        for (int i = speculative.Length - 1; i >= 0; i--) {
          bool foundMatch = false;
          for (int j = 0; j < existing.Length; j++) {
            foundMatch = foundMatch || SpeculativeBuffers.Matches(speculative[i], existing[j]);
          }
          if (foundMatch) {
            // UnityEngine.Debug.Log("Removed redundant speculative entity");
            ecb.DestroyEntity(speculative[i].Entity);
            speculative.RemoveAt(i);
          } else {
            // UnityEngine.Debug.Log("Converted speculative to existing entity");
            existing.Add(speculative[i]);
            speculative.RemoveAt(i);
          }
        }

        // reset the oldest tick each frame ... maybe a better way to do this?
        ecb.SetComponent(singletonEntity, new SpeculativeBuffers(uint.MaxValue, 0));
      })
      .WithoutBurst()
      .Run();

      ecb.Playback(EntityManager);
      ecb.Dispose();
    }
  }
}