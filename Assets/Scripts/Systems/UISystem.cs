using UnityEngine;

[System.Serializable]
public class UISystem {
  public Color ReadyColor = Color.green;
  public Color EmptyColor = Color.gray;
  public Color FullColor = Color.white;

  void UpdatePlayerHUD(PlayerHUD HUD, Player player, float dt) {
    UpdateCooldownMeter(HUD.Ability1CooldownMeter, player.Ability1Cooldown, dt);
    UpdateCooldownMeter(HUD.Ability2CooldownMeter, player.Ability2Cooldown, dt);
    UpdateCooldownMeter(HUD.Ability3CooldownMeter, player.Ability3Cooldown, dt);
    UpdateCooldownMeter(HUD.Ability4CooldownMeter, player.Ability4Cooldown, dt);
  }

  void UpdateCooldownMeter(CooldownMeter meter, Cooldown cooldown, float dt) {
    float targetValue = (cooldown.Duration - cooldown.TimeRemaining) / cooldown.Duration;
    Color targetColor = Color.Lerp(EmptyColor, FullColor, targetValue);

    meter.MeterImage.color = (cooldown.TimeRemaining <= 0) ? ReadyColor : targetColor;
    meter.MeterTransform.anchorMax = new Vector2(meter.MeterTransform.anchorMax.x, targetValue);
  }

  public void Update(UI ui, Player player1, Player player2, float dt) {
    UpdatePlayerHUD(ui.Player1HUD, player1, dt);
    UpdatePlayerHUD(ui.Player2HUD, player2, dt);
  }
}