using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.NetCode;

[UpdateInGroup(typeof(ClientPresentationSystemGroup))]
public class PlayerRenderingSystem : ComponentSystem {
  const int MAX_PLAYERS = 4;

  // See https://docs.unity3d.com/Packages/com.unity.entities@0.14/manual/system_state_components.html
  // This uses a "managed component" since it (a) is a class, and (b) contains a non-blittable type. Supposedly
  // this has performance drawbacks, but they are unlikely to matter in this case, and I think the alternative
  // (a Dictionary) has the same drawbacks.
  public class PlayerState : ISystemStateComponentData {
    public RenderedPlayer Value;
  }

  protected override void OnUpdate() {
    Entities
    .WithNone<PlayerState>()
    .ForEach((Entity e, ref NetworkPlayer np) => {
      RenderedPlayer rp = RenderedPlayer.Instantiate(SystemConfig.Instance.RenderedPlayerPrefab);
      PostUpdateCommands.AddComponent(e, new PlayerState { Value = rp });
    });

    Entities
    .WithAll<PlayerState>()
    .WithNone<NetworkPlayer>()
    .ForEach((Entity e) => {
      RenderedPlayer rp = EntityManager.GetComponentData<PlayerState>(e).Value;
      RenderedPlayer.Destroy(rp);
      PostUpdateCommands.RemoveComponent<PlayerState>(e);
    });

    Entities
    .WithAll<PlayerState>()
    .ForEach((Entity e, ref NetworkPlayer np, ref Translation translation, ref Rotation rotation, ref MoveSpeed moveSpeed) => {
      RenderedPlayer rp = EntityManager.GetComponentData<PlayerState>(e).Value;
      rp.transform.SetPositionAndRotation(translation.Value, rotation.Value);
      rp.Animator.SetFloat("Speed", moveSpeed.Value);
    });
  }
}