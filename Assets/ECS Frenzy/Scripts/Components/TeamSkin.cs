using System;
using Unity.Entities;
using Unity.NetCode;
using Unity.Rendering;

namespace ECSFrenzy {
  // TeamSkin is used by a Team-holding Entity to point to a child Entity that owns the RenderMesh to skin.
  [Serializable]
  [GenerateAuthoringComponent]
  public struct TeamSkin : IComponentData {
    public Entity EntityToSkin;
  }

  [UpdateInGroup(typeof(ClientPresentationSystemGroup))]
  public class TeamSkinningSystem : ComponentSystem {
    public struct TeamState : ISystemStateComponentData { }

    protected override void OnUpdate() {
      Entities
      .WithAll<RenderMesh>()
      .WithNone<TeamState>()
      .ForEach((Entity e, ref Team team) => {
        SetMaterial(e, e, in team);
      });

      Entities
      .WithNone<TeamState>()
      .ForEach((Entity e, ref Team team, ref TeamSkin skin) => {
        SetMaterial(e, EntityManager.GetComponentData<TeamSkin>(e).EntityToSkin, in team);
      });
    }

    void SetMaterial(Entity e, Entity entityToSkin, in Team team) {
      var rm = EntityManager.GetSharedComponentData<RenderMesh>(entityToSkin);
      rm.material = SystemConfig.Instance.TeamConfigs[team.Value].Material;
      PostUpdateCommands.AddComponent(e, new TeamState());
      PostUpdateCommands.SetSharedComponent(entityToSkin, rm);
    }
  }
}