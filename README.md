# COP RUNNER 6000

A competitive online multiplayer cops-and-robbers chase game set in a **real-world urban environment** generated from OpenStreetMap data.

---

## What is this game?

COP RUNNER 6000 is an **asymmetric multiplayer game** for 2–8 players. One team plays as **Cops**, the other as **Robbers**. The game takes place on a real 3D map of Seoul, South Korea, built on-the-fly from live map data every round.

- **Robbers** must survive for 3 minutes without getting caught.
- **Cops** must hunt down and catch every robber before time runs out.

---

## Roles

| Role | Objective | Special Ability |
|------|-----------|----------------|
| **Cop (경찰)** | Catch all robbers within 3 minutes | — |
| **Robber (도둑)** | Survive until the timer runs out | Sprint (hold Shift) |

**Win conditions:**
- Cops win if all robbers are caught before time expires.
- Robbers win if at least one robber is still free when the 3-minute timer ends.

---

## How to Play

### 1. Lobby

1. Enter your **nickname** (up to 10 characters).
2. **Create a room** or **join an existing room** (up to 8 players).
3. Select your role — **Cop** (blue) or **Robber** (red).
4. Click **Ready** when you're set.
5. The room master can start the game once all players are ready with valid roles assigned.

> Teams must be balanced: there must be at least 1 cop and 1 robber, and the number of cops cannot exceed the number of robbers.

### 2. Map Loading

After the game starts, the map is generated from real OpenStreetMap data — 3D buildings and roads will appear automatically. Wait for all players to finish loading before the round begins.

### 3. Countdown

A 3-second countdown plays, then the round starts.

### 4. Playing

Players spawn at random road locations on the map. The 3-minute timer begins immediately.

- **Cops** chase robbers on foot.
- **Robbers** flee and hide using the city layout.
- A cop catches a robber by getting within **1.5 units** of them.
- Caught robbers are locked in place and enter **spectator mode**.

### 5. Game Over

When all robbers are caught or the timer hits 0:00, the results screen is shown.

- The room master can **restart** for another round.
- Any player can **leave** the room.

---

## Controls

| Action | Keyboard | Gamepad |
|--------|----------|---------|
| Move | WASD | Left Stick |
| Look | Mouse | Right Stick |
| Sprint *(Robbers only)* | Hold Shift | Gamepad Button |
| Adjust camera distance | Mouse Scroll | Scroll Wheel |
| Toggle cursor | ESC | — |

---

## Game Info

| Setting | Value |
|---------|-------|
| Max players per room | 8 |
| Round duration | 3 minutes (180 seconds) |
| Catch radius | 1.5 units |
| Robber sprint speed | 9 units/sec |
| Cop move speed | 5 units/sec |
| Map location | Seoul, South Korea |
| Map size | ~0.1 km radius |

---

## Tech

- **Engine**: Unity
- **Networking**: Photon PUN 2
- **Map Data**: OpenStreetMap (Overpass API)
- **Map Visual**: Google Maps Static API
