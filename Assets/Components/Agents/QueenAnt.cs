using UnityEngine;
using Antymology.Terrain;

namespace Antymology.Components.Agents
{
    public class QueenAnt : AntAgent
    {
        public int nestsProduced = 0;
        
        [Header("Queen Settings")]
        public float nestProductionInterval = 3f;
        private float nestTimer = 0f;
        public float minHealthToProduceNest = 40f;
        
        protected override void Start()
        {
            base.Start();
            
            // Make queen distinct (REQUIRED by assignment)
            transform.localScale = Vector3.one * 1.5f;
            GetComponent<Renderer>().material.color = Color.magenta;
            
            // Queen doesn't explore as much, stays near nest
            explorationRate = 0.2f;
            diggingProbability = 0.05f;
        }

        protected override void Update()
        {
            base.Update();
            
            if (!isAlive) return;
            
            // Produce nest periodically if healthy enough
            nestTimer += Time.deltaTime;
            
            if (nestTimer >= nestProductionInterval && currentHealth > minHealthToProduceNest)
            {
                nestTimer = 0f;
                ProduceNest();
            }
        }

        void ProduceNest()
        {
            // Cost: 1/3 max health (REQUIRED by assignment)
            float cost = maxHealth / 3f;
            
            if (currentHealth < cost)
            {
                return;
            }
            
            currentHealth -= cost;
            
            // Place nest block at current position
            int x = currentGridPosition.x;
            int y = currentGridPosition.y;
            int z = currentGridPosition.z;
            
            // Check if we can place a nest here
            AbstractBlock currentBlock = WorldManager.Instance.GetBlock(x, y, z);
            
            // Only place nest in air or on top of existing blocks
            if (currentBlock is AirBlock || currentBlock is NestBlock)
            {
                WorldManager.Instance.SetBlock(x, y, z, new NestBlock());
                
                // Queen climbs on top of her new nest
                currentGridPosition.y += 1;
                transform.position = currentGridPosition; // Snap immediately
                
                nestsProduced++;
            }
            else
            {
                // If we can't place nest at current position, try below
                AbstractBlock blockBelow = WorldManager.Instance.GetBlock(x, y - 1, z);
                
                if (blockBelow is AirBlock)
                {
                    WorldManager.Instance.SetBlock(x, y - 1, z, new NestBlock());
                    nestsProduced++;
                }
                else
                {
                    // Couldn't place nest, refund the health
                    currentHealth += cost;
                }
            }
        }
    }
}