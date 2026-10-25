# Delivery Driver — WinForms MVP

A small reference prototype for the Programming on Windows midterm. The project targets **.NET Framework 4.7.2** and uses only Windows Forms, GDI+ drawing, and the standard .NET libraries. It needs no external image or audio files.

## Run it

1. Open `DeliveryDriverDemo.sln` in Visual Studio on Windows.
2. Make sure the **.NET Framework 4.7.2 Developer Pack/Targeting Pack** is installed.
3. Set `DeliveryDriverDemo` as the startup project and press **F5**.

The form opens maximized. Resize it to see that the grid and mouse-to-cell conversion stay aligned.

## Controls

- Hold the left mouse button on `S` (start) and drag one road tile at a time.
- A route can only extend onto a 4-direction adjacent drivable tile that has not already been visited.
- Dragging back onto the previous tile undoes the last step.
- **GO** becomes enabled only after the route reaches `F` (finish).
- **Undo last tile**, **Clear route**, and **Retry / Reset** reset the route or state.

## Demo checklist

- Hardcoded 16 × 10 map with houses, roads, intersections, mud, highway, start, and finish.
- Double-buffered map panel with resize-aware cell size and offsets.
- Drag-to-draw path validation, backtracking undo, clear, ink limit, and Go validation.
- Driver advances tile-by-tile; mud slows it and highway speeds it up.
- Two deterministic traffic vehicles: one loops and one ping-pongs.
- Same-cell and swap collision checks; win, crash, timeout, and retry states.
- Minimal HUD for state, remaining time, and route length.

## Suggested routes for a live demo

- **Risky / likely to crash:** draw straight along the top road from `S` to `F`.
- **Safer detour:** from `S`, go down the left road to row 4, right one tile to column 2, down to row 7, travel along the lower road to column 13, go up to row 4, right to column 14, then up to `F`. It is longer, but avoids the central traffic block for most of the route.

Traffic uses fixed routes and intervals, so play outcomes are deterministic for the same start conditions. Exact outcomes can still depend on how much time elapses between pressing GO and the Windows timer's first update.

## Source layout

- `GameMap.cs` — the hardcoded map and drivable tile rules.
- `TrafficVehicle.cs` — loop / ping-pong route timing.
- `GameEngine.cs` — route rules, game states, movement, collision, win/lose checks.
- `GamePanel.cs` — map rendering and mouse conversion.
- `GameForm.cs` — HUD, buttons, and WinForms timer.
- `Program.cs` — application entry point.

## Important coursework note

The project brief says students should design their own UI and not rely on generative AI to create the entire UI automatically. Use this prototype to understand the logic and interaction, then adapt/rebuild the layout and visual style yourselves before submitting it. The optional items from the final-project document (leaderboard, sound, menus, multiple levels, level files, traffic lights, and polished exception handling) are intentionally not included in this MVP.
