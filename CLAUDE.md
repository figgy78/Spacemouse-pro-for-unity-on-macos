# SpaceMouse Pro Driver — Claude Instructions

## SDK Reference

The folder `3DxMacWare SDK/` contains the official 3Dconnexion SDK for macOS:

- `3DxMacWare SDK.pdf` — full API reference (framework functions, structs, constants)
- `3DxSampleCode/Connexion Client Test/` — reference app using the newer `SetConnexionHandlers` API (matches our driver)
- `3DxSampleCode/3DxMultithreadedValues/` — multithreaded polling example
- `3DxSampleCode/3DxDemo/` and `3DxSNAxisDemo/` — additional usage examples

Consult this folder when working on native plugin changes, adding new SDK features (button labels, prefs pane integration, device switch modes), or verifying API usage.
