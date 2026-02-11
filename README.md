# Antymology - Evolutionary Ant Colony Simulation

An evolutionary algorithm that optimizes ant colony behavior for nest production in a 3D voxel environment. Worker ants and a queen evolve cooperative strategies over generations to maximize nest construction.

**CPSC 565 - Emergent Computing | Winter 2026 | University of Calgary | Author: Youssef Shawky**

---

## Evolutionary Algorithm

### How It Works
1. **Spawn:** 1 Queen + 14 Workers per generation
2. **Evaluate:** 50 seconds of autonomous behavior
3. **Fitness:**
   - Queen: `nests × 100 + (50 if alive)`
   - Workers: `currentHealth + (20 if alive)`
4. **Select:** Top 50% become breeding pool
5. **Elite:** Top 3 preserved unchanged
6. **Crossover:** Offspring inherit genes from 2 parents
7. **Mutate:** 30% chance, ±0.2 variance

### The Three Genes

| Gene | Range | Purpose |
|------|-------|---------|
| `explorationRate` | 0.1-1.0 | Random movement frequency |
| `diggingProbability` | 0.0-0.5 | Terrain clearing near queen |
| `foodSeekingWeight` | 0.3-2.0 | Hunger threshold multiplier |

These genes evolved to produce **emergent cooperative behavior** - workers learned to keep queens alive by sharing health, resulting in significantly more nests.

---

## Implementation

### Ant Behavior (AntAgent.cs)

Decision priority system (every 0.5s):
1. **Queen Support** - Share health if queen is weak
2. **Survival** - Eat mulch when health < `maxHealth × (0.6 × foodSeekingWeight)`
3. **Digging** - Clear space near queen (probability-based)
4. **Exploration** - Random movement (rate-based)

Key mechanics:
- Health decays 2 HP/s (4 HP/s on acidic blocks)
- Mulch consumption refills health fully
- Can't eat mulch if another ant occupies it
- Movement limited to ±2 block height
- Zero-sum health sharing between ants

### Queen Behavior (QueenAnt.cs)

- Produces nest blocks every 3 seconds
- Cost: 33% max health per nest
- Magenta color, 1.5× scale for visibility
- Limited exploration (stays near nests)

### Evolution System (EvolutionManager.cs)

**Fitness Calculation:**
```csharp
Queen: nestsProduced × 100 + (alive ? 50 : 0)
Worker: currentHealth + (alive ? 20 : 0)
```

**Genetic Operators:**
- **Elitism:** Preserves top 3 solutions
- **Crossover:** 50/50 gene inheritance from two parents
- **Mutation:** 30% chance per gene, ±0.2 range with clamping

---

## Results

### Evolution Progress
```
Gen 1: Fitness 200  | 2 nests  | Queen died   | 99/100 alive
Gen 2: Fitness 950  | 9 nests  | Queen alive  | 100/100 alive ★ NEW RECORD
```

### Emergent Behaviors
1. **Cooperative Health Sharing** - Workers evolved to sustain queen
2. **Strategic Positioning** - Ants stayed closer to queen
3. **Balanced Digging** - Moderate digging (0.1-0.2) outperformed extremes

### Gene Convergence (5+ generations)
- `explorationRate`: 0.3-0.5 (moderate)
- `diggingProbability`: 0.1-0.2 (strategic)
- `foodSeekingWeight`: 1.0-1.5 (proactive)

---

## Setup

**Requirements:** Unity 6000.3.x, TextMeshPro

1. Open project in Unity 6000.3.x
2. Load `SampleScene`
3. Press Play
4. Use WASD + Mouse to navigate

**Adjustable Parameters** (EvolutionManager Inspector):
```
populationSize = 15
generationDuration = 50s
mutationChance = 0.3
mutationAmount = 0.2
eliteCount = 3
```

---

## Technical Notes

### Key Design Decisions

**Coroutine Initialization:** Ants delay position initialization by 1 frame to ensure WorldManager completes terrain generation first, preventing crashes.

**Mulch Occupancy Check:** Prevents simultaneous consumption by checking if another ant occupies the same block position.

**Priority-Based AI:** Hierarchical decision tree ensures critical actions (queen support) override less important ones (exploration).

---

## File Structure
```
Assets/
├── Components/Agents/
│   ├── AntAgent.cs         // Base behavior, genes, decision AI
│   ├── QueenAnt.cs         // Nest production
├   └──EvolutionManager.cs     // Genetic algorithm
└── AntSimulationUI.cs      // Real-time stats display
```

---

## Author - Youssef Shawky

Created for CPSC 565 - Emergent Computing  
University of Calgary, Winter 2026

Forked from: [DaviesCooper/Antymology](https://github.com/DaviesCooper/Antymology)