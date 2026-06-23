# Recycle Rush 🤖🌳

**Recycle Rush** is a multiplayer "Vampire Survivors-like" game centered around sustainability and environmental preservation. In the game, players control a robot tasked with protecting a central tree from endless threats by collecting and recycling materials.

## 🎓 Academic Research & Performance Benchmark
This project serves as the practical foundation for an academic thesis. The core objective is to conduct a rigorous **stress test** comparing the performance of Unity's traditional **MonoBehaviour** approach against the **Data-Oriented Technology Stack (DOTS)**. 

By rendering and processing thousands of entities simultaneously, this repository demonstrates the massive performance gains and frame-rate stability achieved by transitioning from Object-Oriented Programming (OOP) to Data-Oriented Programming (DOP) using ECS.

## 🚀 Key Features
- **Vampire Survivors Gameplay Loop:** Fast-paced survival against massive enemy hordes with continuous resource gathering.
- **Eco-Friendly Theme:** Core mechanics revolve around recycling materials to upgrade the robot and defend the tree.
- **Multiplayer Architecture:** Built to support synchronized gameplay sessions over the network.
- **Extreme Performance:** Engineered to maintain high frame rates despite having thousands of active entities and complex physics interactions on screen.

## 🛠️ Technology Stack
- **Unity Engine**
- **DOTS & ECS (Entity Component System):** Cache-friendly memory layout for extreme optimization.
- **Burst Compiler & C# Job System:** Aggressive multithreading and highly optimized native code compilation.
- **Netcode for Entities:** Robust networking infrastructure integrated natively into the ECS workflow.
- **Languages:** C#, ShaderLab, HLSL.

## 🎮 Core Concept & Gameplay
* **Genre and Style**: *Recycle Rush* is a 3D, top-down, cooperative roguelike game developed in Unity. It features a simple LowPoly visual style modeled using Magica Voxel.
* **Target Audience**: The game is designed for children and youth between 9 and 16 years old to encourage environmental responsibility and sustainability.
* **Narrative Goal**: Players control a small robot fighting against the "Trupe do Lixão" (Trash Troupe) in a polluted wasteland. The main objective is to recycle the trash and clean the environment to save the forest's last surviving seedling.
* **Controls**: The game uses standard PC controls: WASD for movement, Spacebar to dash, 'F' to interact with the environment, and the Mouse to select upgrades.

## ⚔️ Combat & Enemies
* **Automatic Combat**: The robot automatically fires energy beams at the closest enemy, allowing the player to focus on movement and positioning.
* **Enemy Behaviors**: The enemies represent threats to terrestrial life (SDG 15) and are categorized into four distinct types:
  * **Swarm (Melee)**: Trash bags and cardboard boxes that attack in massive numbers to overwhelm the player.
  * **Fast Dash**: Discarded tires that move quickly and perform high-speed rolling attacks.
  * **Ranged**: Enemies that keep their distance and shoot trash or glass projectiles, creating hazardous zones on the map.
  * **Tanks**: Heavy metal scrap and large trash piles that move slowly but absorb massive amounts of damage, protecting weaker enemies.
* **Co-op Revival System**: The robot's health is tied to its structural integrity. If health drops to zero, the robot deactivates, but active teammates can revive it by standing nearby for a period of time. The match is lost only if all players are deactivated.

## ♻️ Resources & Sustainability (SDGs 7, 12, & 15)
* **Energy Management**: Energy is the central resource used to perform actions like shooting and dashing. Players must utilize clean energy sources (solar, wind, and biodigesters) to recharge.
* **Recycling System**: Defeated enemies drop various recyclable materials, including paper, plastic, glass, metal, organic, and electronic waste. 
* **Progression**: Collecting enough recyclable materials allows the player to "recycle" them into new skills, attributes, or technological upgrades.

## 💡 Upgrades
* **Energy Upgrades**: Includes a *Wind Motor* (generates energy while moving), a *Solar Panel* (provides passive energy), and a *Biomass Generator* (converts enemy waste into massive energy, but negatively affects the plant's integrity).
* **Damage Upgrades**: Includes *Lightning Beams* (deals area-of-effect damage to nearby enemies), *Chemical Beams* (applies damage-over-time), and general base damage increases.
* **Mobility Upgrades**: Includes *Thrusters* (consumes energy to dash forward), *Overvoltage* (spends energy for a temporary speed boost), and *Better Wheels* (permanently increases base movement speed).
* **Defensive Upgrades**: Includes *Oil Change* (increases base maximum health) and *Hull Upgrade* (grants health regeneration).

## 💻 UI & Technical Architecture
* **User Interface (UI)**: The screen displays the robot's health, current experience/materials gathered, progress to the next level, the current wave indicator, and the upgrade selection menu.
* **Software Design**: The game is built using Unity's Data-Oriented Technology Stack (DOTS) and Entity Component System (ECS) to ensure high performance. Multiplayer synchronization is handled natively using Unity Relay and Netcode for Entities.

## ⚙️ How to Run
1. Ensure you have **Unity Hub** and a compatible Unity LTS version 6000.0.41f1.
2. Clone this repository:
   ```bash
   git clone [https://github.com/Cast43/Recycle_Rush.git](https://github.com/Cast43/Recycle_Rush.git)
