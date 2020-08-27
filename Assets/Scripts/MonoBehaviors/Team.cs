using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class Team : MonoBehaviour {
  // Config
  public TeamConfiguration TeamConfiguration;
  //public UpgradeConfig UpgradeConfig;  // TODO: Add real data here.
  public Stanchion Stanchion;
  public Player Player;
  public Base Base;
  public Base SpawnerTop;
  public Base SpawnerBot;

  public struct ControlsMap {
    public InputAction Move;
    public InputAction PlaceStanchion;
    public InputAction Attack;
    public InputAction Ability1;
    public InputAction Ability2;
    public InputAction Ability3;
    public InputAction Ability4;
  }
  public ControlsMap Controls;
  public string ControlScheme;

  // State
  [HideInInspector]
  public List<Minion> Minions;
  [HideInInspector]
  public List<Projectile> Projectiles;
  public float Money;
  public Dictionary<UpgradeType, int> CurrentUpgradeLevels;
  public float IncomeMultiplier = 1f;
  public float MinionHealthMultiplier = 1f;
  public float MinionDamageMultiplier = 1f;
  public float MinionSpawnRateMultiplier = 1f;

  void Awake() {
    foreach (var skinnable in GetComponentsInChildren<TeamSkinnable>(true))
      skinnable.AssignTeam(this);
    Player.AssignTeam(this);

    CurrentUpgradeLevels = new Dictionary<UpgradeType, int>(); 
    foreach (UpgradeType type in Enum.GetValues(typeof(UpgradeType)))
      SetUpgradeLevel(type, 0);
  }

  public float GetCostForNextUpgrade(UpgradeType type) {
    // TODO: data driven
    int level = CurrentUpgradeLevels[type]+1;
    if (level >= 6) return Mathf.Infinity;
    return level*100;
  }

  public void PurchaseNextUpgrade(UpgradeType type) {
    float cost = GetCostForNextUpgrade(type);
    if (Money < cost) {
      Debug.LogError($"Attempt to purchase upgrade {type} for {cost} with only {Money}");
      return;
    }
    Money -= cost;
    SetUpgradeLevel(type, CurrentUpgradeLevels[type]+1);
    Debug.Log($"Purchased upgrade {type} for {cost}. Level is {CurrentUpgradeLevels[type]}");
  }

  public void SetUpgradeLevel(UpgradeType type, int level) {
    CurrentUpgradeLevels[type] = level;
    float value = level == 0 ? 1f : 1.25f; // constant 25% increase for now.
    switch (type) {
    case UpgradeType.PlayerHealth:
      Player.Health *= value;
      Player.MaxHealth *= value;
      break;
    case UpgradeType.PlayerDamage:
      Player.Damage *= value;
      break;
    case UpgradeType.PlayerIncomeMultiplier:
      IncomeMultiplier *= value;
      break;
    case UpgradeType.MinionHealth: // TODO: Should we upgrade current minions?
      MinionHealthMultiplier *= value;
      break;
    case UpgradeType.MinionDamage:
      MinionDamageMultiplier *= value;
      break;
    case UpgradeType.MinionSpawnRate:
      MinionSpawnRateMultiplier *= value;
      break;
    }
  }

  public void OnKilledEnemyUnit(float money) {
    Money += money * IncomeMultiplier;
  }
}