using System.Collections.Generic;
using Unity.NetCode;
using Unity.NetCode.Editor;
using static Unity.NetCode.Editor.GhostAuthoringComponentEditor;

public class GhostOverrides : IGhostDefaultOverridesModifier {
  public void Modify(Dictionary<string, GhostAuthoringComponentEditor.GhostComponent> overrides) {
    UnityEngine.Debug.Log($"Changing: {overrides["Unity.Transforms.Translation"].attribute}");
    overrides["Unity.Transforms.NonUniformScale"] = new GhostAuthoringComponentEditor.GhostComponent {
      name = "Unity.Transforms.NonUniformScale",
      attribute = new GhostComponentAttribute { PrefabType = GhostPrefabType.All, OwnerPredictedSendType = GhostSendType.All, SendDataForChildEntity = false },
      fields = new GhostComponentField[] {
        new GhostComponentField {
          name = "Value",
          attribute = new GhostFieldAttribute{Quantization = 100, Interpolate = true}
        }
      },
      entityIndex = 0
    };
  }

  public void ModifyAlwaysIncludedAssembly(HashSet<string> alwaysIncludedAssemblies) {
    //UnityEngine.Debug.Log($"Changing: {alwaysIncludedAssemblies.Contains("Unity.Transforms.NonUniformScale")}");
    alwaysIncludedAssemblies.Add("Unity.Transforms.NonUniformScale");
  }

  public void ModifyTypeRegistry(TypeRegistry typeRegistry, string netCodeGenAssemblyPath) {
  }
}
