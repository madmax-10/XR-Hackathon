# Networking milestone: two copies of SafeShelter

## What this increment does

The local DesktopTest scene and SharedBed prefab stay available. DesktopNetworkTest uses a
separate NetworkBed prefab with NetworkObject, server-authoritative NetworkTransform, and
NetworkFurniture. One SessionController references one NetworkManager/UnityTransport pair.
There is no player avatar or second networking manager.

The host can move/rotate the starting bed, add/remove beds, and reset the layout. Connected
clients receive transforms, runtime spawns/despawns, and the starting bed's presence state.
The client planner is read-only, and direct furniture mutation methods also reject client
writes. A failed connection does not switch to offline editing.

Removing the starting bed hides its renderers/colliders through a server-written NetworkVariable;
the network root remains active so Reset Layout can restore it. Removing an added bed network-
despawns it. Reset deletes added beds and restores the starting bed and its original pose.
Persistent state supports clients that join after furniture has been edited.

Packages are pinned to Netcode for GameObjects 2.7.0 and Unity Transport 2.6.0. These versions
and their dependencies were checked against Unity's registry and package source. Keep the
manifest and Unity-resolved lockfile together. No other dependency upgrades are intended.
The implementation follows the official [NetworkManager API](https://docs.unity3d.com/Packages/com.unity.netcode.gameobjects@2.7/api/Unity.Netcode.NetworkManager.html)
and [NetworkTransform API](https://docs.unity3d.com/Packages/com.unity.netcode.gameobjects@2.7/api/Unity.Netcode.Components.NetworkTransform.html).

## First-time Unity steps

1. Stop Play mode. Let Unity import packages and finish recompiling. Check the Console for errors.
2. Open **Tools > SafeShelter > Open Networking Test**. This opens DesktopNetworkTest; if the
   assets are absent it generates them from the local test. It never overwrites an existing
   network test scene on a repeated invocation. The main VR scene remains B's responsibility.
3. Press Play. You should see Network Session controls above the planner. Before choosing a
   role, furniture editing is disabled.
4. Leave IPv4 at **127.0.0.1** and port at **7777**. Click **Start Host**. The status should say
   Host ready; the planner can now move the bed. These values are for same-laptop testing.
5. Stop Play before making the executable. Choose **Tools > SafeShelter > Build Windows
   Networking Test**. It builds only this test scene to **Builds/DesktopNetworkTest/SafeShelter.exe**.
   It temporarily selects windowed mode, Mono scripting, and disables Standalone XR initialization for the build,
   then restores the editor settings. Setup adds/enables DesktopNetworkTest in the editor's build
   scene list so the Editor host can resolve it; existing entries and Android XR settings are preserved.
   This test build excludes unused inference-package shader variants to avoid lengthy ML shader
   compilation. The filter is inactive during ordinary builds; no package is removed.

## Run both copies on one laptop

1. Back in Unity, press Play in DesktopNetworkTest and click Start Host.
2. In Windows File Explorer, open the repository's **Builds > DesktopNetworkTest** folder.
   Double-click **SafeShelter.exe**. Keep the entire build folder together, including its Data folder.
3. In the executable, keep **127.0.0.1 / 7777**, then click **Join Client**. It should report
   Connected as client. Its planner controls are disabled; camera orbit/zoom still work.
4. In the Editor's Game view, drag and rotate the bed. Confirm the executable follows.
5. On the host, add two beds at different floor points. Confirm exactly two added beds appear
   on the client. Move one, remove it, and check the change arrives there too.
6. Remove the starting bed, then Reset Layout. Both views should end with only the original
   starting bed at its initial position and rotation.
7. Close the executable, move the bed and add another on the host, then reopen the executable
   and Join Client. Confirm it receives the current layout, including the added bed.
8. Try editing in the client. It must not be able to add, move, rotate, remove, or reset furniture.
9. Stop the host. The client should report disconnected and remain read-only. **Reload Session**
   unloads the old manager and reloads the test scene before retrying. Restart/reconnect the two
   applications for a clean test; retaining layouts across host restarts is outside this milestone.

Only start one host on port 7777. If connection fails, verify both copies use the same build,
scene and package versions, start the host first, and check the Console/player logs. An ordinary
application close/disconnect is supported; seamless reconnect or session persistence is not promised.

## Different laptop or Quest, later

Both devices use the same network test scene/build content. The client must enter the host
laptop's LAN IPv4 address instead of 127.0.0.1. The host listens on all local interfaces.
Actual cross-device testing still needs firewall and LAN/client-isolation checks. No Relay,
Lobby, account login, cloud server, or public internet hosting is introduced here.

B integrates the same prefab/list, one manager/transport, and session references into their
main scene, with DesktopRig enabled only on desktop and the Quest XR rig on Android.
SessionController cannot start an Android host. Quest movement/marking and shared zones are
not implemented in this increment.

## Opt-in automated integration probe

Run **Tools/Run-NetworkSmokeTest.ps1** against the development executable to automate the
two-process test. It starts only its own hidden test processes, waits for their results, and
stops those processes afterward; results/logs stay in a unique project Temp directory.

Development builds include an inert test probe. It runs only with explicit command-line flags:
`--safeshelter-role host` or `--safeshelter-role client`, plus
`--safeshelter-smoke <existing absolute results folder>`. Optional address/port flags are
`--safeshelter-address` and `--safeshelter-port`. Each test run needs an empty results folder.

Start the headless host first. After its host-ready file appears, launch the headless client
with the same result directory. The probe checks late-join transforms and spawning, rejected
client mutations, live move/rotation, runtime despawn, scene-bed hiding, and reset. It writes
host-pass/client-pass or a failure file and exits. Files coordinate test phases; each client
assertion examines actual NGO-replicated GameObjects. It does not fabricate shared state.

## Verification

Verified with Unity **6000.3.27f1** in an isolated project copy:

- Package resolution and compilation succeeded; only the intended NGO/Transport entries were added
  to the lockfile. The original local scene and visual bed prefab were preserved.
- The Windows Mono development build succeeded. The initial IL2CPP attempt identified the missing
  optional module; the test builder now temporarily chooses Mono. Unused inference shader
  compilation is filtered only during this test build.
- Two actual Windows player processes passed over loopback: late-join transforms and added beds,
  rejected client mutations, live host movement/rotation, added-bed despawn, starting-bed hiding,
  reset, host disconnect without client write authority, and client reload with exactly one manager.
- The exact tested source and Unity-generated assets were copied into the main project. The ready
  executable is Builds/DesktopNetworkTest/SafeShelter.exe. Keep its entire build directory together.
- Logs/markers for this run are in Temp/NetworkingSmoke-d175bc84b7964924bd4ebd73cab7da05;
  the Unity build log is Temp/SafeShelterVerification-Final.log. Temp and Builds are git-ignored.

Still manual: Editor-host plus player-client interaction, mouse controls and rendering in this
networking scene, Windows firewall/LAN behavior between devices, Android build, and Quest testing.
Headless loopback tests do not establish those. Zones and VR marking remain a later increment.
