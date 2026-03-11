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
        static double _lastTime = -1.0;   // -1 = not yet initialised; skip first frame
        static uint   _prevButtons;

        static SpaceMouseController()
        {
            EditorApplication.update += OnUpdate;
            // Unsubscribe before the next domain reload to prevent duplicate callbacks
            // after script recompilation or entering Play mode.
            AssemblyReloadEvents.beforeAssemblyReload += OnBeforeAssemblyReload;
        }

        static void OnBeforeAssemblyReload()
        {
            EditorApplication.update -= OnUpdate;
            AssemblyReloadEvents.beforeAssemblyReload -= OnBeforeAssemblyReload;
        }

        static void DispatchAction(string action)
        {
            if (string.IsNullOrEmpty(action)) return;

            if (action.StartsWith("#", System.StringComparison.Ordinal))
            {
                SpaceMouseCommands.Execute(action);
                return;
            }

            // "@shortcutId" — simulate via ShortcutManager binding + CGEvent
            if (action.StartsWith("@", System.StringComparison.Ordinal))
            {
                SpaceMouseCommands.ExecuteShortcut(action.Substring(1));
                return;
            }

            const string mainMenuPrefix = "Main Menu/";
            string menuPath = action.StartsWith(mainMenuPrefix, System.StringComparison.Ordinal)
                ? action.Substring(mainMenuPrefix.Length)
                : action; // backward-compat: plain paths used before this feature

            if (!EditorApplication.ExecuteMenuItem(menuPath))
                Debug.LogWarning($"[SpaceMouse] Unknown menu item: \"{menuPath}\"");
        }

        static void OnUpdate()
        {
            if (!SpaceMouseDevice.Poll()) return;

            // ── Button edge detection & action dispatch ───────────────────────
            uint curr     = SpaceMouseDevice.Buttons;
            uint pressed  = curr  & ~_prevButtons;   // newly pressed this frame
            uint released = _prevButtons & ~curr;     // newly released this frame
            _prevButtons  = curr;

            if (pressed != 0)
            {
                for (int i = 0; i < SpaceMouseSettings.MaxButtons; i++)
                {
                    if ((pressed & (1u << i)) == 0) continue;
                    string action = SpaceMouseSettings.GetButtonAction(i);
                    DispatchAction(action);
                }
            }

            if (released != 0)
            {
                for (int i = 0; i < SpaceMouseSettings.MaxButtons; i++)
                {
                    if ((released & (1u << i)) == 0) continue;
                    string action = SpaceMouseSettings.GetButtonAction(i);
                    if (!string.IsNullOrEmpty(action) && action.StartsWith("#", System.StringComparison.Ordinal))
                        SpaceMouseCommands.ExecuteRelease(action);
                }
            }

            SceneView sv = SceneView.lastActiveSceneView;
            if (sv == null) return;

            // ── Delta time ──────────────────────────────────────────────────
            double now = EditorApplication.timeSinceStartup;
            if (_lastTime < 0) { _lastTime = now; return; }  // skip first frame
            float  dt  = Mathf.Clamp((float)(now - _lastTime), 0f, 0.05f);
            _lastTime  = now;
            if (dt <= 0f) return;

            // ── Read processed channel values ────────────────────────────────
            float panX = SpaceMouseSettings.GetPanX();
            float panY = SpaceMouseSettings.GetPanY();
            float zoom = SpaceMouseSettings.GetZoom();
            bool  rotLocked = SpaceMouseSettings.RotationLocked;
            float tilt = rotLocked ? 0f : SpaceMouseSettings.GetTilt();
            float spin = rotLocked ? 0f : SpaceMouseSettings.GetSpin();
            float roll = rotLocked ? 0f : SpaceMouseSettings.GetRoll();

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
