# SafeShelter — YC-Style Demo Video Script

## One-line description

**SafeShelter helps two people plan a temporary disaster shelter together: one arranges the room from a laptop, while the other walks through it in VR and marks the spaces that must stay clear.**

## Production brief

This is a **direct, YC-style product demo**, not a cinematic explainer. It should feel like a founder showing a real product: simple language, quick cuts, real screen recordings, no speculative claims, and the working interaction shown before the code. Target **3:15–3:45** to meet the hackathon’s 3–5 minute requirement while retaining the YC rhythm.

Use a founder voiceover or on-camera introduction. Use real captures of the Windows host and Quest view; show them side-by-side for every shared-state moment. Add only restrained labels and captions. Do **not** create fake product screens, claim safety certification, or show the optional bot/route-test feature unless it has been verified in the exact build being recorded.

## Script

| Time | Visual direction | Spoken script | On-screen text |
|---|---|---|---|
| 0:00–0:15 | Founder on camera, then immediately cut to side-by-side laptop overview and Quest first-person views of the same hall. | “We’re building SafeShelter. When a school hall becomes a temporary shelter, a layout can look fine on a floor plan but be hard to move through in real life. SafeShelter lets a planner and an in-room inspector fix that together before anyone moves equipment.” | `SafeShelter`<br>`Plan from above. Inspect from within.` |
| 0:15–0:35 | Desktop host: orbit the hall, select a bed, drag it, then press `R` to rotate it. The Quest view must show the same bed moving. | “The planner works from a laptop. They can select, move, rotate, add, remove, and reset furniture. The Quest user sees the same layout at human scale, live.” | `Laptop planner` / `Quest inspector` |
| 0:35–0:55 | Quest: walk or teleport to the narrow passage beside the intentionally misplaced bed. Pause briefly so the obstruction is obvious. | “The person in the room can spot problems that are easy to miss from above. Here, this bed obstructs a passage. Instead of trying to explain that with a voice call, they make the requirement spatial.” | `A blocked passage` |
| 0:55–1:25 | Quest: raycast two floor corners around the passage. Show the first-corner preview, then show the finished red keep-clear zone on both views. | “They point at two corners on the floor to mark a keep-clear zone. The zone is sent to the session host and appears for both users. It turns red because the bed overlaps the area.” | `Mark a keep-clear zone`<br>`RED = furniture overlap` |
| 1:25–1:50 | Split screen: planner drags the bed outside the zone; show both zones turn green at once. | “Now the planner sees the exact constraint from above and moves the bed. The host checks the furniture footprint against the zone. Once the overlap is gone, both users see green.” | `GREEN = clear` |
| 1:50–2:05 | Keep the final green scene on screen; optionally show Reset Layout and the original setup returning. | “That is the core loop: inspect in VR, turn an observation into shared geometry, and resolve it together from the view that is best for the task.” | `Inspect → Mark → Resolve` |
| 2:05–2:15 | Transition to the code editor, with the project scene or a diagram as a small picture-in-picture. | “Here’s how we built it.” | `How it works` |
| 2:15–2:35 | Highlight `SessionController.cs`, then `PlatformRigSelector.cs`. | “The Windows app starts as the host. Quest joins over the local network with an in-headset IP and port panel. One Unity scene chooses the desktop planner rig on Windows and the XR rig on Android, so both users share the same world.” | `Unity 6 · OpenXR · XR Interaction Toolkit` |
| 2:35–2:55 | Highlight `SharedFurniture.cs`, `NetworkFurniture.cs`, and `FurnitureLayout.cs`. | “Furniture uses Netcode for GameObjects. The host owns the shared furniture state; its position, rotation, presence, and runtime additions replicate to connected clients. That keeps the immersive view synchronized without giving the client editing authority.” | `Host-authoritative shared furniture` |
| 2:55–3:20 | Highlight `ConstraintManager.cs` and `ZoneState.cs`; briefly point at the network variable, RPC request, normalization, and overlap test. | “The keep-clear zone is deliberately small shared state: two floor corners and a conflict flag. Both users can request a zone, but the host validates the points, checks all present furniture bounds, and replicates the result. We synchronize the meaningful constraint, not a stream of mesh updates.” | `Two points → one shared constraint` |
| 3:20–3:35 | Highlight `QuestKeepClearMarker.cs` and `ZoneDebugView.cs`; return to the green live demo. | “On Quest, the controller ray collects the two floor points. The view component only renders the replicated result: red when there is overlap, green when there is not. That separation makes the feedback consistent for both collaborators.” | `VR input · Shared feedback` |
| 3:35–3:45 | End on both views showing the clear corridor. | “SafeShelter is a prototype for making spatial feedback actionable across devices—before real equipment is moved. Thank you.” | `SafeShelter` |

## Exact code-demo talking points

Keep this portion visual and concise. Do not read code line-by-line.

- **`SessionController.cs`** — One `NetworkManager` and Unity Transport session; Windows hosts, Quest joins using a LAN IPv4 address and port. Host-only furniture editing is intentional.
- **`PlatformRigSelector.cs`** — Enables the desktop orbit/planner rig on desktop and the XR rig on Android, while leaving shared networking active for both.
- **`SharedFurniture.cs` + `NetworkFurniture.cs`** — Encapsulates move/rotate/reset behavior and pairs it with a server-authoritative `NetworkTransform` plus replicated presence for scene furniture.
- **`FurnitureLayout.cs`** — Registers the baseline furniture, spawns runtime beds through the network, and restores the deterministic starting layout.
- **`ConstraintManager.cs` + `ZoneState.cs`** — Stores one server-write network snapshot: active state, normalized corners, and conflict state. The host validates inputs and tests furniture collider bounds in X/Z; touching an edge counts as an overlap.
- **`QuestKeepClearMarker.cs` + `ZoneDebugView.cs`** — Quest captures two raycast points on the floor; the shared view renders the replicated zone as red or green. The desktop has the same marking path for debugging without a headset.

## Prompt / handoff for an automated video-production workflow

Use the following as the production input. It is intentionally specific so an automated workflow can assemble a video from real recordings without inventing product behavior.

```text
Create a 3 minute 30 second YC-style product demo for “SafeShelter.”

Tone: direct, technical, credible, founder-led, fast but readable. Do not make it cinematic or use stock footage. Use real screen recordings only. Start with the problem, show the working product within 15 seconds, then explain implementation after the live demo. Use clean sans-serif captions and restrained cuts.

Required source clips:
1. Founder intro (optional but preferred).
2. Windows desktop host: orbit, select, drag, rotate, reset furniture.
3. Quest first-person capture: movement/teleportation and controller-ray floor marking.
4. Synchronized split-screen capture of a bed moving on desktop and Quest.
5. Synchronized split-screen capture of a red keep-clear zone becoming green after the desktop moves a bed.
6. Code-editor captures of SessionController, PlatformRigSelector, SharedFurniture, NetworkFurniture, FurnitureLayout, ConstraintManager, ZoneState, QuestKeepClearMarker, and ZoneDebugView.

Required story:
- A temporary-shelter layout can look fine from above but fail at human scale.
- A laptop planner edits shared furniture while a Quest inspector experiences the same hall.
- The inspector marks two floor corners for a keep-clear zone.
- The shared zone is red when furniture overlaps and green after the planner clears it.
- Explain host-authoritative Netcode state, compact replicated zone state, host validation/overlap detection, and separate Quest input/rendering.

Truth constraints:
- Call SafeShelter an exploratory planning/training prototype, not a certified safety, accessibility, or evacuation tool.
- Red and green represent geometric furniture overlap only.
- Do not show or claim route testing, bots, AI layout optimization, voice chat, or other features unless supplied with verified capture.
- Do not fabricate UI, device behavior, performance metrics, user testimonials, or network results.

Use the timed script in Docs/Demo_Video_Script.md as the voiceover source. Make shared-state moments split-screen. End on the clear green corridor and “SafeShelter.”
```

## Pre-record checklist

- Start the Windows app as host and join Quest on the same reachable LAN.
- Begin with a bed visibly intersecting the corridor that will be marked.
- Capture the red-to-green transition in both device views in one continuous take.
- Verify the exact build used for recording; remove unverified features from the narration.
- Keep the explicit scope statement: colors indicate geometric overlap, not a real-world safety determination.
