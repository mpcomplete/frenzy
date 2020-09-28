﻿using System;
using Unity.Entities;
using Unity.NetCode;

namespace ECSFrenzy {
  [Serializable]
  [GenerateAuthoringComponent]
  public struct DeathTimer : IComponentData {
    public float TimeRemaining;
  }
}