using Unity.Entities;
using Unity.Transforms;
using Unity.Jobs;
using Unity.NetCode;
using UnityEngine;
using static Unity.Mathematics.math;

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
      var dt = Time.DeltaTime;

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
      .WithAll<NetworkPlayer, PlayerInput>()
      .ForEach((Entity entity, PlayerHUDInstance playerHUDInstance, ref PlayerAbilites abilities, ref Translation position) => {
        var ability1Cooldown = EntityManager.GetComponentData<Cooldown>(abilities.Ability1);
        var ability1Status = EntityManager.GetComponentData<CooldownStatus>(abilities.Ability1);
        var notifications = playerHUDInstance.Value.Notifications;

        playerHUDInstance.Value.PlayerPosition = position.Value;
        playerHUDInstance.Value.Ability1.TimeRemaining = ability1Cooldown.TimeRemaining;
        playerHUDInstance.Value.Ability1.Duration = ability1Cooldown.Duration;
        for (int i = notifications.Count - 1; i >= 0; i--) {
          notifications[i].TimeRemaining = max(0, notifications[i].TimeRemaining - dt);
          if (notifications[i].TimeRemaining <= 0) {
            notifications.RemoveAt(i);
          }
        }
        if (ability1Status.Value == CooldownStatus.Status.JustElapsed) {
          playerHUDInstance.Value.Notifications.Add(new PlayerHUD.Notification(playerHUDInstance.Value.NotificationDuration, "Fireball Ready"));
        }
      })
      .WithoutBurst()
      .Run();
    }
  }
}