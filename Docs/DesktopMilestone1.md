# SafeShelter desktop milestone 1

## Scope and repository findings

Implemented: local desktop camera orbit/zoom, explicit furniture selection, floor-plane dragging,
90-degree world-Y rotation, adding/removing beds, and reset to the starting scene layout. The isolated
`Assets/SafeShelter/Scenes/DesktopTest.unity` references the reusable
`Assets/SafeShelter/Prefabs/SharedBed.prefab`. One unit represents one meter; the test floor is
12 by 8 meters, its top is Y=0, and the bed root starts at Y=0. The bed uses existing template
URP materials and primitive geometry. Existing interactable prefabs are XR tutorial objects,
so their grab, tutorial, and affordance scripts are not appropriate dependencies for this bed.

This scene is **local only**. The subsequent networking increment is documented separately in
NetworkingMilestone.md. Zone requests, overlap status, debug marking, and bots are still deferred.
The local scene alone does not meet the plans' cross-device acceptance criterion.

Inspected both supplied plans, the repository plan, package manifest/lock, scene contents,
template/sample scripts and prefabs, and project/build settings. No AGENTS.md was found in
the repository (including hidden directories) or its ancestor locations. The initial working
tree was clean. No existing SafeShelter scripts, furniture, NetworkManager, or main
SafeShelter scene were present. Existing scenes are BasicScene and SampleScene plus samples.

Project version: **6000.3.27f1**. At the initial inspection only **6000.3.28f1** was installed.
The matching editor was subsequently installed and used. Existing packages at initial inspection
include URP 17.3.0, Input System 1.20.1, XRI 3.5.1, and OpenXR 1.17.1. NGO and Unity
Transport are absent; Multiplayer Center does not supply either runtime. Input handling is
already set to the new Input System. No package, project setting, existing scene, or existing
prefab was changed.

## Contract interpretation

Only the two supplied plan documents were available. The three-day plan labels its
Integration API contract as conceptual, not drop-in code. The user subsequently confirmed
these plans are the contract and delegated engineering decisions. See
[IntegrationContract.md](IntegrationContract.md) for the concrete decisions governing future
increments. The notes below record the initial ambiguities; that document now resolves them
except package release selection, which requires package resolution and API verification.

- Furniture API: implemented as public instance methods `SafeShelter.SharedFurniture.MoveTo(Vector3)`
  and `Rotate90()`, plus `ResetPose()` and a local `PoseChanged` event. World X/Z are edited;
  initial root Y is preserved. The namespace and extra reset/event API are implementation
  choices, not new agreed contract requirements. Stable furniture IDs remain unspecified.
- Host authority: the plans consistently assign all shared mutations to the laptop host.
  These local mutation methods are the future authority boundary. Before networking, settle
  the NGO/Transport versions, spawn/scene strategy, IDs, and behavior before connection or
  after disconnection. Do not expose the current ungated local component to a network client.
- Zone input: retain the agreed conceptual `ConstraintManager.RequestZone(cornerA, cornerB)`,
  `RequestClearZone()`, and `GetConflict()` interface. Exact types, namespace, clear/inactive
  semantics, renderer change notification/snapshot API, sender permissions, and RPC validation
  are unspecified. No placeholder ConstraintManager is introduced that could conflict with B's work.
- Coordinates: plans describe meters, world X/Z, and an axis-aligned rectangle. Agree on a common
  hall origin, floor Y, and bounds with B before implementing requests. Settle degenerate/outside
  requests, edge-touch overlap, and the conservative AABB footprint policy before overlap code.
- Ownership: the original plan assigns hall building to A; the three-day plan and user's explicit
  request assign main-scene ownership to B. Follow the user's instruction: B owns the main scene;
  A supplies reusable scripts/prefabs and this isolated test scene.
- A future SessionController must reference the main scene's one NetworkManager. None is added
  in milestone 1. Do not add another manager when integrating the desktop rig.

## Exact Unity setup

1. Install/open this repository with **Unity 6000.3.27f1**, matching your teammate. Let Unity resolve
   the existing packages. Do not upgrade the project just because 6000.3.28f1 is installed here.
2. For laptop testing, select Windows in **File > Build Profiles** and switch platform if needed.
   In **Edit > Project Settings > XR Plug-in Management**, select the **Standalone** tab and turn
   off **Initialize XR on Startup** for the desktop configuration. Coordinate any serialized
   settings change with B; keep Android XR enabled. This setting has not been changed in this patch.
3. Open `Assets/SafeShelter/Scenes/DesktopTest.unity` alone, rather than additively with a VR scene.
   Confirm the Console has no compile/import errors before pressing Play.
4. Inspect `DesktopRig > DesktopPlanner`: Planner Camera references PlannerCamera; Floor Plane
   references FloorReference; Placement Surface references TestFloor's BoxCollider; Layout references
   FurnitureLayout on the same rig. On FurnitureLayout, Bed Prefab references the SharedBed prefab
   asset and Initial Furniture contains the scene's SharedBed instance. Inspect the camera's
   DesktopOrbitCamera: Focus references FloorReference. These are already serialized in the scene.
5. Press Play and focus the Game view. The panel must say **milestone 1 (local)**. Enable Game-view
   Gizmos if you want the yellow selected-collider wireframes; the panel shows selection regardless.
6. Optional Windows executable: add only DesktopTest to the active desktop Build Profile's scene
   list, then Build and Run. Existing build settings still select SampleScene; they were preserved.

## Manual acceptance tests

1. Left-click a visible part of the bed. The panel changes to `Selected: SharedBed`.
2. Hold left mouse and drag. The bed follows the mouse without jumping its center to the cursor;
   its root Y stays at 0. Release, move the mouse again, and confirm movement stops.
3. Press R once: rotate 90 degrees about world Y. Press four times total: recover the original
   orientation. Drag after rotating and confirm grab offset/height remain stable.
4. Right-drag to orbit and scroll to zoom. Pitch is limited to 25–85 degrees and distance to 4–24
   meters. Right-dragging while left is held cancels furniture dragging.
5. Click empty floor: selection clears. R must then do nothing. Click the help panel: it must not
   select or move furniture underneath. Click Reset Layout after moving/rotating: recover the
   exact initial position and rotation. Repeat this several times.
6. While dragging, switch away from the app and back. Furniture must stop following until a new
   left press. Re-enter Play mode and confirm the initial pose is captured again.
7. Optional executable: repeat steps 1–6 outside the Editor. Selection text is available even when
   editor Gizmos are unavailable.
8. Click Add Bed, then click the visible floor away from the panel. A new bed should appear there
   and be selected. It should remain at rest until a new drag. Move and rotate the new bed.
   Repeat to create several beds. Clicking outside the floor leaves placement pending;
   Escape cancels without creating anything. This placement step raycasts the assigned floor collider.
9. Select an added bed and click Remove Selected: it disappears. Remove the original bed too.
   Click Reset Layout: all added beds disappear and the original returns at its starting pose.
   Repeat add/remove/reset and confirm there are no leftover active beds in the Hierarchy.

Dragging uses an infinite horizontal plane through FloorReference, rather than hall bounds.
Furniture collision, wall containment, snapping, and placement validation are not implemented.
If moved offscreen, Reset Layout recovers the bed. There are no rigidbodies or XR grab components
on the bed. The selection ray respects solid foreground colliders and ignores triggers.

## Main-scene integration for B

1. Keep SharedBed as a linked prefab instance, with root scale (1,1,1), no nonuniform parent
   scale, and a root transform whose Y represents its resting height. Move the instance into
   the intended initial layout before Play; ResetPose captures that instance pose in Awake.
2. Copy DesktopRig from DesktopTest into the main scene. Assign its floor-plane reference to a
   serialized transform at the hall's floor height and its camera focus to the hall center.
   Assign every initial SharedFurniture instance explicitly to FurnitureLayout's Initial Furniture
   list; set its Bed Prefab to the shared prefab asset. Set the planner's Layout reference to
   FurnitureLayout and its Placement Surface to the hall floor collider. Newly created beds are
   registered automatically by FurnitureLayout. Keep that component enabled only for the desktop role.
3. Activate DesktopRig only for the desktop role; disable the XR rig/camera there. On Quest,
   disable the entire DesktopRig including its camera and AudioListener. DesktopPlanner also
   disables itself on Android, but that alone does not switch cameras or XR startup settings.
4. Preserve the same world coordinates on desktop and Quest. Future networking must wrap the
   SharedFurniture API with server-only writes and add NetworkObject/server-authoritative
   NetworkTransform to the same shared root. No desktop code writes transform poses directly.

## Verification status

Verified by repository/static inspection: isolated new asset paths, linked bed prefab, explicit
serialized references, unique asset GUIDs, resolved project-local asset references and local
file IDs, and no changes to existing tracked files. The inherited URP additional-light script
reference requires Unity package import to resolve. Source/document whitespace checks pass.

The initial delivery had not been run in Unity. The user subsequently opened Unity 6000.3.27f1
and confirmed the local planner workflow works, including placement and manipulation. The
local milestone does not establish Windows build, networking, or Quest behavior. The networking increment
is now documented in NetworkingMilestone.md. Zone requests, overlap state, and mouse debug
marking follow that increment; bots remain deferred.
