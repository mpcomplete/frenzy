using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

namespace ECSFrenzy {
  [UpdateInGroup(typeof(ClientPresentationSystemGroup))]
  public class AudioSystem : SystemBase {
    public class AudioInstance : ISystemStateComponentData {
      public AudioSource Value;

      public static AudioInstance Instanced(AudioSource source) {
        return new AudioInstance { Value = AudioSource.Instantiate(source) };
      }
    }

    protected override void OnUpdate() {
      Entities
      .WithName("Instantiate_Audio_Instances")
      .WithNone<AudioInstance>()
      .ForEach((Entity e, AudioSourceWrapper audioSourceWrapper) => {
        EntityManager.AddComponentData(e, AudioInstance.Instanced(audioSourceWrapper.Source));
      })
      .WithStructuralChanges()
      .WithoutBurst()
      .Run();

      Entities
      .WithName("Destroy_Audio_Instances")
      .WithNone<AudioSourceWrapper>()
      .ForEach((Entity e, AudioInstance audioInstance) => {
        AudioSource.Destroy(audioInstance.Value.gameObject);
        EntityManager.RemoveComponent<AudioInstance>(e);
      })
      .WithStructuralChanges()
      .WithoutBurst()
      .Run();
    }
  }
}