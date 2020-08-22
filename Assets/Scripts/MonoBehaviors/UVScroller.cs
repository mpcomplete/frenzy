using UnityEngine;

public class UVScroller : MonoBehaviour {
  public Vector2 Rate;

  public Vector2 Offset;
  [SerializeField] MeshRenderer MeshRenderer;

  public void Update() {
    Offset += Rate * Time.deltaTime;
    MeshRenderer.material.mainTextureOffset = Offset;
  }
}