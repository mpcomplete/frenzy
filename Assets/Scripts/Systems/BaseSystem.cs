using UnityEngine;

[System.Serializable]
public class BaseSystem {
  public int MAX_MINIONS_PER_TEAM;
  public Minion MeleeMinionPrefab = null;
  public Minion RangedMinionPrefab = null;

  public void Update(Team team, float dt) {
    // TODO: This is a BAD spawning algorithm that will miss spawns at low
    // spawncooldowns... fix later?
    team.Base.TimeRemainingTillNextSpawn -= dt;

    if (team.Base.TimeRemainingTillNextSpawn <= 0 && team.Minions.Count < MAX_MINIONS_PER_TEAM) {
      Minion prefab = Random.value < .7f ? MeleeMinionPrefab : RangedMinionPrefab;
      Minion minion = Minion.Instantiate(prefab, team.Base.SpawnLocation.position, team.Base.SpawnLocation.rotation);
      minion.AssignTeam(team);

      team.Minions.Add(minion);
      team.Base.TimeRemainingTillNextSpawn = team.Base.SpawnCooldown;
    }
  }
}