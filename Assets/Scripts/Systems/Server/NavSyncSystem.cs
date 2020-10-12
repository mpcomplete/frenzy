using Unity.Entities;
using Unity.NetCode;
using Unity.Transforms;
using UnityEngine.AI;

namespace ECSFrenzy {
  [UpdateInGroup(typeof(ServerSimulationSystemGroup))]
  public class NavSyncSystem : ComponentSystem {
    public class NavState : ISystemStateComponentData {
      public NavMeshAgent Value;
    }
    protected override void OnUpdate() {
      Entities
      .WithAll<NavTarget>()
      .WithNone<NavState>()
      .ForEach((Entity e, ref Translation translation, ref Team team, ref NavTarget target) => {
        if (target.Value == Entity.Null)
          return;

        NavMeshAgent agent = NavMeshAgent.Instantiate(SystemConfig.Instance.NavAgentBridgePrefab);
        EntityManager.AddComponentData(e, new NavState { Value = agent });

        if (NavMesh.SamplePosition(translation.Value, out NavMeshHit hit, 10f, 1))
          translation.Value = hit.position;
        agent.Warp(translation.Value);
        LocalToWorld targetTransform = EntityManager.GetComponentData<LocalToWorld>(target.Value);
        agent.SetDestination(targetTransform.Position);
      });

      Entities
      .WithNone<NavTarget>()
      .WithAll<NavState>()
      .ForEach((Entity e) => {
        NavMeshAgent agent = EntityManager.GetComponentData<NavState>(e).Value;
        NavMeshAgent.Destroy(agent.gameObject);
        EntityManager.RemoveComponent<NavState>(e);
      });

      Entities
      .WithAll<NavTarget>()
      .WithAll<NavState>()
      .ForEach((Entity e, ref Translation translation, ref NavTarget target) => {
        NavMeshAgent agent = EntityManager.GetComponentData<NavState>(e).Value;
        translation.Value = agent.transform.position;

        LocalToWorld targetTransform = EntityManager.GetComponentData<LocalToWorld>(target.Value);
        agent.SetDestination(targetTransform.Position);
      });
    }
  }
}