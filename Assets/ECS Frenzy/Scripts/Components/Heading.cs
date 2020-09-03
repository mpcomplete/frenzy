﻿using System;
using Unity.Entities;
using Unity.Mathematics;

[Serializable]
[GenerateAuthoringComponent]
public struct Heading : IComponentData {
  public float3 Value;
}