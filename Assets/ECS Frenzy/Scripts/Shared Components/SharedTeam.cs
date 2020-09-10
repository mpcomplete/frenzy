using System;
using Unity.Entities;

namespace ECSFrenzy.SharedComponents {
  [Serializable]
  [GenerateAuthoringComponent]
  public struct SharedTeam : ISharedComponentData {
    public int Value;
  }
}