using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

namespace ECSFrenzy {
  [UpdateInGroup(typeof(GhostPredictionSystemGroup))]
  public class SpeculativeSpawnSystem : SystemBase {
    GhostPredictionSystemGroup PredictionSystemGroup;

    protected override void OnCreate() {
      PredictionSystemGroup = World.GetExistingSystem<GhostPredictionSystemGroup>();
    }

    protected override void OnUpdate() {
      var predictingTick = PredictionSystemGroup.PredictingTick;  

      Entities
      .ForEach((Entity e, AudioSourceWrapper audioWrapper, in SpeculativeSpawn speculativeSpawn) => {
        if (predictingTick <= speculativeSpawn.SpawnTick) {
          EntityManager.DestroyEntity(e);
          AudioSource.Destroy(audioWrapper.Source.gameObject);
        }

        // TODO: This is just here for testing convenience. Probably not an intended behavior
        if (speculativeSpawn.SpawnTick + 300 < predictingTick) {
          EntityManager.DestroyEntity(e);
          AudioSource.Destroy(audioWrapper.Source.gameObject);
        }
      }) 
      .WithStructuralChanges()
      .WithoutBurst() // TODO: conservative since I'm intending to mutate-in-place here.. could probably do this differently if needed
      .Run();
    }
  }
}