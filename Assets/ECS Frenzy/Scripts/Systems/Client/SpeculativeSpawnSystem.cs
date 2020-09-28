using System;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;

namespace ECSFrenzy {
  public struct SpeculativeBuffers : IComponentData {}

  public struct ExistingSpeculativeBuffers : IComponentData {}

  public struct SpeculativeSpawnBufferEntry : IBufferElementData {
    public Entity OwnerEntity;
    public Entity Entity;
    public uint SpawnTick;
    public uint Identifier;

    public bool SameAs(SpeculativeSpawnBufferEntry ssbe) {
      return OwnerEntity == ssbe.OwnerEntity && SpawnTick == ssbe.SpawnTick && Identifier == ssbe.Identifier;
    }
  }

  // TODO: This really only should run on a client... goddamn Ghosts are annoying
  // [UpdateInWorld(UpdateInWorld.TargetWorld.Client)]
  [UpdateInGroup(typeof(GhostSimulationSystemGroup))]
  [UpdateAfter(typeof(GhostPredictionSystemGroup))]
  public class SpeculativeSpawnSystem : SystemBase {
    GhostPredictionSystemGroup PredictionSystemGroup;
    ClientSimulationSystemGroup ClientSimulationSystemGroup;

    protected override void OnCreate() {
      var speculativeBuffersEntity = EntityManager.CreateEntity();
      var existingSpeculativeBuffersEntity = EntityManager.CreateEntity();

      EntityManager.SetName(speculativeBuffersEntity, "Speculative Buffers Singleton");
      EntityManager.AddComponent<SpeculativeBuffers>(speculativeBuffersEntity);
      EntityManager.AddBuffer<SpeculativeSpawnBufferEntry>(speculativeBuffersEntity);
      EntityManager.SetName(existingSpeculativeBuffersEntity, "Existing Speculative Buffers Singleton");
      EntityManager.AddComponent<ExistingSpeculativeBuffers>(existingSpeculativeBuffersEntity);
      EntityManager.AddBuffer<SpeculativeSpawnBufferEntry>(existingSpeculativeBuffersEntity);
      PredictionSystemGroup = World.GetExistingSystem<GhostPredictionSystemGroup>();
      ClientSimulationSystemGroup = World.GetExistingSystem<ClientSimulationSystemGroup>();
      RequireSingletonForUpdate<SpeculativeBuffers>();
      RequireSingletonForUpdate<ExistingSpeculativeBuffers>();
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
      var speculativeBuffersEntity = GetSingletonEntity<SpeculativeBuffers>();
      var existingSpeculativeBuffersEntity = GetSingletonEntity<ExistingSpeculativeBuffers>();
      var speculative = GetBuffer<SpeculativeSpawnBufferEntry>(speculativeBuffersEntity);
      var existing = GetBuffer<SpeculativeSpawnBufferEntry>(existingSpeculativeBuffersEntity);
      var predictedGhosts = GetComponentDataFromEntity<PredictedGhostComponent>(true);
      var ecb = new EntityCommandBuffer(Allocator.TempJob, PlaybackPolicy.SinglePlayback);

      Job
      .WithCode(()=> {
        // Loop over existing entities and remove/destroy them if they are not found in the speculative spawns and were re-simulated
        for (int i = existing.Length - 1; i >= 0; i--) {
          var e = existing[i];
          var predictedGhost = predictedGhosts[e.OwnerEntity];
          var foundMatch = false;
          var resimulatedThisFrame = e.SpawnTick > predictedGhost.PredictionStartTick;

          for (int j = 0; j < speculative.Length; j++) {
            foundMatch = foundMatch || speculative[j].SameAs(e);
          }

          if (resimulatedThisFrame && !foundMatch) {
            ecb.DestroyEntity(e.Entity);
            existing.RemoveAt(i);
          }
        }

        // Loop over speculativeSpawns and move to existing if they are not already there otherwise destroy
        for (int i = speculative.Length - 1; i >= 0; i--) {
          var foundMatch = false;

          for (int j = 0; j < existing.Length; j++) {
            foundMatch = foundMatch || speculative[i].SameAs(existing[j]);
          }
          if (foundMatch) {
            ecb.DestroyEntity(speculative[i].Entity);
            speculative.RemoveAt(i);
          } else {
            existing.Add(speculative[i]);
            speculative.RemoveAt(i);
          }
        }
      })
      .WithReadOnly(predictedGhosts)
      .WithoutBurst()
      .Run();

      ecb.Playback(EntityManager);
      ecb.Dispose();
    }
  }
}