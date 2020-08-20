﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Team {
  public TeamConfiguration TeamConfiguration;
  public List<Minion> Minions;
  public Stanchion Stanchion;
  public Player Player;
  public Base Base;
}