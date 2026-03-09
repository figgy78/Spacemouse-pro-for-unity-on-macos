using UnityEditor;
using UnityEditor.Overlays;
using UnityEditor.Toolbars;
using UnityEngine.UIElements;

namespace SpaceMousePro
{
    /// <summary>
    /// SceneView toolbar overlay for switching SpaceMouse navigation mode.
    /// Enable via the SceneView overlay menu (☰ top-right of SceneView).
    /// </summary>
    [Overlay(typeof(SceneView), "spacemouse-nav", "SpaceMouse")]
    sealed class SpaceMouseNavOverlay : ToolbarOverlay
    {
        SpaceMouseNavOverlay() : base(OrbitToggle.id, FreeLookToggle.id, RollToggle.id) { }

        // ── Orbit toggle ─────────────────────────────────────────────────────

        [EditorToolbarElement(id, typeof(SceneView))]
        sealed class OrbitToggle : EditorToolbarToggle
        {
            public const string id = "SpaceMousePro/Orbit";

            OrbitToggle()
            {
                text    = "Orbit";
                tooltip = "SpaceMouse: orbit camera around pivot";
                this.RegisterValueChangedCallback(OnChanged);
                RegisterCallback<AttachToPanelEvent>(_ =>
                {
                    Refresh();
                    EditorApplication.update += Refresh;
                });
                RegisterCallback<DetachFromPanelEvent>(_ => EditorApplication.update -= Refresh);
            }

            void OnChanged(ChangeEvent<bool> evt)
            {
                if (evt.newValue)
                    SpaceMouseSettings.NavigationMode = NavigationMode.Orbit;
            }

            void Refresh() =>
                SetValueWithoutNotify(SpaceMouseSettings.NavigationMode == NavigationMode.Orbit);
        }

        // ── Roll toggle ──────────────────────────────────────────────────────

        [EditorToolbarElement(id, typeof(SceneView))]
        sealed class RollToggle : EditorToolbarToggle
        {
            public const string id = "SpaceMousePro/Roll";

            RollToggle()
            {
                text    = "Lock horizon";
                tooltip = "SpaceMouse: lock horizon (disable camera roll)";
                this.RegisterValueChangedCallback(OnChanged);
                RegisterCallback<AttachToPanelEvent>(_ =>
                {
                    Refresh();
                    EditorApplication.update += Refresh;
                });
                RegisterCallback<DetachFromPanelEvent>(_ => EditorApplication.update -= Refresh);
            }

            void OnChanged(ChangeEvent<bool> evt) =>
                SpaceMouseSettings.HorizonLocked = evt.newValue;

            void Refresh() =>
                SetValueWithoutNotify(SpaceMouseSettings.HorizonLocked);
        }

        // ── FreeLook toggle ──────────────────────────────────────────────────

        [EditorToolbarElement(id, typeof(SceneView))]
        sealed class FreeLookToggle : EditorToolbarToggle
        {
            public const string id = "SpaceMousePro/FreeLook";

            FreeLookToggle()
            {
                text    = "Free Look";
                tooltip = "SpaceMouse: first-person camera, no pivot";
                this.RegisterValueChangedCallback(OnChanged);
                RegisterCallback<AttachToPanelEvent>(_ =>
                {
                    Refresh();
                    EditorApplication.update += Refresh;
                });
                RegisterCallback<DetachFromPanelEvent>(_ => EditorApplication.update -= Refresh);
            }

            void OnChanged(ChangeEvent<bool> evt)
            {
                if (evt.newValue)
                    SpaceMouseSettings.NavigationMode = NavigationMode.FreeLook;
            }

            void Refresh() =>
                SetValueWithoutNotify(SpaceMouseSettings.NavigationMode == NavigationMode.FreeLook);
        }
    }
}
