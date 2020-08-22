using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class StatusEffectsSystem {
  public void Execute(IEnumerable<Unit> units, float dt) {
    foreach (var unit in units) {
      Update(unit, dt);
    }
  }

  public void Update(Unit unit, float dt) {
    unit.StatusEffects.StunTimeRemaining = Mathf.Max(0, unit.StatusEffects.StunTimeRemaining - dt);
    unit.IsMobile = unit.Alive && unit.StatusEffects.StunTimeRemaining <= 0;

    unit.StatusEffects.StunBox.SetActive(unit.StatusEffects.StunTimeRemaining > 0);
  }
}