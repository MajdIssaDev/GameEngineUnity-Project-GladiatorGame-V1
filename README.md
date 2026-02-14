# Project Gladiator: Advanced Combat & Systems Demo
**A High-Performance 3D Action-RPG Prototype Built in Unity**

---

## Technical Highlights

### **1. State-Driven Combat System**
Implemented a robust `PlayerCombat` architecture that manages complex transitions between light attacks, heavy attacks, and blocking. 
* **Mechanic:** Frame-perfect parrying system using a Coroutine-based startup window to sync logical "block" activation with physical shield animations.
* **Optimization:** Used state-priority logic to prevent input conflicts and "animation-canceling" exploits.

### **2. Dynamic Weapon & Animation Overrides**
The system supports hot-swapping weapons by utilizing `AnimatorOverrideControllers`.
* **Feature:** Dynamically updates attack logic and `WeaponDamage` scripts at runtime, allowing for modular equipment sets (e.g., swapping a sword for a mace) without rewriting core logic.

### **3. Resource & Energy Management**
Integrated a comprehensive `Stats` and `HealthScript` system that governs every action.
* **Logic:** Implemented a `TrySpendEnergy` pattern for heavy attacks and combos, preventing "spamming" and forcing strategic play.

---

##  Gameplay Demo: The Parrying System
https://medal.tv/games/screen-capture/clips/m7jJwNLp0eYWWFBxI?invite=cr-MSxxWkMsMTg3NjQzNTA1&v=15

**Technical Breakdown of the Parry:**
1. **Input Detection:** Polled via `Input.GetKeyDown(KeyCode.E)`.
2. **Visual Start:** Immediately triggers the "weapon up" animation and locks movement via `PlayerLocomotion`.
3. **Logic Sync:** Uses an `EnableBlockRoutine` (IEnumerator) to delay the actual `StartBlocking()` call until the animation frames match the gameplay expectation.

---

## 🛠️ Tech Stack & Patterns
* **Engine:** Unity 2022+ / C#
* **Architecture:** Component-Based Design, Interface Implementation (`ICombatReceiver`).
* **Patterns:** Finite State Machine (FSM) for combat states, Observer-lite for animation events.