using System;
using Unity.Entities;

[Serializable]
[GenerateAuthoringComponent]
public struct SharedTeam : ISharedComponentData {
  public int Value;
}
