using Unity.Entities;
using Unity.NetCode;

namespace ECSFrenzy {
  [UpdateInGroup(typeof(ClientPresentationSystemGroup))]
  public class AudioSystem : SystemBase {
    protected override void OnUpdate() {
      Entities
      .WithName("Play_New_Audio")
      .ForEach((Entity e, AudioSourceWrapper audioWrapper) => {
        if (!audioWrapper.Source.isPlaying) {
          audioWrapper.Source.Play();
        }
      })
      .WithStructuralChanges()
      .WithoutBurst()
      .Run();
    }
  }
}