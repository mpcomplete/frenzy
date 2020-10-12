using System;
using Unity.Entities;

[Serializable]
[GenerateAuthoringComponent]
public struct AttackTarget : IComponentData {
  public Entity Value;
}
