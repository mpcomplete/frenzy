﻿using System;
using Unity.Entities;

[Serializable]
[GenerateAuthoringComponent]
public struct SpawnLocation : IComponentData {
  public ushort TeamNumber;
}