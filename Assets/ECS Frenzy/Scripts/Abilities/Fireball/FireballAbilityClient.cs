using Unity.Entities;
using Unity.NetCode;

namespace ECSFrenzy {
  [GenerateAuthoringComponent]
  [GhostComponent(PrefabType=GhostPrefabType.Client)]
  public struct FireballAbilityClient : IComponentData {
    public Entity castingSoundEntity;
    public Entity fireballEntity;

    public static void Evaluate(uint tick) {
      
    }
  }
}