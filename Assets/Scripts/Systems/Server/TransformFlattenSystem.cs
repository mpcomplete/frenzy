using Unity.Entities;
using Unity.Transforms;
using Unity.NetCode;

// This dumb system reparents every object to be toplevel, fixing up its transform as it goes.
//
// This is hack EXISTS to fix the following issue: server-side ghosts that have non-ghost parents
// do not send their LocalToWorld data - only their local Translation and Rotation. The client
// would then render the ghost at the incorrect local transform, instead of the global transform.
//
// This converts every object to top-level, so that an objects local transform and global transform
// are the same, side-stepping the issue once and for all.
[UpdateInGroup(typeof(ServerSimulationSystemGroup))]
public class TransformFlattenSystem : ComponentSystem {
  protected override void OnUpdate() {
    Entities
    .WithAll<GhostComponent>()
    .ForEach((Entity e, ref Parent parent, ref Translation translation, ref Rotation rotation, ref LocalToWorld transform) => {
      parent.Value = Entity.Null;
      translation.Value = transform.Position;
      rotation.Value = transform.Rotation;
    });
  }
}