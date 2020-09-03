using System;
using Unity.Entities;

[Serializable]
[GenerateAuthoringComponent]
public struct MoveSpeed : IComponentData {
  public float Value;
}