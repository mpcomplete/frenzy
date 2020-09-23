using System;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using static Unity.Mathematics.math;

[Serializable]
[GenerateAuthoringComponent]
public struct Heading : IComponentData {
  [GhostField] public float3 Value;
  public static implicit operator Heading(float3 v) => new Heading { Value = v };
  public static implicit operator Heading(quaternion q) => new Heading { Value = forward(q) };
}