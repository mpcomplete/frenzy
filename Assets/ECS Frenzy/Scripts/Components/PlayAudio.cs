using System;
using Unity.Collections;
using Unity.Entities;

namespace ECSFrenzy {
  [Serializable]
  [GenerateAuthoringComponent]
  public struct PlayAudio : IComponentData {
    public int NameHash;
    public float Volume;

    public PlayAudio(int nameHash, float volume) {
      NameHash = nameHash;
      Volume = volume;
    }
  }
}