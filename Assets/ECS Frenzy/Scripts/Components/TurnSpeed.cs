using System;
using Unity.Entities;

[Serializable]
[GenerateAuthoringComponent]
public struct TurnSpeed : IComponentData {
  public float Value;
}