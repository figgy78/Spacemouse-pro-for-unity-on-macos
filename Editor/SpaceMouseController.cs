using UnityEditor;
using UnityEngine;

namespace SpaceMousePro
{
    /// <summary>
    /// Hooks into EditorApplication.update and drives the active SceneView camera
    /// from SpaceMouse Pro 6DOF input.
    ///
    /// Orbit mode:
    ///   Rotations move the camera along a sphere around sv.pivot.
    ///   The pivot is fixed; camera always looks at it.
    ///   Translation slides the pivot (pan) or moves along the look axis (zoom).
    ///
    /// FreeLook mode:
    ///   No pivot. Camera rotates around its own position (FPS-style).
    ///   Translation moves the camera directly through space.
    /// </summary>
    [InitializeOnLoad]
    static class SpaceMouseController
    {
        static double _lastTime;

        static SpaceMouseController()
        {
            EditorApplication.update += OnUpdate;
        }

        static void OnUpdate()
        {
            if (!SpaceMouseDevice.Poll()) return;

            SceneView sv = SceneView.lastActiveSceneView;
            if (sv == null) return;

            // ── Delta time ──────────────────────────────────────────────────
            double now = EditorApplication.timeSinceStartup;
            float  dt  = Mathf.Clamp((float)(now - _lastTime), 0f, 0.05f);
            _lastTime  = now;
            if (dt <= 0f) return;

            // ── Read processed channel values ────────────────────────────────
            float panX = SpaceMouseSettings.GetPanX();
            float panY = SpaceMouseSettings.GetPanY();
            float zoom = SpaceMouseSettings.GetZoom();
            float tilt = SpaceMouseSettings.GetTilt();
            float spin = SpaceMouseSettings.GetSpin();
            float roll = SpaceMouseSettings.GetRoll();

            bool hasInput = panX != 0f || panY != 0f || zoom != 0f ||
                            tilt != 0f || spin != 0f || roll != 0f;
            if (!hasInput) return;

            // ── Shared setup ─────────────────────────────────────────────────
            float transSpeed = SpaceMouseSettings.TranslationSpeed;
            float distScale  = sv.size * transSpeed;

            float rotSpeed  = SpaceMouseSettings.RotationSpeed;
            float degreesPS = 120f * rotSpeed;

            Vector3 camRight   = sv.rotation * Vector3.right;
            Vector3 camUp      = sv.rotation * Vector3.up;
            Vector3 camForward = sv.rotation * Vector3.forward;

            // Rotation delta shared by both modes
            Quaternion deltaQ =
                Quaternion.AngleAxis(-spin * degreesPS * dt, Vector3.up) *
                Quaternion.AngleAxis( tilt * degreesPS * dt, camRight)   *
                Quaternion.AngleAxis(-roll * degreesPS * dt * 0.5f, camForward);

            if (SpaceMouseSettings.NavigationMode == NavigationMode.Orbit)
            {
                // ── Orbit ────────────────────────────────────────────────────
                // Pivot is the fixed orbit center.
                // Pan slides the pivot laterally; zoom changes the orbit radius.
                // Rotation moves the camera on a sphere around the pivot.

                sv.pivot += camRight * (-panX * distScale * dt);
                sv.pivot += camUp    * ( panY * distScale * dt);

                // Zoom changes orbit radius (sv.size), not camera position.
                sv.size = Mathf.Max(0.001f, sv.size - zoom * sv.size * transSpeed * dt);

                sv.rotation = deltaQ * sv.rotation;
            }
            else
            {
                // ── FreeLook ─────────────────────────────────────────────────
                // No pivot. Camera rotates around its own position.
                // Use the actual camera transform position as the anchor so the
                // calculation is correct in perspective mode (sv.size ≠ camera distance).

                Vector3 camPos = sv.camera != null
                    ? sv.camera.transform.position
                    : sv.pivot - camForward * sv.size;

                camPos += camRight   * (-panX * distScale * dt);
                camPos += camUp      * ( panY * distScale * dt);
                camPos += camForward * ( zoom * distScale * dt);

                // Preserve the actual camera-to-pivot distance so sv.size stays consistent.
                float camDist = sv.camera != null
                    ? Vector3.Distance(sv.camera.transform.position, sv.pivot)
                    : sv.size;

                sv.rotation = deltaQ * sv.rotation;

                sv.pivot = camPos + sv.rotation * Vector3.forward * camDist;
            }

            sv.Repaint();
        }
    }
}
