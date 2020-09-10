using System;
using Unity.Entities;

namespace ECSFrenzy.Components {
  [Serializable]
  [GenerateAuthoringComponent]
  public struct Team : IComponentData {
    public int Value;
  }
}