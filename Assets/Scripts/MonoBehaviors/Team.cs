using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Team : MonoBehaviour {
  // Config
  public TeamConfiguration TeamConfiguration;
  public KeyMap KeyMap;
  public Stanchion Stanchion;
  public Player Player;
  public Base Base;
  public Base SpawnerTop;
  public Base SpawnerBot;

  // State
  [HideInInspector]
  public List<Minion> Minions;
  [HideInInspector]
  public List<Projectile> Projectiles;
  [HideInInspector]
  public float Money;

  void Start() {
    foreach (var skinnable in GetComponentsInChildren<TeamSkinnable>(true))
      skinnable.AssignTeam(this);
    Player.AssignTeam(this);
  }
}