using System;
using Unity.Entities;
using UnityEngine;

[Serializable]
[GenerateAuthoringComponent]
public class AudioSourceWrapper : IComponentData {
  public AudioSource Source;
}
