using Unity.Entities;
using Unity.Transforms;
using Unity.NetCode;

namespace ECSFrenzy {
  [UpdateInGroup(typeof(ClientPresentationSystemGroup))]
  public class FireballRenderingSystem : ComponentSystem {
    public class FireballState : ISystemStateComponentData {
      public RenderedFireball Value;
    }

    protected override void OnUpdate() {
      Entities
      .WithNone<FireballState>()
      .WithAll<NetworkFireball>()
      .ForEach((Entity e) => {
        RenderedFireball rf = RenderedFireball.Instantiate(SystemConfig.Instance.RenderedFireballPrefab);

        EntityManager.AddComponentData(e, new FireballState { Value = rf });
      });

      Entities
      .WithAll<FireballState>()
      .WithNone<NetworkFireball>()
      .ForEach((Entity e) => {
        RenderedFireball rf = EntityManager.GetComponentData<FireballState>(e).Value;

        RenderedFireball.Destroy(rf.gameObject);
        EntityManager.RemoveComponent<FireballState>(e);
      });

      Entities
      .WithAll<NetworkFireball, FireballState>()
      .ForEach((Entity e, ref Translation translation, ref Rotation rotation) => {
        RenderedFireball rf = EntityManager.GetComponentData<FireballState>(e).Value;

        rf.transform.SetPositionAndRotation(translation.Value, rotation.Value);
      });
    }
  }
}