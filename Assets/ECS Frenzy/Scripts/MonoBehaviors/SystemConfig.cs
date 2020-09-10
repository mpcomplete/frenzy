﻿using ECSFrenzy;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

public class SystemConfig : MonoBehaviour {
  public static SystemConfig Instance;
  public RenderedPlayer RenderedPlayerPrefab;
  public float PlayerMoveSpeed = 5;
  public float ControllerDeadzone = .19f;
  public TeamConfig[] TeamConfigs;

  void Awake() {
    Instance = this;
  }
}