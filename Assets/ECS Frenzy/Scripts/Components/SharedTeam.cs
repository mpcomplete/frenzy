using System;
using Unity.Entities;

[Serializable]
public struct SharedTeam : ISharedComponentData {
  public ushort Value;
}