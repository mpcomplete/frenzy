using Unity.Entities;
using Unity.NetCode;

namespace ECSFrenzy {
  [GenerateAuthoringComponent]
  [GhostComponent(PrefabType=GhostPrefabType.Server)]
  public struct FireballAbilityServer : IComponentData {
    public static void Evaluate(uint tick) {

    }
  }
}