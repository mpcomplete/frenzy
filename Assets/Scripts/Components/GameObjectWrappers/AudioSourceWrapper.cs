using System;
using Unity.Entities;
using UnityEngine;

namespace ECSFrenzy {
  [Serializable]
  [GenerateAuthoringComponent]
  public class AudioSourceWrapper : IComponentData {
    public AudioSource Source;
  }
}