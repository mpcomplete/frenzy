using System;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;

[Serializable]
[GenerateAuthoringComponent]
public struct Heading : IComponentData {
  public float3 Value;
}