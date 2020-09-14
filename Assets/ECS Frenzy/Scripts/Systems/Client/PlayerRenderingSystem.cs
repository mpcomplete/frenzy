using Unity.Entities;
using Unity.Transforms;
using Unity.NetCode;

[UpdateInGroup(typeof(ClientPresentationSystemGroup))]
public class PlayerRenderingSystem : ComponentSystem {
  public class PlayerState : ISystemStateComponentData {
    public RenderedPlayer Value;
  }

  protected override void OnUpdate() {
    Entities
    .WithNone<PlayerState>()
    .ForEach((Entity e, ref NetworkPlayer np) => {
      RenderedPlayer rp = RenderedPlayer.Instantiate(SystemConfig.Instance.RenderedPlayerPrefab);

      EntityManager.AddComponentData(e, new PlayerState { Value = rp });
    });

    Entities
    .WithAll<PlayerState>()
    .WithNone<NetworkPlayer>()
    .ForEach((Entity e) => {
      RenderedPlayer rp = EntityManager.GetComponentData<PlayerState>(e).Value;

      RenderedPlayer.Destroy(rp.gameObject);
      EntityManager.RemoveComponent<PlayerState>(e);
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