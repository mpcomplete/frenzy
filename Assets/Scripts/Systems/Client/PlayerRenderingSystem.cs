using Unity.Entities;
using Unity.Transforms;
using Unity.NetCode;

namespace ECSFrenzy {
  [UpdateInGroup(typeof(ClientPresentationSystemGroup))]
  public class PlayerRenderingSystem : ComponentSystem {
    public class RenderedPlayerInstance : ISystemStateComponentData {
      public RenderedPlayer Value;

      public static RenderedPlayerInstance Instanced(RenderedPlayer prefab) {
        return new RenderedPlayerInstance { Value = RenderedPlayer.Instantiate(prefab) };
      }
    }

    protected override void OnUpdate() {
      Entities
      .WithNone<RenderedPlayerInstance>()
      .ForEach((Entity e, ref NetworkPlayer np) => {
        EntityManager.AddComponentData(e, RenderedPlayerInstance.Instanced(SystemConfig.Instance.RenderedPlayerPrefab));
      });

      Entities
      .WithAll<PlayerState>()
      .WithNone<NetworkPlayer>()
      .ForEach((Entity e) => {
        RenderedPlayer rp = EntityManager.GetComponentData<RenderedPlayerInstance>(e).Value;

        RenderedPlayer.Destroy(rp.gameObject);
        EntityManager.RemoveComponent<RenderedPlayerInstance>(e);
      });

      Entities
      .WithAll<RenderedPlayerInstance>()
      .ForEach((Entity e, ref NetworkPlayer np, ref Translation translation, ref Rotation rotation, ref PlayerState playerState) => {
        RenderedPlayer rp = EntityManager.GetComponentData<RenderedPlayerInstance>(e).Value;

        rp.transform.SetPositionAndRotation(translation.Value, rotation.Value);
        rp.Animator.SetFloat("Speed", playerState.IsMoving ? 1 : 0);
        if (playerState.DidFireball) 
          rp.Animator.SetTrigger("Fireball");
      });
    }
  }
}