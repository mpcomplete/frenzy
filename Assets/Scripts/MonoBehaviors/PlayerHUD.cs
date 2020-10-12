using System;
using System.Collections.Generic;
using UnityEngine;

namespace ECSFrenzy {
  public class PlayerHUD : MonoBehaviour {
    [Serializable]
    public class AbilityInfo {
      public string Name;
      public float TimeRemaining;
      public float Duration;
    }

    [Serializable]
    public class Notification {
      public float TimeRemaining;
      public string Message;

      public Notification(float duration, string message) {
        TimeRemaining = duration;
        Message = message;
      }
    }

    public Camera Camera;
    public Vector3 PlayerPosition;
    public AbilityInfo Ability1 = new AbilityInfo { Name = "Fireball" };
    public Vector2 AbilityScreenOffsetX;
    public float AbilityLabelWidth = 20;
    public float AbilityLabelHeight = 100;
    public float NotificationWidth = 200;
    public float NotificationHeight = 24;
    public float NotificationDuration = 1;
    public float NotificationDriftHeight = 100;
    public List<Notification> Notifications = new List<Notification>();

    void OnGUI() {
      void GUICooldownMeter(AbilityInfo a, Color bgc, Color mc, float x, float y, float w, float h) {
        var cooldownBackgroundRect = new Rect(x, y, w, h);
        var originalColor = GUI.backgroundColor;

        GUI.backgroundColor = bgc;
        GUI.Box(cooldownBackgroundRect, GUIContent.none);

        if (a.TimeRemaining > 0) {
          var fraction = a.TimeRemaining / a.Duration;
          var meterRect = new Rect(x, y, w, fraction * h);

          GUI.backgroundColor = mc;
          GUI.Box(meterRect, GUIContent.none);
        }
        GUI.backgroundColor = originalColor;
      }

      Vector2 screenPosition = (Vector2)Camera.WorldToScreenPoint(PlayerPosition) + AbilityScreenOffsetX;

      GUICooldownMeter(Ability1, Color.grey, Color.white, screenPosition.x, Camera.pixelHeight - screenPosition.y, AbilityLabelWidth, AbilityLabelHeight);

      foreach (var notification in Notifications) {
        var yOffset = NotificationDriftHeight * (1 - notification.TimeRemaining / NotificationDuration);
        var notificationsRect = new Rect(Camera.pixelWidth / 2, Camera.pixelHeight / 2 - yOffset, NotificationWidth, NotificationHeight);

        GUI.Label(notificationsRect, notification.Message);
      }
    }
  }
}