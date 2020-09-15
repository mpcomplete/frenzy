using System;
using Unity.Entities;

namespace ECSFrenzy {
  [Serializable]
  [GenerateAuthoringComponent]
  public struct Target : IComponentData {
    public Entity Value;
  }
}