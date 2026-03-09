# SpaceMouse Pro Driver for Unity on macOS

A Unity Editor plugin that brings full 6DOF SpaceMouse Pro navigation to the Unity Scene View on macOS. Supports orbit and free-look camera modes, per-axis remapping, speed controls, and a live Scene View overlay.

---

## Requirements

- **macOS** (Apple Silicon or Intel)
- **Unity 6** (6000.x) or later
- **3DxWareMac** driver installed — [download from 3Dconnexion](https://3dconnexion.com/us/drivers/)
  The driver must be running for the plugin to receive device input.
- A **SpaceMouse Pro** (or compatible 3Dconnexion device)

---

## Installation

1. Download or clone this repository.
2. Copy the following folders into your Unity project's `Assets` folder:
   ```
   Assets/Plugins/
   Assets/SpaceMousePro/
   ```
   Your project's `Assets` folder should end up containing both `Plugins/` and `SpaceMousePro/`.

3. Open Unity. In the **Project** window, select:
   ```
   Assets/Plugins/macOS/libSpaceMouseBridge.dylib
   ```
   In the **Inspector**, confirm:
   - **Platform** is set to **Editor only**
   - **CPU** is **ARM64** (Apple Silicon) or **Any CPU**

4. Reopen or reload the project. On load you should see a console message:
   ```
   [SpaceMouse] Initialized. Device connected.
   ```

> **macOS Security:** If macOS blocks the dylib, go to **System Settings → Privacy & Security** and allow it.

---

## Settings

Open **Edit → Preferences → SpaceMouse Pro**.

| Section | Description |
|---|---|
| **Device** | Shows driver and connection status |
| **Speed** | Translation (0–10) and Rotation (0–10) speed multipliers |
| **Navigation** | Switch between Orbit and Free Look; toggle Lock Horizon (Roll) |
| **Dead Zone** | Minimum axis threshold before input registers |
| **Axis Mapping** | Remap each output channel to any raw device axis; invert per-channel |
| **Scene View** | Button to show the navigation overlay directly in the Scene View |
| **Raw Axis Monitor** | Live readout of all 6 raw axes and button bitmask (expandable) |

Click **Reset to Defaults** to restore all settings to factory values.

---

## Scene View Overlay

The overlay adds compact navigation controls directly to the Scene View toolbar.

**To enable the overlay:**

1. Open a **Scene View** window.
2. Either:
   - Click **Show Navigation Overlay** in **Preferences → SpaceMouse Pro → Scene View**, or
   - Click the **☰ (overlay menu)** icon in the top-right of the Scene View and enable **SpaceMouse**.

The overlay shows three toggle buttons:

| Button | Action |
|---|---|
| **Orbit** | Camera orbits around a fixed pivot point |
| **Free Look** | First-person camera — rotates in place, no pivot |
| **Lock horizon** | Suppresses roll so the horizon stays level |

---

## How to Use

### Navigation Modes

**Orbit** (default)
The camera moves on a sphere around the current pivot point. Rotation always faces the pivot. Pan slides the pivot; zoom changes the orbit radius.

**Free Look**
First-person mode. The camera rotates around its own position and translates freely through space. No pivot point.

Switch modes from the Scene View overlay or from **Preferences → Navigation → Rotation Mode**.

### Lock Horizon

Enable **Lock Horizon (Roll)** to prevent the camera from rolling. The roll axis mapping remains configured but its output is suppressed. Disable it to allow free roll.

### Axis Mapping

Each of the 6 output channels (Pan Horizontal, Pan Vertical, Zoom, Tilt, Spin, Roll) can be freely mapped to any raw device axis. Set a channel's source to **— None —** to disable it entirely. Each channel can also be individually inverted.

Click **Reset to Defaults** to restore the factory mapping.

---

## Building the Native Plugin (optional)

The compiled `libSpaceMouseBridge.dylib` is included and ready to use. If you want to rebuild it from source:

```bash
cd NativePlugin
bash build.sh
```

Requires Xcode command-line tools and the 3DxWareMac driver installed at `/Library/Frameworks/3DconnexionClient.framework`.

---

## Copyright & Disclaimer

This plugin communicates with the **3DconnexionClient.framework**, which is proprietary software developed and distributed by **3Dconnexion**. The SpaceMouse Pro hardware and all associated drivers are products of 3Dconnexion.

This project is an **independent, open-source integration** and is **not affiliated with, endorsed by, or supported by 3Dconnexion**. No 3Dconnexion proprietary code or SDK files are distributed with this plugin — only the public framework API is used at runtime.

---

## Credits

Made by **Erlend Dal Sakshaug**
📧 esakshaug@gmail.com

Contributions and feedback welcome — open an issue or pull request on GitHub.
