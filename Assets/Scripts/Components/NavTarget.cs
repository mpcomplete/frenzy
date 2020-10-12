using System;
using Unity.Entities;

namespace ECSFrenzy {
  [Serializable]
  [GenerateAuthoringComponent]
  public struct NavTarget : IComponentData {
    public Entity Value;
  }
}