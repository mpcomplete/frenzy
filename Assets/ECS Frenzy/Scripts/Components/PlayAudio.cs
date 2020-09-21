using System;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

namespace ECSFrenzy {
  [Serializable]
  [GenerateAuthoringComponent]
  public struct PlayAudio : IComponentData {
    public FixedString128 Name;
    public float Volume;

    public PlayAudio(FixedString128 name, float volume) {
      Name = name;
      Volume = volume;
    }
  }
}