using System;
using Unity.Entities;
using Unity.NetCode;

[Serializable]
[GenerateAuthoringComponent]
public struct DeathTimer : IComponentData {
  public float TimeRemaining;
}
