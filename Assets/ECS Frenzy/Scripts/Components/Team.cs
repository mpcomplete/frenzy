using System;
using Unity.Entities;
using Unity.NetCode;

namespace ECSFrenzy {
  [Serializable]
  [GenerateAuthoringComponent]
  public struct Team : IComponentData {
    [GhostField] public int Value;
  }
}