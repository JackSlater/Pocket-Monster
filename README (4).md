# Pocket Monster — Phone Addiction Mini‑Game

A Unity‑based WebGL mini‑game created to illustrate how different types of digital media addiction affect a population’s productivity and behavior.

---

## 🎮 Gameplay Overview

Your city begins productive and peaceful. Villagers work together to construct buildings—until you begin dropping different types of **phones** that introduce distractions and harmful behaviors.

### Phone Types
- **Social Media Phone (Red)** – Causes villagers to become violent.
- **Streaming Service Phone (Yellow)** – Causes villagers to stop working and become idle.
- **Main Stream Media Phone (Blue)** – Slows the movement speed of villagers.
- **Gambling Phone (Green)** – Causes villagers to become destructive and damage the city.

### Goal  
Survive as long as possible while watching the population slowly collapse due to addiction.

---

## 🧱 Key Features
- Multiple phone types with unique negative effects
- Villager AI with working, idle, violent, and destructive behaviors
- Progress bar representing population productivity
- Building system with randomized spawn locations
- Restart button that resets the current scene cleanly
- Smooth phone physics (fall‑to‑ground behavior)
- Fully playable WebGL build for browser deployment

---

## 📂 Project Structure
```
Assets/
├── Prefabs/
├── Scripts/
│   ├── Villager.cs
│   ├── Building.cs
│   ├── BuildingManager.cs
│   ├── Phone.cs
│   ├── PhoneDropManager.cs
│   ├── UIManager.cs
│   └── GameManager.cs
├── Sprites/
└── Scenes/
```

---

## 🚀 Build & Deployment (WebGL)

1. In Unity: **File → Build Settings**
2. Select **WebGL** → *Switch Platform*
3. Click **Build**
4. Save the output into a `Build/` folder
5. Commit the folder to GitHub
6. Enable **GitHub Pages** and set the build folder as the source

---

## 🖼️ Screenshots

### How To Play Screen
![How To Play](/mnt/data/a56bcd78-ac12-4723-ae6d-c8c53d83e65b.png)

### Village Starting State
![Village Starting](/mnt/data/f5eb0ac6-4b60-464a-acfc-678743fde130.png)

### Gameplay Example
![Gameplay](/mnt/data/8e2c86db-9bdf-4055-ba5e-44786a9c2f91.png)

---

## 📜 License
This project is for educational use. Free to modify or expand.

