using System;
using Unity.Entities;

namespace ECSFrenzy {
  [Serializable]
  [GenerateAuthoringComponent]
  public struct Health : IComponentData {
    public float Value;
  }
}