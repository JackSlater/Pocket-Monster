Pocket Monster — Phone Addiction Mini-Game

A simple Unity-based mini-game designed as part of a school project to demonstrate the harmful effects of phone addiction through interactive gameplay. Built in Unity 2D and deployed via WebGL, this game simulates how a village’s productivity collapses as more villagers become distracted by smartphones.

🎮 Gameplay Overview

You begin with a peaceful village of hard-working villagers who cooperate to construct buildings.
However, once smartphones enter the world, everything changes:

Core Mechanics

Villagers build buildings as long as they remain focused.

Click anywhere in the game world to drop a phone.

Only one villager becomes phone-addicted each time a phone is dropped.

Phone-addicted villagers stop working and wander aimlessly.

The village’s population productivity bar drops as addiction spreads.

Violent villagers (rare event) may attack one villager and then become idle.

Destructive villagers may damage buildings at the same rate as a normal builder builds.

When productivity becomes too low → the game ends.

A Restart button lets you instantly reset the scene without loading a new one.

🧱 Key Features

Unity WebGL build designed to run in-browser.

Villager AI system:

Building behavior

Phone-addiction behavior

Aggressive and destructive variants

Random building spawning & construction system

Sprite-based 2D environment with roads, buildings, and villager visuals

Smooth phone physics (falling from drop point to ground)

UI overlays:

Status text

Time text

Productivity progress bar

Restart button

🛠️ Tech Stack

Unity (2022+)

C# Scripts

Villager.cs

Building.cs

BuildingManager.cs

Phone.cs

PhoneDropManager.cs

UIManager.cs

GameManager.cs

WebGL Deployment

HTML/CSS/JS (GitHub Pages hosting)

📂 Project Structure
Assets/
│
├── Prefabs/
│   ├── Villager.prefab
│   ├── Building.prefab
│   ├── Phone.prefab
│   └── UI elements
│
├── Scripts/
│   ├── Villager.cs
│   ├── Building.cs
│   ├── BuildingManager.cs
│   ├── Phone.cs
│   ├── PhoneDropManager.cs
│   ├── UIManager.cs
│   └── GameManager.cs
│
├── Sprites/
│   ├── Villagers/
│   ├── Buildings/
│   ├── Road/
│   └── Backgrounds/
│
└── Scenes/
    └── SampleScene.unity

🚀 How to Build for WebGL

Open Unity → File → Build Settings

Select WebGL, click Switch Platform

Click Build

Output a folder named Build/

Commit & push the Build/ folder to GitHub

Enable GitHub Pages → set source to the Build/ directory

Your WebGL game is now online

(Make sure .gitignore does NOT ignore your Build folder.)

📦 Running Locally

Unity automatically runs the game in the Game View when you press Play.

To run the WebGL build locally, host it with a simple HTTP server:

python -m http.server


Open your browser → http://localhost:8000

🖼️ Screenshots

(You can add screenshots here later)

📝 Credits

Created by Jack Slater

📜 License

This project is for educational use. Modify, expand, or repurpose freely.
