using System;
using Unity.Entities;

[Serializable]
[GenerateAuthoringComponent]
public struct Health : IComponentData {
  public float Value;
}
