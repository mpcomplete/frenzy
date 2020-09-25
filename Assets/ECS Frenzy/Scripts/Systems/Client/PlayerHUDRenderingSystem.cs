using Unity.Entities;
using Unity.Transforms;
using Unity.Jobs;
using Unity.NetCode;
using UnityEngine;

namespace ECSFrenzy {
  [UpdateInGroup(typeof(ClientPresentationSystemGroup))]
  public class PlayerHUDRenderingSystem : SystemBase {
    public class PlayerHUDInstance : ISystemStateComponentData {
      public PlayerHUD Value;

      public static PlayerHUDInstance Instanced(PlayerHUD hud) {
        return new PlayerHUDInstance { Value = PlayerHUD.Instantiate(hud) };
      }
    }

    Camera Camera;

    protected override void OnCreate() {
      Camera = Camera.main;
    }

    protected override void OnUpdate() {
      var hudPrefab = SystemConfig.Instance.PlayerHUDPrefab;

      Entities
      .WithName("Instantiate_PlayerHUD_Instances")
      .WithNone<PlayerHUDInstance>()
      .ForEach((Entity e) => {
        var playerHUD = PlayerHUD.Instantiate(hudPrefab);
        
        playerHUD.Camera = Camera;
        EntityManager.AddComponentData(e, new PlayerHUDInstance { Value = playerHUD });
      })
      .WithStructuralChanges()
      .WithoutBurst()
      .Run();

      Entities
      .WithName("Destroy_PlayerHUD_Instances")
      .WithNone<NetworkPlayer>()
      .ForEach((Entity e, PlayerHUDInstance playerHUDInstance) => {
        PlayerHUD.Destroy(playerHUDInstance.Value.gameObject);
        EntityManager.RemoveComponent<PlayerHUDInstance>(e);
      })
      .WithStructuralChanges()
      .WithoutBurst()
      .Run();

      Entities
      .WithName("Render_Player_HUD")
      .WithAll<NetworkPlayer>()
      .ForEach((Entity entity, PlayerHUDInstance playerHUDInstance, ref PlayerAbilites abilities, ref Translation position) => {
        var ability1TimeRemaining = EntityManager.GetComponentData<Cooldown>(abilities.Ability1).TimeRemaining;

        playerHUDInstance.Value.PlayerPosition = position.Value;
        playerHUDInstance.Value.TimeRemaining = ability1TimeRemaining;
      })
      .WithoutBurst()
      .Run();
    }
  }
}