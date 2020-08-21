using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeaponEffectLaser : WeaponEffect {
  LineRenderer lineRenderer;
  void Start() {
    lineRenderer = GetComponent<LineRenderer>();
  }

  protected override void DoPlay(Unit target) {
    lineRenderer.positionCount = 2;
    lineRenderer.SetPosition(0, transform.position);
    lineRenderer.SetPosition(1, target.transform.position);
    lineRenderer.gameObject.SetActive(true);
  }


  protected override void DoStop() {
    lineRenderer.gameObject.SetActive(false);
  }
}