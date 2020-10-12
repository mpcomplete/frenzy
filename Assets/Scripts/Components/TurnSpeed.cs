using System;
using Unity.Entities;
using Unity.NetCode;

[Serializable]
[GenerateAuthoringComponent]
public struct TurnSpeed : IComponentData {
  [GhostField] public float Value;
}