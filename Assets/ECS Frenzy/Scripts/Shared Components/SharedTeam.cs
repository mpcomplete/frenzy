using System;
using Unity.Entities;

namespace ECSFrenzy {
  [Serializable]
  [GenerateAuthoringComponent]
  public struct SharedTeam : ISharedComponentData {
    public int Value;
  }
}