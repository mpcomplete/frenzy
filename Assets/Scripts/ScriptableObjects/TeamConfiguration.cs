﻿using UnityEngine;

[CreateAssetMenu]
public class TeamConfiguration : ScriptableObject {
  public Material Material;
  public int MinionLayer;
  public int PlayerLayer;
  public LayerMask AttackableMinionLayerMask;
  public LayerMask AttackablePlayerLayerMask;
}