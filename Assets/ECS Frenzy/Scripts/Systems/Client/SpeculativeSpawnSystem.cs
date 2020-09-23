using Unity.Entities;
using Unity.NetCode;

namespace ECSFrenzy {
  [UpdateInGroup(typeof(GhostPredictionSystemGroup))]
  public class SpeculativeSpawnSystem : SystemBase {
    GhostPredictionSystemGroup PredictionSystemGroup;

    protected override void OnCreate() {
      PredictionSystemGroup = World.GetExistingSystem<GhostPredictionSystemGroup>();
    }

    protected override void OnUpdate() {
      var predictingTick = PredictionSystemGroup.PredictingTick;  

      // UnityEngine.Debug.Log("-----------------------------------------------------------------");
      // UnityEngine.Debug.Log($"SpeculativeSpawnSystem fired on PredictingTick: {predictingTick}");

      Entities
      .ForEach((ref Entity e, in SpeculativeSpawn speculativeSpawn) => {
        if (predictingTick <= speculativeSpawn.SpawnTick) {
          UnityEngine.Debug.Log($"Predicting the past so destroyed");
          EntityManager.DestroyEntity(e);
        }

        // TODO: This is just here for testing convenience. Probably not an intended behavior
        if (speculativeSpawn.SpawnTick + 300 < predictingTick) {
          UnityEngine.Debug.Log($"Too old so destroyed");
          EntityManager.DestroyEntity(e);
        }
      }) 
      .WithStructuralChanges()
      .WithoutBurst() // TODO: conservative since I'm intending to mutate-in-place here.. could probably do this differently if needed
      .Run();
    }
  }
}