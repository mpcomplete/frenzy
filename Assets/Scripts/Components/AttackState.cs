using System;
using Unity.Entities;

namespace ECSFrenzy {
  [Serializable]
  [GenerateAuthoringComponent]
  public struct AttackState : IComponentData {
    public float TimeRemaining;
  }
}