# Your first SafeShelter test

The goal today is small: add a test bed to a room and arrange it with your mouse.
The bed is temporary geometry. Your teammate can finish the real scene and furniture in parallel.

## Open and run

1. In Unity Hub, add the project folder `C:\Users\adhik\Desktop\XR-Hackathon` if it is not listed.
2. Open it with **Unity 6000.3.27f1**, the version recorded by the project. Let packages import.
   This environment has 6000.3.28f1 installed; install the matching version rather than silently
   upgrading the shared project.
3. For laptop testing, select Windows in **File > Build Profiles**. In **Edit > Project Settings >
   XR Plug-in Management**, turn off **Initialize XR on Startup** on the **Standalone** tab only.
   Keep Android XR enabled for your teammate. These settings have not been changed by this patch.
4. In Unity's Project window, open **Assets > SafeShelter > Scenes > DesktopTest**.
   Open it by itself, not additively alongside another scene.
5. Open **Window > General > Console**. Resolve any red errors before running. Then press
   Unity's Play button at the top and click inside the **Game** view.

## Try your planner

1. Click **Add Bed** in the panel, then click somewhere on the visible floor outside the panel.
   A bed should appear at that point. You choose its position; it is not automatically arranged.
2. Left-drag a bed to move it. Click a bed to select it and press **R** to rotate it by 90 degrees.
3. Hold the right mouse button and move the mouse to orbit. Scroll to zoom.
4. Select a bed, then click **Remove Selected**.
5. Click **Reset Layout**. Added beds should disappear and the original bed should return to
   its starting position, even if you removed it.
6. Click **Add Bed**, then press **Escape**. Placement should cancel without adding anything.
7. Stop Play mode. Changes made while playing are temporary and do not edit the saved scene.

Beds can overlap at this stage. Furniture collision, wall constraints, and keep-clear checks
come later. Adding requires a hit on the assigned floor collider; dragging uses a horizontal
plane and can move an existing bed beyond the floor. Reset recovers it.

## Where the coding happens

In **Assets > SafeShelter > Scripts**, double-click a script to open it in your code editor:

- `DesktopPlanner.cs` reads the mouse/keyboard and displays the test controls.
- `FurnitureLayout.cs` creates/removes beds and restores the starting layout.
- `SharedFurniture.cs` moves/rotates/resets an individual bed.
- `DesktopOrbitCamera.cs` controls the overhead camera.

The test scene already connects these components in the Inspector. No scripts or prefabs need
to be created manually. You can inspect DesktopRig to see those connections.

Your teammate's final bed visuals can replace the test bed's Frame/Mattress/Pillow children,
keeping the root SharedFurniture component and suitable colliders. Place the root at floor
height, preserve human-scale dimensions, and keep the prefab reference intact. Coordinate
prefab edits; B supplies visuals and A maintains behavior/networking.

## What has been checked

Only source and scene/prefab reference checks have run here. Unity import/compilation,
Play mode, and the interactions above still require testing in the matching editor.
There is no networking yet. After this local test works, the next engineering step is
making a host-created bed appear on a second connected application, before testing Quest.

The fuller Inspector wiring and integration instructions are in DesktopMilestone1.md.
