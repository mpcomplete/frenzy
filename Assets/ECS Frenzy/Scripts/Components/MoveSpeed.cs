using System;
using Unity.Entities;
using Unity.NetCode;

[Serializable]
[GenerateAuthoringComponent]
public struct MoveSpeed : IComponentData {
  [GhostField] public float Value;
}