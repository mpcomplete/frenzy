using System;
using Unity.Entities;

namespace ECSFrenzy {
  [Serializable]
  [GenerateAuthoringComponent]
  public struct DeathTimer : IComponentData {
    public float TimeRemaining;
  }
}