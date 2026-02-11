using UnityEngine;
using Antymology.Terrain;
using System.Collections;
using System.Collections.Generic;

namespace Antymology.Components.Agents
{
    public class AntAgent : MonoBehaviour
    {
        [Header("Stats")]
        public float maxHealth = 100f;
        public float currentHealth;
        public float healthDecayRate = 2f;
        public bool isAlive = true;
        
        [Header("Movement")]
        public float moveInterval = 0.5f;
        private float moveTimer;
        
        [Header("Evolutionary Genes")]
        // GENE 1: Controls random exploration behavior (0.1-1.0)
        // Higher values = more exploration, lower values = stay in place more
        public float explorationRate = 0.5f;
        
        // GENE 2: Probability of digging when near queen (0.0-0.5)
        // Helps clear terrain and create nest space
        public float diggingProbability = 0.1f;
        
        // GENE 3: Multiplier for hunger threshold (0.3-2.0)
        // Higher values = seeks food earlier, lower values = waits longer
        public float foodSeekingWeight = 1.0f;
        
        public Vector3Int currentGridPosition;
        public bool initialized = false;

        protected virtual void Start()
        {
            currentHealth = maxHealth;
            // Delay initialization by one frame to ensure WorldManager is ready
            StartCoroutine(DelayedInitialization());
        }

        private System.Collections.IEnumerator DelayedInitialization()
        {
            yield return null; // Wait one frame
            InitializePosition();
            initialized = true;
        }

        protected virtual void Update()
        {
            if (!isAlive) return;

            // 1. Health Decay
            float decay = healthDecayRate * Time.deltaTime;
            
            // Acidic Block Check doubles decay rate
            AbstractBlock blockUnder = WorldManager.Instance.GetBlock(
                currentGridPosition.x, currentGridPosition.y - 1, currentGridPosition.z);
            
            if (blockUnder is AcidicBlock) 
                decay *= 2f;
            
            currentHealth -= decay;
            if (currentHealth <= 0) 
                Die();

            // 2. Movement & Decisions
            moveTimer += Time.deltaTime;
            if (moveTimer >= moveInterval)
            {
                moveTimer = 0;
                MakeDecision();
            }
            
            // 3. Smooth movement toward grid position
            transform.position = Vector3.Lerp(transform.position, currentGridPosition, Time.deltaTime * 5f);
        }

        void InitializePosition()
        {
            // Calculate initial grid position from spawn position
            int spawnX = Mathf.RoundToInt(transform.position.x);
            int spawnZ = Mathf.RoundToInt(transform.position.z);
            int spawnY = Mathf.RoundToInt(transform.position.y);
            
            // Find the actual surface at this X, Z position
            int surfaceY = FindSurfaceY(spawnX, spawnZ, spawnY);
            
            if (surfaceY != -999)
            {
                currentGridPosition = new Vector3Int(spawnX, surfaceY, spawnZ);
            }
            else
            {
                // Fallback to spawn position
                currentGridPosition = new Vector3Int(spawnX, spawnY, spawnZ);
            }
            
            // Snap immediately to grid position
            transform.position = currentGridPosition;
        }

        void MakeDecision()
        {
            QueenAnt queen = FindObjectOfType<QueenAnt>();
            
            // PRIORITY 1: Help the queen survive
            // Queens produce nests, so keeping them alive is critical for fitness
            if (queen != null && queen.isAlive && queen.initialized)
            {
                float distance = Vector3.Distance(currentGridPosition, queen.currentGridPosition);
                
                // Donate health if we're healthy and queen needs it
                if (distance <= 0.5f && currentHealth > maxHealth * 0.7f && queen.currentHealth < queen.maxHealth * 0.8f)
                {
                    ShareHealthWithQueen(queen);
                    return;
                }
                
                // Move toward queen if she's far away and needs help
                if (currentHealth > maxHealth * 0.6f && distance > 3f && queen.currentHealth < queen.maxHealth * 0.7f)
                {
                    MoveTowardQueen(queen);
                    return;
                }
            }

            // PRIORITY 2: Self-preservation through eating
            // foodSeekingWeight gene affects when ants seek food
            float hungerThreshold = maxHealth * (0.6f * foodSeekingWeight); 
            if (currentHealth < hungerThreshold)
            {
                if (TryEatMulch()) return;
                if (SearchForMulch()) return;
            }

            // PRIORITY 3: Strategic digging near queen
            // Controlled by diggingProbability gene - creates space for nests
            if (queen != null && queen.initialized)
            {
                float queenDistance = Vector3.Distance(currentGridPosition, queen.currentGridPosition);
                if (queenDistance < 5f && Random.value < diggingProbability)
                {
                    if (TryDig()) return;
                }
            }

            // PRIORITY 4: Exploration
            // Controlled by explorationRate gene
            MoveRandomly();
        }

        void ShareHealthWithQueen(QueenAnt queen)
        {
            // Must be at same position
            if (currentGridPosition == queen.currentGridPosition)
            {
                float donation = maxHealth * 0.25f; // Give 25% of our max health
                
                if (currentHealth > donation)
                {
                    currentHealth -= donation;
                    queen.currentHealth = Mathf.Min(queen.currentHealth + donation, queen.maxHealth);
                }
            }
        }

        void MoveTowardQueen(QueenAnt queen)
        {
            Vector3Int queenPos = queen.currentGridPosition;
            
            // Move one step closer on X or Z axis (whichever is farther)
            int dx = 0, dz = 0;
            
            if (Mathf.Abs(queenPos.x - currentGridPosition.x) > Mathf.Abs(queenPos.z - currentGridPosition.z))
            {
                dx = queenPos.x > currentGridPosition.x ? 1 : -1;
            }
            else if (queenPos.z != currentGridPosition.z)
            {
                dz = queenPos.z > currentGridPosition.z ? 1 : -1;
            }
            else
            {
                return; // Already at same X/Z
            }
            
            Vector3Int target = new Vector3Int(
                currentGridPosition.x + dx,
                currentGridPosition.y,
                currentGridPosition.z + dz
            );

            int surfaceY = FindSurfaceY(target.x, target.z, target.y);
            
            // Only move if height difference is 2 or less
            if (surfaceY != -999 && Mathf.Abs(surfaceY - currentGridPosition.y) <= 2)
            {
                target.y = surfaceY;
                currentGridPosition = target;
            }
        }

        bool SearchForMulch()
        {
            // Look around for nearby mulch in a 3x3x3 area
            int searchRadius = 3;
            Vector3Int? closestMulch = null;
            float closestDistance = float.MaxValue;
            
            for (int dx = -searchRadius; dx <= searchRadius; dx++)
            {
                for (int dz = -searchRadius; dz <= searchRadius; dz++)
                {
                    if (dx == 0 && dz == 0) continue;
                    
                    int checkX = currentGridPosition.x + dx;
                    int checkZ = currentGridPosition.z + dz;
                    
                    for (int dy = -2; dy <= 2; dy++)
                    {
                        int checkY = currentGridPosition.y + dy;
                        AbstractBlock block = WorldManager.Instance.GetBlock(checkX, checkY, checkZ);
                        
                        if (block is MulchBlock)
                        {
                            float dist = Vector3.Distance(currentGridPosition, new Vector3Int(checkX, checkY, checkZ));
                            if (dist < closestDistance)
                            {
                                closestDistance = dist;
                                closestMulch = new Vector3Int(checkX, checkY + 1, checkZ); // Position on top of mulch
                            }
                        }
                    }
                }
            }
            
            // If we found mulch, move toward it
            if (closestMulch.HasValue)
            {
                Vector3Int target = closestMulch.Value;
                
                // Move one step closer
                int dx = 0, dz = 0;
                if (Mathf.Abs(target.x - currentGridPosition.x) > Mathf.Abs(target.z - currentGridPosition.z))
                {
                    dx = target.x > currentGridPosition.x ? 1 : -1;
                }
                else if (target.z != currentGridPosition.z)
                {
                    dz = target.z > currentGridPosition.z ? 1 : -1;
                }
                
                Vector3Int moveTarget = new Vector3Int(
                    currentGridPosition.x + dx,
                    currentGridPosition.y,
                    currentGridPosition.z + dz
                );

                int surfaceY = FindSurfaceY(moveTarget.x, moveTarget.z, currentGridPosition.y);
                
                if (surfaceY != -999 && Mathf.Abs(surfaceY - currentGridPosition.y) <= 2)
                {
                    moveTarget.y = surfaceY;
                    currentGridPosition = moveTarget;
                    return true;
                }
            }
            
            return false;
        }

        bool TryEatMulch()
        {
            int x = currentGridPosition.x;
            int y = currentGridPosition.y - 1;
            int z = currentGridPosition.z;

            AbstractBlock block = WorldManager.Instance.GetBlock(x, y, z);
            
            if (block is MulchBlock)
            {
                // Check if another ant is on the same mulch block 
                AntAgent[] allAnts = FindObjectsOfType<AntAgent>();
                foreach (AntAgent other in allAnts)
                {
                    if (other != this && other.isAlive && other.initialized)
                    {
                        Vector3Int otherPos = new Vector3Int(
                            other.currentGridPosition.x,
                            other.currentGridPosition.y - 1,
                            other.currentGridPosition.z
                        );
                        
                        if (otherPos.x == x && otherPos.y == y && otherPos.z == z)
                        {
                            return false; // Another ant is on this mulch
                        }
                    }
                }
                
                // No other ant on this mulch, consume it
                currentHealth = maxHealth;
                WorldManager.Instance.SetBlock(x, y, z, new AirBlock());
                // Fall down to where the mulch was
                currentGridPosition.y -= 1;
                return true;
            }
            return false;
        }

        bool TryDig()
        {
            int x = currentGridPosition.x;
            int y = currentGridPosition.y - 1;
            int z = currentGridPosition.z;

            AbstractBlock block = WorldManager.Instance.GetBlock(x, y, z);

            // Cannot dig certain block types
            if (block is ContainerBlock || block is AirBlock || block is AcidicBlock || block is NestBlock) 
                return false;

            WorldManager.Instance.SetBlock(x, y, z, new AirBlock());
            currentGridPosition.y -= 1;
            return true;
        }

        void MoveRandomly()
        {
            if (Random.value > explorationRate) return;

            int dx = Random.Range(-1, 2);
            int dz = Random.Range(-1, 2);
            
            if (dx == 0 && dz == 0) return;

            Vector3Int target = new Vector3Int(
                currentGridPosition.x + dx,
                currentGridPosition.y,
                currentGridPosition.z + dz
            );

            int surfaceY = FindSurfaceY(target.x, target.z, target.y);
            
            // Only move if height difference is 2 or less 
            if (surfaceY != -999 && Mathf.Abs(surfaceY - currentGridPosition.y) <= 2)
            {
                target.y = surfaceY;
                currentGridPosition = target;
            }
        }

        int FindSurfaceY(int x, int z, int startY)
        {
            for (int y = startY + 2; y >= startY - 2; y--)
            {
                AbstractBlock b = WorldManager.Instance.GetBlock(x, y, z);
                AbstractBlock below = WorldManager.Instance.GetBlock(x, y - 1, z);

                if (b is AirBlock && !(below is AirBlock))
                {
                    return y;
                }
            }
            return -999;
        }

        protected void Die()
        {
            isAlive = false;
            Renderer renderer = GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = Color.black;
            }
            transform.localScale *= 0.5f;
            this.enabled = false;
        }
    }
}