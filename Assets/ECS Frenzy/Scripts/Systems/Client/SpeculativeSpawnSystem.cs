#define SHOW_SPECULATIVE_DEBUGGING

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
    ClientSimulationSystemGroup ClientSimulationSystemGroup;

    protected override void OnCreate() {
      var e = EntityManager.CreateEntity();

      EntityManager.SetName(e, "SPECULATIVE BUFFER SINGLETON");
      EntityManager.AddComponent<SpeculativeBuffers>(e);
      EntityManager.SetComponentData(e, new SpeculativeBuffers(uint.MaxValue, 0)); // initial value since we use min function each frame
      EntityManager.AddBuffer<SpeculativeSpawnBufferEntry>(e);
      EntityManager.AddBuffer<ExistingSpeculativeSpawnBufferEntry>(e);
      PredictionSystemGroup = World.GetExistingSystem<GhostPredictionSystemGroup>();
      ClientSimulationSystemGroup = World.GetExistingSystem<ClientSimulationSystemGroup>();
    }

    protected override void OnUpdate() {
      // TODO: It would be nice to truly run this only on the client but there are a few odd details that would need to get cleaned up in the InputPredictionSystem to do that
      // To do this would require changing the way speculative spawns work to include a SpeculativelySpawnedTagComponent which then would get queried for in this system and 
      // ultimately added to the bookkeeping already defined here. This would allow the Prefab for any speculatively-spawned thing to simply include the tag and then all the rest
      // of the magic would just happen...
      if (World.GetExistingSystem<ServerSimulationSystemGroup>() != null)
        return;

      var predictingTick = PredictionSystemGroup.PredictingTick;  
      var serverTick = ClientSimulationSystemGroup.ServerTick;
      var singletonEntity = GetSingletonEntity<SpeculativeBuffers>();
      var speculativeBuffers = GetComponent<SpeculativeBuffers>(singletonEntity);
      var existing = GetBuffer<ExistingSpeculativeSpawnBufferEntry>(singletonEntity);
      var speculative = GetBuffer<SpeculativeSpawnBufferEntry>(singletonEntity);
      var oldestTickSimulated = speculativeBuffers.OldestTickSimulated;
      var newestTickSimulated = speculativeBuffers.NewestTickSimulated;
      var ecb = new EntityCommandBuffer(Allocator.TempJob, PlaybackPolicy.SinglePlayback);

      #if SHOW_SPECULATIVE_DEBUGGING
      UnityEngine.Debug.Log($"{ oldestTickSimulated }..{newestTickSimulated}");
      #endif

      Job
      .WithCode(()=> {
        // Loop over existing entities and remove/destroy them if they are not found in the speculative spawns
        // AND the frame they were spawned on was re-simulated during this frame!
        for (int i = existing.Length - 1; i >= 0; i--) {
          bool foundMatch = false;
          bool resimulatedThisFrame = existing[i].SpawnTick >= oldestTickSimulated && existing[i].SpawnTick <= newestTickSimulated;

          for (int j = 0; j < speculative.Length; j++) {
            foundMatch = foundMatch || SpeculativeBuffers.Matches(speculative[j], existing[i]);
          }

          if (resimulatedThisFrame && !foundMatch) {
            #if SHOW_SPECULATIVE_DEBUGGING
            UnityEngine.Debug.Log($"Destroyed erroneous existing entity {existing[i].Identifier}. {speculative.Length} Speculated entities were in the buffer.");
            #endif
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
            #if SHOW_SPECULATIVE_DEBUGGING
            UnityEngine.Debug.Log($"Removed redundant speculative entity {speculative[i].Identifier} on estimated server tick {serverTick} with {speculative.Length} elements in speculativeSpawnBuffer");
            #endif
            ecb.DestroyEntity(speculative[i].Entity);
            speculative.RemoveAt(i);
          } else {
            #if SHOW_SPECULATIVE_DEBUGGING
            UnityEngine.Debug.Log($"Converted speculative to existing entity {speculative[i].Identifier}");
            #endif
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