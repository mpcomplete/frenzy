using System;
using Unity.Entities;

namespace ECSFrenzy {
  [Serializable]
  [GenerateAuthoringComponent]
  public struct AttackTarget : IComponentData {
    public Entity Value;
  }
}