using UnityEngine;

[System.Serializable]
public class BaseSystem {
  public void Update(Team team, float dt) {
    // TODO: This is a BAD spawning algorithm that will miss spawns at low
    // spawncooldowns... fix later?
    team.Base.TimeRemainingTillNextSpawn -= dt;

    if (team.Base.TimeRemainingTillNextSpawn <= 0) {
      Vector3 position = team.Base.SpawnLocation.position;
      Quaternion rotation = team.Base.SpawnLocation.rotation;
      Minion minion = Minion.Instantiate(team.TeamConfiguration.MinionPrefab, position, rotation);

      minion.NavMeshAgent.Warp(team.Base.SpawnLocation.position);
      team.Minions.Add(minion);
      team.Base.TimeRemainingTillNextSpawn = team.Base.SpawnCooldown;
    }
  }
}