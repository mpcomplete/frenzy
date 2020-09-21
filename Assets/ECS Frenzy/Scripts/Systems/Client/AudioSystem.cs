using System.Collections.Generic;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

namespace ECSFrenzy {
  [UpdateInGroup(typeof(ClientPresentationSystemGroup))]
  public class AudioSystem : SystemBase {
    AudioListener MainListener;
    Dictionary<string, AudioClip> AudioDictionary;

    protected override void OnCreate() {
      MainListener = Camera.main.GetComponent<AudioListener>();
      AudioDictionary = new Dictionary<string, AudioClip>();

      foreach (AudioClip clip in Resources.LoadAll("", typeof(AudioClip))) {
        Debug.Log($"Added {clip.name} to AudioDictionary");
        AudioDictionary.Add(clip.name, clip);
      }
    }

    protected override void OnUpdate() {
      Entities
      .WithName("Play_New_Audio")
      .WithoutBurst()
      .WithStructuralChanges()
      .ForEach((ref Entity e, ref PlayAudio playAudio) => {
        var clipName = playAudio.Name.ToString();

        if (AudioDictionary.TryGetValue(clipName, out AudioClip clip)) {
          AudioSource.PlayClipAtPoint(clip, MainListener.transform.position, playAudio.Volume);
        } else {
          Debug.LogError($"No AudioClip found in Resources Folder(s) matching the name {clipName}");
        }
        EntityManager.DestroyEntity(e);
      }).Run();
    }
  }
}