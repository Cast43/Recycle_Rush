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
* **Genre and Style**: *Recycle Rush* is a 3D, top-down, cooperative roguelike game developed in Unity[cite: 3]. It features a simple LowPoly visual style modeled using Magica Voxel[cite: 3].
* **Target Audience**: The game is designed for children and youth between 9 and 16 years old to encourage environmental responsibility and sustainability[cite: 3].
* **Narrative Goal**: Players control a small robot fighting against the "Trupe do Lixão" (Trash Troupe) in a polluted wasteland[cite: 3]. The main objective is to recycle the trash and clean the environment to save the forest's last surviving seedling[cite: 3].
* **Controls**: The game uses standard PC controls: WASD for movement, Spacebar to dash, 'F' to interact with the environment, and the Mouse to select upgrades[cite: 3].

## ⚔️ Combat & Enemies
* **Automatic Combat**: The robot automatically fires energy beams at the closest enemy, allowing the player to focus on movement and positioning[cite: 3].
* **Enemy Behaviors**: The enemies represent threats to terrestrial life (SDG 15) and are categorized into four distinct types[cite: 3]:
  * **Swarm (Melee)**: Trash bags and cardboard boxes that attack in massive numbers to overwhelm the player[cite: 3].
  * **Fast Dash**: Discarded tires that move quickly and perform high-speed rolling attacks[cite: 3].
  * **Ranged**: Enemies that keep their distance and shoot trash or glass projectiles, creating hazardous zones on the map[cite: 3].
  * **Tanks**: Heavy metal scrap and large trash piles that move slowly but absorb massive amounts of damage, protecting weaker enemies[cite: 3].
* **Co-op Revival System**: The robot's health is tied to its structural integrity[cite: 3]. If health drops to zero, the robot deactivates, but active teammates can revive it by standing nearby for a period of time[cite: 3]. The match is lost only if all players are deactivated[cite: 3].

## ♻️ Resources & Sustainability (SDGs 7, 12, & 15)
* **Energy Management**: Energy is the central resource used to perform actions like shooting and dashing[cite: 3]. Players must utilize clean energy sources (solar, wind, and biodigesters) to recharge[cite: 3].
* **Recycling System**: Defeated enemies drop various recyclable materials, including paper, plastic, glass, metal, organic, and electronic waste[cite: 3]. 
* **Progression**: Collecting enough recyclable materials allows the player to "recycle" them into new skills, attributes, or technological upgrades[cite: 3].

## 💡 Upgrades
* **Energy Upgrades**: Includes a *Wind Motor* (generates energy while moving), a *Solar Panel* (provides passive energy), and a *Biomass Generator* (converts enemy waste into massive energy, but negatively affects the plant's integrity)[cite: 3].
* **Damage Upgrades**: Includes *Lightning Beams* (deals area-of-effect damage to nearby enemies), *Chemical Beams* (applies damage-over-time), and general base damage increases[cite: 3].
* **Mobility Upgrades**: Includes *Thrusters* (consumes energy to dash forward), *Overvoltage* (spends energy for a temporary speed boost), and *Better Wheels* (permanently increases base movement speed)[cite: 3].
* **Defensive Upgrades**: Includes *Oil Change* (increases base maximum health) and *Hull Upgrade* (grants health regeneration)[cite: 3].

## 💻 UI & Technical Architecture
* **User Interface (UI)**: The screen displays the robot's health, current experience/materials gathered, progress to the next level, the current wave indicator, and the upgrade selection menu[cite: 3].
* **Software Design**: The game is built using Unity's Data-Oriented Technology Stack (DOTS) and Entity Component System (ECS) to ensure high performance[cite: 3]. Multiplayer synchronization is handled natively using Unity Relay and Netcode for Entities[cite: 3].

## ⚙️ How to Run
1. Ensure you have **Unity Hub** and a compatible Unity LTS version 6000.0.41f1.
2. Clone this repository:
   ```bash
   git clone [https://github.com/Cast43/Recycle_Rush.git](https://github.com/Cast43/Recycle_Rush.git)
