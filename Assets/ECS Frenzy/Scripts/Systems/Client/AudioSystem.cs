using System.Collections.Generic;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

namespace ECSFrenzy {
  [UpdateInGroup(typeof(ClientPresentationSystemGroup))]
  public class AudioSystem : SystemBase {
    AudioListener MainListener;
    Dictionary<int, AudioClip> AudioDictionary;

    protected override void OnCreate() {
      MainListener = Camera.main.GetComponent<AudioListener>();
      AudioDictionary = new Dictionary<int, AudioClip>();

      foreach (AudioClip clip in Resources.LoadAll("", typeof(AudioClip))) {
        Debug.Log($"Added {clip.name} to AudioDictionary");
        AudioDictionary.Add(Animator.StringToHash(clip.name), clip);
      }
    }

    protected override void OnUpdate() {
      Entities
      .WithName("Play_New_Audio")
      .ForEach((ref Entity e, ref PlayAudio playAudio) => {
        if (AudioDictionary.TryGetValue(playAudio.NameHash, out AudioClip clip)) {
          AudioSource.PlayClipAtPoint(clip, MainListener.transform.position, playAudio.Volume);
        } else {
          Debug.LogError($"No AudioClip found in Resources Folder(s) matching the hash {playAudio.NameHash}");
        }
      })
      .WithStructuralChanges()
      .WithoutBurst()
      .Run();
    }
  }
}