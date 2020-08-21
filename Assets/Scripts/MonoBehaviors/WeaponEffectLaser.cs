using UnityEngine;

public class WeaponEffectLaser : WeaponEffect {
  [SerializeField] LineRenderer lineRenderer;
  [SerializeField] AudioSource audioSource;
  public float minPitch = .9f;
  public float maxPitch = 1f;

  protected override void DoPlay(Unit target) {
    if (!audioSource.isPlaying) {
      audioSource.pitch = Random.Range(minPitch, maxPitch);
      audioSource.Play();
    }
    lineRenderer.positionCount = 2;
    lineRenderer.SetPosition(0, transform.position);
    lineRenderer.SetPosition(1, target.transform.position);
    lineRenderer.gameObject.SetActive(true);
  }

  protected override void DoStop() {
    lineRenderer.gameObject.SetActive(false);
  }
}