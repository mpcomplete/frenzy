using Unity.Entities;
using Unity.NetCode;

namespace ECSFrenzy {
  [UpdateInGroup(typeof(GhostPredictionSystemGroup))]
  public class FireballAbilitySystem : SystemBase {
    BeginSimulationEntityCommandBufferSystem CommandBufferSystem;
    GhostPredictionSystemGroup PredictionSystemGroup;
    EntityArchetype SoundArchetype;

    protected override void OnCreate() {
      CommandBufferSystem = World.GetExistingSystem<BeginSimulationEntityCommandBufferSystem>();
      PredictionSystemGroup = World.GetExistingSystem<GhostPredictionSystemGroup>();
      SoundArchetype = EntityManager.CreateArchetype(new ComponentType[] { 
        typeof(PlayAudio) 
      });
    }

    /*
    Programs can create new data
    Destroy existing data
    Update existing data

    We want to know what data should be modified ( easy )
                    what data should be created
                    what data should be destroyed
    
    For simplicity, we can model these programs as structures with a Run function
    that takes a tick as an argument. These programs ALWAYS run by being evaluated at
    a timestep.  Naively, they would just spawn instances of objects based on the logic
    of the program. However, instead we need to track the existence of spawned objects
    and use those instances when possible.

    Any entity that is spawned has a status: Predicted, Confirmed
    Confirmed entities have shown up in a snapshot from the server and should be modified
    or destroyed by future code. 
    Predicted entities have been speculatively spawned by the client and should only
    be kept around if a call to instantiate them is found when replaying prediction from the
    most recent server snapshot.
    */

    protected override void OnUpdate() {
      var dt = Time.DeltaTime;
      var predictingTick = PredictionSystemGroup.PredictingTick;
      var ecb = CommandBufferSystem.CreateCommandBuffer().AsParallelWriter();
      var soundArchetype = SoundArchetype;

      Entities
      .WithName("Server_Fireball")
      .ForEach((Entity e, int nativeThreadIndex, in FireballAbility fb, in FireballAbilityServer state, in PredictedGhostComponent predictedGhost) => {
      })
      .WithBurst()
      .ScheduleParallel();

      Entities
      .WithName("Client_Fireball")
      .WithAll<FireballAbility>()
      .ForEach((Entity e, int nativeThreadIndex, in FireballAbility fb, in FireballAbilityClient state, in PredictedGhostComponent predictedGhost) => {
        var spawnTickOffset = predictingTick - fb.SpawnTick;

        if (!GhostPredictionSystemGroup.ShouldPredict(predictingTick, predictedGhost)) 
          return;

        // uint spawnTickOffset = predictingTick - fb.SpawnTick;

        // UnityEngine.Debug.Log($"{spawnTickOffset}");

        // Look at the state, if there is no casting sound entity then create it
        // if (!state.playedSpawnSound) {
        //   var spawnSoundEntity = ecb.CreateEntity(nativeThreadIndex, soundArchetype);

        //   ecb.SetComponent<PlayAudio>(nativeThreadIndex, spawnSoundEntity, new PlayAudio("Fireball", 1f));
        //   ecb.SetComponent<FireballAbilityClient>(nativeThreadIndex, e, new FireballAbilityClient { playedSpawnSound = true });
        // }
      })
      .WithBurst()
      .ScheduleParallel();
      CommandBufferSystem.AddJobHandleForProducer(Dependency);
    }
  }
}