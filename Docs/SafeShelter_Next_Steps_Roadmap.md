# SafeShelter next steps

This roadmap starts from `main` at `4e2a099`. Dev A's desktop planner and Netcode work are merged. Dev B's `Assets/Scenes/SafeShelter.unity` contains the hall, four beds, supply table, exit, XR rig, and teleportation setup. Furniture sync passed a two-process Windows loopback test in `DesktopNetworkTest`. The main scene still needs networking wired in and a Windows-to-Quest test.

**First checkpoint** — Move and rotate `Bed_01` on Windows and see both changes live on Quest in the main shelter scene.

## 1. Connect Windows and Quest in the main scene

### Dev A — desktop and host

- [ ] Walk Dev B through `DesktopNetworkTest`. Hand off `NetworkBed`, `SafeShelterNetworkPrefabs.asset`, and the working `DesktopRig` and `NetworkSession` setup. Point out the camera, floor collider, layout, session, and prefab-list references.
- [ ] Agree on how Quest supplies the host address. If Dev B builds a headset join control, expose a shared connection entry point in `SessionController` that takes the address and port and reuses its validation. Agree on the hall floor height and world X/Z bounds with Dev B.
- [ ] Run the Windows host from the integrated `SafeShelter` scene. Give Dev B your LAN IPv4 address and UDP port `7777`. Verify mouse selection, dragging, rotation, and reset in that scene.

### Dev B — main scene and Quest

- [ ] Add one `NetworkManager`/`UnityTransport` pair, register `SafeShelterNetworkPrefabs.asset`, and connect the session references. Replace or upgrade `Bed_01` with the networked bed setup and assign it as starting furniture. Keep a single visible instance at its original position.
- [ ] Wire the desktop camera and mouse input for Windows and the XR rig for Quest. Keep the session object and shared furniture available on both. Give Quest a way to join as client using Dev A's LAN address.
- [ ] Build the integrated scene for Quest. On the headset, verify tracking, controller rays, and teleportation. Join the Windows host from the same reachable LAN or Wi-Fi.

### Acceptance test

Launch both builds from the same scene revision and confirm they show the same hall and starting bed pose. Drag, rotate, and reset the bed on Windows. Verify Quest sees each change and stays read-only for furniture. Close and reopen the Quest client, then verify it receives the current layout on join.

If the client cannot join, check the Windows LAN address, UDP `7777`, Windows firewall, Wi-Fi client isolation, and build revisions. `127.0.0.1` points to the device running the client.

## 2. Build the keep-clear interaction

Dev A and Dev B can work in parallel against the [integration contract](IntegrationContract.md). Use one zone for the MVP.

### Dev A — shared state and overlap

- [ ] Implement `ConstraintManager` with `RequestZone`, `RequestClearZone`, `CurrentZone`, `ZoneChanged`, and `GetConflict`. Validate and normalize the two floor points on the host. Replicate one snapshot containing the corners, active state, and conflict state so late joiners get the current zone.
- [ ] Check the active zone against the world X/Z bounds of present furniture colliders, including child colliders. Count edge contact as overlap. Recalculate when the zone changes or furniture is moved, rotated, added, removed, or reset.
- [ ] Add mouse-based debug marking through the same request methods. Use it to verify red-to-green changes and clearing in the Windows test scene.
- [ ] After the one-bed test passes, prepare network-ready versions of the remaining beds and supply table with Dev B's visuals. Verify they work with the existing planner and reset logic before handing them over for scene wiring.

### Dev B — VR input and presentation

- [ ] Implement two controller-ray hits on the floor. The first sets a local preview corner. The second sends both world-space points to `RequestZone`. Provide a clear action through `RequestClearZone` and brief in-headset control instructions.
- [ ] Render the finalized zone from `CurrentZone` and `ZoneChanged` on both platforms. Place it slightly above the floor. Hide it when inactive, show red for overlap, and show green when clear.
- [ ] Wire the constraint and presentation components into `SafeShelter.unity`. Add the remaining network-ready furniture when Dev A hands it over. Set up a repeatable starting layout with a bed blocking the marked passage.

### Acceptance test

On Quest, mark a rectangle under the obstructing bed. Both devices show red. Move the bed clear on Windows and both turn green. Move it back and both turn red. Verify clearing hides the zone, reset restores the expected furniture layout and overlap status, and a late join receives the current zone and furniture.

## 3. Rehearse and prepare the submission

- [ ] **Dev A** verifies a fresh Windows launch and host startup, writes the README setup steps, and records the desktop view and a brief explanation of host authority and overlap checks.
- [ ] **Dev B** verifies a fresh Quest build and headset join, rehearses the marking sequence, and records the Quest view and a brief explanation of the controller interaction.
- [ ] **Both** rehearse the full sequence from a clean launch, then assemble and review the 3–5 minute video with the code walkthrough. Check that reviewers can access the repository and play the video. Confirm the organizer's time zone and submit before the November 9 noon deadline listed in the project plan.

Route-test bots and extra polish come after the Windows-to-Quest demo is repeatable. If time is short, record the working two-person interaction first.

## Working agreement

- **Dev A owns** reusable desktop, networking, furniture, and constraint code and prefabs. **Dev B owns** `Assets/Scenes/SafeShelter.unity`, Quest input, and zone presentation. Dev B makes the main-scene edits for each handoff.
- Both use Unity `6000.3.27f1` and the committed package versions. Branch from current `main` and coordinate edits to shared package and XR settings before committing them.
- At each handoff, share the commit, assets to wire, and one concrete test the other developer can run.

[Networking milestone](NetworkingMilestone.md) · [Dev B integration guide](SafeShelter_Developer_B_Friday_Guide.md#7-integrate-with-developer-as-networking) · [Three-day implementation plan](SafeShelter_3_Day_Implementation_Plan.md) · [Project plan](SafeShelter_Project_Plan.md)
