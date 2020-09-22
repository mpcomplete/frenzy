using Unity.Entities;
using Unity.NetCode;

namespace ECSFrenzy {
  [GenerateAuthoringComponent]
  [GhostComponent(PrefabType=GhostPrefabType.Client)]
  public struct FireballAbilityClient : IComponentData {
    public Entity SpawnSoundEntity;
    public Entity SpawnParticlesEntity;
  }
}