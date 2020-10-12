using Unity.Transforms;
using Unity.Mathematics;

public static class ComponentExtensions {
  public static Translation ToTranslation(this float3 v) => new Translation { Value = v };
}
