using System;
using Unity.Entities;

[Serializable]
[GenerateAuthoringComponent]
public struct NavTarget : IComponentData {
  public Entity Value;
}
