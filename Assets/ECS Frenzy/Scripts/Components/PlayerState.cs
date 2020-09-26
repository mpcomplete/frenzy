using System;
using Unity.Entities;
using Unity.NetCode;

namespace ECSFrenzy {
  [Serializable]
  [GenerateAuthoringComponent]
  [GhostComponent(PrefabType = GhostPrefabType.All)]
  public struct PlayerState : IComponentData {
    [GhostField] public float FireballCooldown;
    [GhostField] public bool IsMoving;
    [GhostField] public bool DidFireball;
    [GhostField] public bool DidBanner;
  }
}