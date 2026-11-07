# SafeShelter integration handoff

The authoritative scene is `Assets/Scenes/SafeShelter_Integration.unity` (GUID
`5a3a3e8cd09322d44b3a17d69529b0ac`). Open it alone.

## Changed files and reasons

- `Assets/Scenes/SafeShelter_Integration.unity`: repaired references with Unity 6000.3.27f1 APIs;
  retained the existing NetworkBed, removed the overlapping plain Bed_01, kept Bed_02/03/04
  and SupplyTable visuals and poses, and added host-authoritative networking to those four objects.
  All five starting objects are explicitly registered in FurnitureLayout. NetworkBed is grounded
  against HallFloor. Add Bed references the registered NetworkBed asset. SessionController now
  lives on NetworkSession so disabling DesktopRig on Quest does not disable networking.
  HallCenter, camera focus, floor collider, platform selector and Quest panel references are assigned.
  Missing optional tutorial UI sprites/materials and audio references were cleared as scene
  overrides; the original prefab assets and XR rig structure were preserved.
- `Assets/SafeShelter/Scripts/FurnitureLayout.cs`: additions use the prefab's resting root height
  rather than the raycast's Y before Awake captures the initial pose. Existing host-only Spawn,
  runtime Despawn, scene-object presence replication, and reset paths are reused.
- `Assets/SafeShelter/Scripts/SessionController.cs`: configurable host auto-start; LAN IPv4/port
  preferences; optional Quest JSON configuration; 30-second connection timeout; Quest host/loopback
  rejection; status logs. Command-line test settings do not overwrite saved LAN preferences. A temporary
  persistent coroutine owner lets reload complete after the manager/session object is destroyed.
  Existing command-line options, transport and callbacks are reused.
- `Assets/SafeShelter/Scripts/PlatformRigSelector.cs` and `.meta`: early platform selection.
  Windows/Editor use DesktopRig; an Android player uses the original XR Origin. Desktop tutorial
  XR objects are disabled with the XR role. Networking stays active on both platforms.
- `Assets/SafeShelter/Scripts/QuestSessionPanel.cs` and `.meta`: world-space controller UI with a
  numeric IPv4/port keypad, Connect and Reload. It uses the existing XRI UI event system and hides
  after connection. No physical keyboard or guessed host IP is required.
- `Assets/SafeShelter/Editor/IntegrationSetup.cs` and `.meta`: repeatable Unity repair, validation,
  Windows development build, and Android build entry points. Scene references and identities are
  generated through Unity/NGO, not hand-assigned YAML hashes. Windows development builds temporarily
  use Mono/windowed mode and restore the configured backend/fullscreen settings afterward.
- `Assets/SafeShelter/Editor/NetworkingTestShaderStripper.cs`: the existing opt-in filter also
  skips unused inference-package shaders during integration development builds.
- `Assets/SafeShelter/Tests/NetworkSmokeProbe.cs`: existing two-process test supports multiple
  starting furniture objects and their original heights/rotations instead of assuming one bed at zero.
- `Tools/Run-NetworkSmokeTest.ps1`: loopback is explicit, so LAN preferences cannot redirect the test.
- `ProjectSettings/ProjectSettings.asset`: explicitly requests Android INTERNET permission for LAN sockets.
- `Assets/XR/XRGeneralSettingsPerBuildTarget.asset`: Standalone Initialize XR on Startup disabled;
  Android enabled setting and its OpenXR loader preserved.
- `ProjectSettings/EditorBuildSettings.asset`: integration is the sole enabled launch scene for
  shared builds. The original scene assets are retained.
- `Docs/IntegrationHandoff.md`: this report and device test procedure.

The integration scene and its existing `.meta` were initially untracked; they remain uncommitted.
Existing edits to DesktopTest, DesktopNetworkTest, OpenXRPackageSettings and DefaultNetworkPrefabs
were preserved. Do not mistake those user/editor changes for changes made to the original test scenes
by this repair. No packages, firewall settings, SDK settings, commits or pushes were added.

## Final configuration

- One NetworkManager and one UnityTransport on NetworkSession. Protocol version 1, scene management
  enabled, no player prefab, SafeShelterNetworkPrefabs list containing NetworkBed. Host binds
  `0.0.0.0:7777`; the client destination is configured separately.
- SessionController is on NetworkSession and references that same manager/transport. Windows and
  Editor auto-start a host; explicit `--safeshelter-role client` overrides host auto-start for tests.
- One DesktopRig with planner, layout, and PlannerCamera. Its orbit focus is HallCenter and placement
  collider is HallFloor. Selection accepts only explicitly registered SharedFurniture instances.
- Original XR Origin, tracked controllers, teleport provider, HallFloor teleport area/collider and
  Floor layer retained. Quest disables DesktopRig; Windows disables the XR rig. No shared origin
  offset or alternate furniture-coordinate system was introduced.
- Four starting beds (one retained NetworkBed and three original hall beds), plus the original
  supply table. All use SharedFurniture, NetworkFurniture, NetworkObject and server-authoritative
  NetworkTransform. Added furniture uses the existing registered NetworkBed prefab.
- Scene objects stay spawned when removed: replicated presence hides their visuals/colliders so
  reset can restore them. Added objects are despawned/destroyed. Reset restores all five baseline
  objects and removes runtime additions. Starting objects are never instantiated again on host start.
- Both build entry points explicitly use the integration scene. No custom build profile assets
  with separate scene overrides were found.

## Validation

Passed: Unity editor/player C# compilation; full scene serialized-reference audit (including inherited
prefab components); no cross-scene references; direct scene GUID audit; one manager/session/planner/XR
origin; unique nonzero IDs for five scene NetworkObjects; registered NetworkBed spawn prefab;
initial furniture references; preserved Floor collider/layer; Windows development build.

Final corrected-build result: **PASS** for late join, runtime spawn, client write rejection,
live movement/rotation, runtime despawn, baseline hide/reset, disconnect, and clean session reload.
The test also requires exactly one active desktop camera and AudioListener. It used the integration
scene with five starting network objects. Logs and PASS markers are in
`Temp/NetworkingSmoke-96f65da1cf1c4857970132be0d8bfd60/`.

The first runtime run caught a reload lifecycle bug after passing all furniture checks: destroying
NetworkManager stopped its own reload coroutine. The temporary persistent runner fixed it, and the
full test passed on a rebuilt executable. There are no outstanding failing automated checks.

Both corrected development builds succeeded:

- Windows: `Builds/Integration/SafeShelter.exe`, with the accompanying Data/DLL files kept together.
  Build log: `Temp/IntegrationWindowsReloadFix.log`.
- Quest ARM64 APK: `Builds/Integration/SafeShelter.apk`.
  Build log: `Temp/IntegrationAndroidFinal.log`.
- APK inspection confirmed `arm64-v8a`, min/target SDK 34, INTERNET permission and retained VR
  head-tracking/hand-tracking declarations. No SDK, graphics backend or OpenXR interaction-profile
  changes were made. Standalone scripting backend and fullscreen settings were restored after build.
- No C# compilation or final build errors. The logs include nonblocking inherited package warnings.
  Earlier repair passes exposed missing optional tutorial assets; those references were repaired
  with default UI materials/sprites or cleared audio references, and the final audits pass.

No headset was attached (`adb devices` returned an empty list). Manual mouse interaction, Quest
UI/rays, rendering, tracking, teleportation, physical LAN connectivity, timeout UX, and reconnecting
an actual Quest remain untested. The original rig and teleportation configuration are preserved;
a successful APK build and Windows process test do not establish headset behavior.

## Cross-device test

1. On Windows, run `ipconfig`. Use the IPv4 address of the adapter on the network shared with Quest;
   ignore loopback, disconnected adapters and unrelated VPN adapters. Both devices must be able to
   reach each other on that LAN; a guest network can isolate clients.
2. Run `Builds/Integration/SafeShelter.exe`. It auto-starts the host on UDP 7777 and displays status.
   To explicitly choose a port: `SafeShelter.exe --safeshelter-role host --safeshelter-port 7777`.
   Stop other copies using that port first. No firewall configuration was changed by this work.
3. Launch the Quest app. On its LAN panel, use Back to clear the old address, then enter the Windows
   IPv4 address using the controller ray and UI select action. Edit IP / Port switches the field;
   default port is 7777. Click Connect. The panel hides on connection and returns on disconnect.
   Use Reload after a failed attempt or disconnect to create a clean session, then Connect again.
4. For file-based configuration, create `safeshelter-lan.json` containing
   `{"address":"YOUR_WINDOWS_LAN_IPV4","port":"7777"}` and copy it to the Quest app's persistent
   files directory. Current Android application ID is `com.DefaultCompany.VRTemplate`:
   `adb push safeshelter-lan.json /sdcard/Android/data/com.DefaultCompany.VRTemplate/files/safeshelter-lan.json`.
   The file overrides saved UI preferences at each startup; remove/update it when changing networks.
5. Install a produced APK with `adb install -r Builds/Integration/SafeShelter.apk`. If an APK has not been produced here, open Unity 6000.3.27f1, switch to Android in File > Build
   Profiles, verify SafeShelter_Integration is the enabled scene, select the connected Quest with
   developer mode/USB debugging authorized, and choose Build and Run. Preserve existing Android
   OpenXR settings. Alternatively run the batch entry point `SafeShelter.Editor.IntegrationSetup.BuildAndroid`.
6. On Windows, left-drag each bed and the supply table, press R to rotate, Add Bed and click the
   floor, then Remove Selected. Check every change on Quest. Remove a starting bed, then Reset
   Layout: all five originals return to their saved poses and runtime additions disappear on both.
7. Teleport/walk through the hall on Quest, confirm tracking and UI selection, disconnect/reload,
   reconnect, and verify a late-joining client sees the current layout. Client-side furniture editing
   is intentionally rejected. Confirm only one effective camera/AudioListener per platform.

Editor desktop check: open only SafeShelter_Integration, press Play, and focus Game view. The host
starts automatically. Try mouse selection/drag, R rotation, Add Bed, Remove Selected and Reset Layout;
right-drag orbits and wheel zooms. An Android build target still uses the desktop role in the Editor.

The two-process smoke test command is:
`powershell -ExecutionPolicy Bypass -File Tools/Run-NetworkSmokeTest.ps1 -Executable Builds/Integration/SafeShelter.exe`.
It uses a separate UDP port (17777), headless Windows players and marker/log files under Temp.
This does not establish headset rendering, controller input, teleportation or physical LAN behavior.

Future zone marking can send two world-space floor Vector3 points to an ownership-independent
server RPC on a shared NetworkBehaviour. Validate sender/points on the host there. This task adds no
zone/constraint implementation; Floor layer, controller interaction and world coordinates remain available.