using System;
using Unity.Entities;

[Serializable]
[GenerateAuthoringComponent]
public struct AttackState : IComponentData {
  public float TimeRemaining;
}
