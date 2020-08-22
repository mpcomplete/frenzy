using System.Linq;
using UnityEngine;

public enum UpgradeType {
  PlayerHealth,
  PlayerDamage,
  PlayerIncomeMultiplier,
  MinionHealth,
  MinionDamage,
  MinionSpawnRate,
}

// TODO: the rest is unused for now. Need real data.

[System.Serializable]
public class UpgradeValue {
  public float Cost;
  public float Value;
}

[System.Serializable]
public class UpgradeVariable {
  public UpgradeType Type;
  public UpgradeValue[] Levels;
}

[CreateAssetMenu]
public class UpgradeConfig : ScriptableObject {
  public UpgradeVariable[] Upgrades;

  public UpgradeVariable UpgradeVariableForType(UpgradeType type) {
    return Upgrades.Where((uv) => uv.Type == type).First();
  }
}