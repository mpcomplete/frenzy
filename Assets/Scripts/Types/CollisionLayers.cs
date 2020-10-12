using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;

namespace ECSFrenzy {
  // Keep in sync with the "Physics Category Names" in the Editor.
  public static class CollisionLayer {
    public const uint Team1 = 1<<1;
    public const uint Team2 = 1<<2;
    public const uint Player = 1<<3;
    public const uint Minion = 1<<4;
    public const uint Banner = 1<<5;
    public const uint Base = 1<<6;

    public static BlobAssetReference<Collider> CreateCollider(int teamNumber, uint layerMask) {
      layerMask |= teamNumber == 0 ? Team1 : Team2;
      return SphereCollider.Create(
        new SphereGeometry { Center = float3.zero, Radius = 0.25f },
        new CollisionFilter { BelongsTo = layerMask, CollidesWith = layerMask, GroupIndex = 0 });
    }
  }
}