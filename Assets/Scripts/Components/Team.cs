using System;
using Unity.Entities;
using Unity.NetCode;

[Serializable]
[GenerateAuthoringComponent]
public struct Team : IComponentData {
  [GhostField] public int Value;
}
