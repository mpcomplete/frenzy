using Unity.Entities;
using Unity.NetCode;

namespace ECSFrenzy {
  [GenerateAuthoringComponent]
  [GhostComponent(PrefabType=GhostPrefabType.All)]
  public struct FireballAbility : IComponentData {
    public uint SpawnTick;
    public Entity ownerEntity;
  }
}