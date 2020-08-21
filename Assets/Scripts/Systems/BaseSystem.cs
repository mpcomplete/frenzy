using UnityEngine;

[System.Serializable]
public class BaseSystem {
  public void Update(Team team, float dt) {
    // TODO: This is a BAD spawning algorithm that will miss spawns at low
    // spawncooldowns... fix later?
    team.Base.TimeRemainingTillNextSpawn -= dt;

    if (team.Base.TimeRemainingTillNextSpawn <= 0) {
      Minion minion = Minion.Instantiate(team.TeamConfiguration.MinionPrefab, team.Base.SpawnLocation.position, team.Base.SpawnLocation.rotation);
      minion.Team = team;

      team.Minions.Add(minion);
      team.Base.TimeRemainingTillNextSpawn = team.Base.SpawnCooldown;
    }
  }
}