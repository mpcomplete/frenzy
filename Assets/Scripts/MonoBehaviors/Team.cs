using System;
using System.Collections.Generic;
using UnityEngine;

public class Team : MonoBehaviour {
  // Config
  public TeamConfiguration TeamConfiguration;
  //public UpgradeConfig UpgradeConfig;  // TODO: Add real data here.
  public KeyMap KeyMap;
  public Stanchion Stanchion;
  public Player Player;
  public Base Base;
  public Base SpawnerTop;
  public Base SpawnerBot;

  // State
  [HideInInspector]
  public List<Minion> Minions;
  public List<Projectile> Projectiles;
  [HideInInspector]
  public float Money;
  public Dictionary<UpgradeType, int> CurrentUpgradeLevels = new Dictionary<UpgradeType, int>();
  public float MinionHealthMultiplier = 1f;
  public float MinionDamageMultiplier = 1f;
  public float MinionSpawnRateMultiplier = 1f;

  void Start() {
    foreach (var skinnable in GetComponentsInChildren<TeamSkinnable>(true))
      skinnable.AssignTeam(this);
    Player.AssignTeam(this);

    foreach (UpgradeType type in Enum.GetValues(typeof(UpgradeType))) {
      SetUpgradeLevel(type, 0);
    }
  }

  public void SetUpgradeLevel(UpgradeType type, int level) {
    CurrentUpgradeLevels[type] = level;
    float value = 1.25f; // constant 25% increase for now.
    switch (type) {
    case UpgradeType.PlayerHealth:
      Player.Health *= value;
      Player.MaxHealth *= value;
      break;
    case UpgradeType.PlayerDamage:
      Player.Damage *= value;
      break;
    case UpgradeType.PlayerIncomeMultiplier:
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
}