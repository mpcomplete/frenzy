using Unity.Entities;
using Unity.NetCode;

[GenerateAuthoringComponent]
[GhostComponent(PrefabType = GhostPrefabType.All)]
public struct FireballAbility : IComponentData {
  [GhostField] public int SpawnTick;
}
