# SafeShelter final demo operator notes

Release acceptance requires the Windows player and Quest APK built from one merged commit. A successful scene validation alone does not establish release readiness. See `Handoff.md` for the checks actually completed and the remaining blockers.

1. Keep `SafeShelter.exe`, `SafeShelter_Data/`, `UnityPlayer.dll`, Mono runtime and the other generated player files together under `Builds/Integration/`. Launch `SafeShelter.exe` outside Unity. The planner hosts UDP 7777. Use the LAN IPv4 address shown by the planner, not 127.0.0.1, on Quest.
2. Put both devices on the same reachable LAN. With Quest developer mode and USB debugging authorized, run `adb devices`, then `adb install -r Builds/Integration/SafeShelter.apk`. Launch SafeShelter and enter the Windows LAN address and port 7777. Confirm the host shows a connected client.
3. Check head tracking, controller selection and floor targeting. Left thumbstick is smooth locomotion at 1.15 m/s. Right controller retains teleport and snap turning. Keep teleport available if smooth movement is uncomfortable. The camera height remains tracked.
4. Choose Clear layout and Normal activity; start a run. Walk from SOUTH AISLE VIEW toward NORTH CROSSING VIEW. Residents should be human sized, walk to both service destinations, wait and return.
5. Choose Blocked layout and Meal rush; restart. Mark the floor ticks at (-1, 0, -0.65) and (1, 0, 0.65) with clear floor hits from both Quest aiming rays. Confirm red on both devices. Move out of traffic and let the full 90 seconds finish.
6. Choose Clear layout to move the blocker into BED PARKING at (4, 0.225, -2); confirm green. Replay the same Meal rush for 90 seconds. For both measured passes observe around (0, 0, 5.65), off the central lane. Compare completed trips, peak queue and mean aisle wait for the same profile on Windows.
7. In a separate unmeasured walkthrough, step into the lane and check that nearby people yield. Do not treat this as a measurement pass.
8. Reset Layout must restore all 22 baseline props and the blocked bed pose, remove added beds and stop the run. Repeat after a clean launch; counters must restart from zero.

Record headset model, exact merged commit, LAN join result, tracking, locomotion, snap turning, teleport, red/green state on both devices, resident motion/queues, blocked/clear comparison numbers, reset and clean replay. Do not substitute the desktop simulation for physical headset checks.
