using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Antymology.Components.Agents;
using Antymology.Terrain;

public class EvolutionManager : MonoBehaviour
{
    [Header("Prefabs")]
    public GameObject antPrefab;
    public GameObject queenPrefab;
    
    [Header("Settings")]
    public int populationSize = 15;
    public float generationDuration = 50f;
    
    [Header("Evolution")]
    public float mutationChance = 0.3f;
    public float mutationAmount = 0.2f;
    public int eliteCount = 3;
    
    [Header("Spawning")]
    public Vector3Int queenSpawnGridPos = new Vector3Int(20, 0, 20);
    public float workerSpawnRadius = 5f;
    
    // Made public so UI can access them
    public int currentGeneration { get; private set; } = 0;
    public float generationTimer { get; private set; } = 0f;
    
    private List<AntAgent> currentPopulation = new List<AntAgent>();
    private float bestFitnessEver = 0f;
    
    void Start()
    {
        SpawnFirstGeneration();
    }
    
    void Update()
    {
        generationTimer += Time.deltaTime;
        
        if (generationTimer >= generationDuration)
        {
            EvolveNextGeneration();
        }
    }
    
    int FindSurfaceY(int x, int z)
    {
        // Start from top and work down to find first solid block
        for (int y = 50; y >= 0; y--)
        {
            AbstractBlock currentBlock = WorldManager.Instance.GetBlock(x, y, z);
            AbstractBlock blockBelow = WorldManager.Instance.GetBlock(x, y - 1, z);
            
            // Surface is where we have air above and solid below
            if (currentBlock is AirBlock && !(blockBelow is AirBlock))
            {
                return y;
            }
        }
        
        // Default to Y=10 if we can't find surface
        return 10;
    }
    
    void SpawnFirstGeneration()
    {
        currentGeneration = 1;
        Debug.Log("=== Starting Generation 1 ===");
        
        // Find surface at queen spawn location
        int queenY = FindSurfaceY(queenSpawnGridPos.x, queenSpawnGridPos.z);
        Vector3 queenSpawnPos = new Vector3(queenSpawnGridPos.x, queenY, queenSpawnGridPos.z);
        
        // Spawn queen
        GameObject queenObj = Instantiate(queenPrefab, queenSpawnPos, Quaternion.identity);
        QueenAnt queen = queenObj.GetComponent<QueenAnt>();
        
        // Random initial genes
        queen.explorationRate = Random.Range(0.1f, 0.2f);
        queen.diggingProbability = Random.Range(0.01f, 0.05f);
        queen.foodSeekingWeight = Random.Range(0.8f, 1.2f);
        
        currentPopulation.Add(queen);
        
        // Spawn workers in circle around queen
        for (int i = 0; i < populationSize - 1; i++)
        {
            float angle = (i / (float)(populationSize - 1)) * 2f * Mathf.PI;
            
            int workerX = Mathf.RoundToInt(queenSpawnGridPos.x + Mathf.Cos(angle) * workerSpawnRadius);
            int workerZ = Mathf.RoundToInt(queenSpawnGridPos.z + Mathf.Sin(angle) * workerSpawnRadius);
            int workerY = FindSurfaceY(workerX, workerZ);
            
            Vector3 spawnPos = new Vector3(workerX, workerY, workerZ);
            
            GameObject antObj = Instantiate(antPrefab, spawnPos, Quaternion.identity);
            AntAgent ant = antObj.GetComponent<AntAgent>();
            
            // Random initial genes
            ant.explorationRate = Random.Range(0.3f, 0.8f);
            ant.diggingProbability = Random.Range(0.05f, 0.3f);
            ant.foodSeekingWeight = Random.Range(0.5f, 1.5f);
            
            currentPopulation.Add(ant);
        }
    }
    
    void EvolveNextGeneration()
    {
        Debug.Log($"=== Generation {currentGeneration} Complete ===");
        
        // Calculate fitness for all ants
        List<AntFitness> fitnessScores = new List<AntFitness>();
        QueenAnt currentQueen = null;
        
        foreach (AntAgent ant in currentPopulation)
        {
            if (ant != null)
            {
                float fitness = 0f;
                
                if (ant is QueenAnt queen)
                {
                    currentQueen = queen;
                    // FITNESS CALCULATION FOR QUEEN:
                    // Primary goal is nest production (100 points per nest)
                    fitness = queen.nestsProduced * 100f;
                    
                    // Bonus for survival (encourages sustainable strategies)
                    if (queen.isAlive)
                    {
                        fitness += 50f;
                    }
                }
                else
                {
                    // FITNESS CALCULATION FOR WORKERS:
                    // Reward survival and remaining health
                    if (ant.isAlive)
                    {
                        fitness += ant.currentHealth;
                        fitness += 20f; // Survival bonus
                    }
                }
                
                fitnessScores.Add(new AntFitness
                {
                    ant = ant,
                    fitness = fitness,
                    explorationRate = ant.explorationRate,
                    diggingProbability = ant.diggingProbability,
                    foodSeekingWeight = ant.foodSeekingWeight
                });
            }
        }
        
        // Sort by fitness
        fitnessScores = fitnessScores.OrderByDescending(x => x.fitness).ToList();
        
        // Log statistics
        if (fitnessScores.Count > 0)
        {
            float bestFitness = fitnessScores[0].fitness;
            float avgFitness = fitnessScores.Average(x => x.fitness);
            int aliveCount = fitnessScores.Count(x => x.ant.isAlive);
            
            if (currentQueen != null)
            {
                Debug.Log($"Nests Produced: {currentQueen.nestsProduced}");
                Debug.Log($"Queen Status: {(currentQueen.isAlive ? "ALIVE" : "DEAD")} - Health: {currentQueen.currentHealth:F1}");
            }
            
            Debug.Log($"Best Fitness: {bestFitness:F1}");
            Debug.Log($"Avg Fitness: {avgFitness:F1}");
            Debug.Log($"Alive: {aliveCount}/{fitnessScores.Count}");
            
            if (bestFitness > bestFitnessEver)
            {
                bestFitnessEver = bestFitness;
                Debug.Log($"*** NEW RECORD: {bestFitnessEver:F1} ***");
            }
        }
        
        // Create next generation genes
        List<GeneSet> newGenes = new List<GeneSet>();
        
        // ELITISM: Preserve best genes without modification
        // Ensures good solutions aren't lost
        for (int i = 0; i < Mathf.Min(eliteCount, fitnessScores.Count); i++)
        {
            newGenes.Add(new GeneSet
            {
                explorationRate = fitnessScores[i].explorationRate,
                diggingProbability = fitnessScores[i].diggingProbability,
                foodSeekingWeight = fitnessScores[i].foodSeekingWeight
            });
        }
        
        // BREEDING: Create offspring from top 50% of population
        int breedingPoolSize = Mathf.Max(2, fitnessScores.Count / 2);
        
        while (newGenes.Count < populationSize)
        {
            // Select two parents from top performers
            int parent1Index = Random.Range(0, breedingPoolSize);
            int parent2Index = Random.Range(0, breedingPoolSize);
            
            AntFitness parent1 = fitnessScores[parent1Index];
            AntFitness parent2 = fitnessScores[parent2Index];
            
            // CROSSOVER: Randomly inherit each gene from either parent
            GeneSet childGenes = new GeneSet
            {
                explorationRate = Random.value < 0.5f ? parent1.explorationRate : parent2.explorationRate,
                diggingProbability = Random.value < 0.5f ? parent1.diggingProbability : parent2.diggingProbability,
                foodSeekingWeight = Random.value < 0.5f ? parent1.foodSeekingWeight : parent2.foodSeekingWeight
            };
            
            // MUTATION: Random changes to introduce variation
            if (Random.value < mutationChance)
            {
                childGenes.explorationRate += Random.Range(-mutationAmount, mutationAmount);
                childGenes.explorationRate = Mathf.Clamp(childGenes.explorationRate, 0.1f, 1f);
            }
            
            if (Random.value < mutationChance)
            {
                childGenes.diggingProbability += Random.Range(-mutationAmount, mutationAmount);
                childGenes.diggingProbability = Mathf.Clamp(childGenes.diggingProbability, 0f, 0.5f);
            }
            
            if (Random.value < mutationChance)
            {
                childGenes.foodSeekingWeight += Random.Range(-mutationAmount, mutationAmount);
                childGenes.foodSeekingWeight = Mathf.Clamp(childGenes.foodSeekingWeight, 0.3f, 2f);
            }
            
            newGenes.Add(childGenes);
        }
        
        // Destroy old population
        foreach (AntAgent ant in currentPopulation)
        {
            if (ant != null)
            {
                Destroy(ant.gameObject);
            }
        }
        currentPopulation.Clear();
        
        // Reset generation timer and increment generation
        currentGeneration++;
        generationTimer = 0f;
        
        Debug.Log($"=== Starting Generation {currentGeneration} ===");
        
        // Spawn new queen
        int newQueenY = FindSurfaceY(queenSpawnGridPos.x, queenSpawnGridPos.z);
        Vector3 newQueenPos = new Vector3(queenSpawnGridPos.x, newQueenY, queenSpawnGridPos.z);
        
        GameObject queenObj = Instantiate(queenPrefab, newQueenPos, Quaternion.identity);
        QueenAnt newQueen = queenObj.GetComponent<QueenAnt>();
        
        // Apply best genes to queen
        newQueen.explorationRate = newGenes[0].explorationRate;
        newQueen.diggingProbability = newGenes[0].diggingProbability;
        newQueen.foodSeekingWeight = newGenes[0].foodSeekingWeight;
        currentPopulation.Add(newQueen);
        
        // Spawn workers
        for (int i = 1; i < newGenes.Count; i++)
        {
            float angle = ((i - 1) / (float)(newGenes.Count - 1)) * 2f * Mathf.PI;
            
            int workerX = Mathf.RoundToInt(queenSpawnGridPos.x + Mathf.Cos(angle) * workerSpawnRadius);
            int workerZ = Mathf.RoundToInt(queenSpawnGridPos.z + Mathf.Sin(angle) * workerSpawnRadius);
            int workerY = FindSurfaceY(workerX, workerZ);
            
            Vector3 spawnPos = new Vector3(workerX, workerY, workerZ);
            
            GameObject antObj = Instantiate(antPrefab, spawnPos, Quaternion.identity);
            AntAgent ant = antObj.GetComponent<AntAgent>();
            
            // Apply evolved genes
            ant.explorationRate = newGenes[i].explorationRate;
            ant.diggingProbability = newGenes[i].diggingProbability;
            ant.foodSeekingWeight = newGenes[i].foodSeekingWeight;
            
            currentPopulation.Add(ant);
        }
    }
    
    // Helper class to store fitness and genes together
    class AntFitness
    {
        public AntAgent ant;
        public float fitness;
        public float explorationRate;
        public float diggingProbability;
        public float foodSeekingWeight;
    }
    
    // Helper class to store gene sets
    class GeneSet
    {
        public float explorationRate;
        public float diggingProbability;
        public float foodSeekingWeight;
    }
}