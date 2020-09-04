using System;
using Unity.Entities;
using Unity.NetCode;

[Serializable]
[GenerateAuthoringComponent]
[GhostComponent(PrefabType=GhostPrefabType.All)]
public struct MoveSpeed : IComponentData {
  public float Value;
}