using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.NetCode;
using static Unity.Mathematics.math;

[UpdateInGroup(typeof(ClientPresentationSystemGroup))]
public class DroneAssistantRenderingSystem : ComponentSystem {
  public DroneAssistant DroneAssistant;

  protected override void OnUpdate() {
    float time = (float)Time.ElapsedTime;
    var playerEntities = Entities.WithAll<NetworkPlayer, Translation>().ToEntityQuery();
    var translations = playerEntities.ToComponentDataArray<Translation>(Allocator.Temp);

    if (translations.Length == 0 || DroneAssistant == null)
      return;

    DroneAssistant.transform.position = translations[0].Value + float3(0, DroneAssistant.HoverHeight + sin(time), 0);
  }
}