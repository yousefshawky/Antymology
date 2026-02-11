using UnityEngine;
using TMPro;
using Antymology.Components.Agents;

public class AntSimulationUI : MonoBehaviour
{
    public TextMeshProUGUI nestCountText;
    public TextMeshProUGUI generationText;
    public TextMeshProUGUI aliveAntsText;
    public TextMeshProUGUI queenHealthText;
    public TextMeshProUGUI timerText;
    
    private EvolutionManager evolutionManager;
    
    void Start()
    {
        evolutionManager = FindObjectOfType<EvolutionManager>();
    }
    
    void Update()
    {
        UpdateNestCount();
        UpdateGenerationInfo();
        UpdateAliveCount();
        UpdateQueenHealth();
        UpdateTimer();
    }
    
    void UpdateNestCount()
    {
        if (nestCountText != null)
        {
            QueenAnt queen = FindObjectOfType<QueenAnt>();
            if (queen != null)
            {
                nestCountText.text = $"Nests: {queen.nestsProduced}";
            }
            else
            {
                nestCountText.text = "Nests: 0";
            }
        }
    }
    
    void UpdateGenerationInfo()
    {
        if (generationText != null && evolutionManager != null)
        {
            generationText.text = $"Generation: {evolutionManager.currentGeneration}";
        }
    }
    
    void UpdateAliveCount()
    {
        if (aliveAntsText != null)
        {
            AntAgent[] allAnts = FindObjectsOfType<AntAgent>();
            int aliveCount = 0;
            int workerCount = 0;
            
            foreach (AntAgent ant in allAnts)
            {
                if (ant.isAlive)
                {
                    aliveCount++;
                    if (!(ant is QueenAnt))
                    {
                        workerCount++;
                    }
                }
            }
            
            aliveAntsText.text = $"Alive: {aliveCount} ({workerCount} workers)";
        }
    }
    
    void UpdateQueenHealth()
    {
        if (queenHealthText != null)
        {
            QueenAnt queen = FindObjectOfType<QueenAnt>();
            if (queen != null)
            {
                float healthPercent = (queen.currentHealth / queen.maxHealth) * 100f;
                string status = queen.isAlive ? "ALIVE" : "DEAD";
                queenHealthText.text = $"Queen: {status} - {queen.currentHealth:F0}/{queen.maxHealth:F0} ({healthPercent:F0}%)";
                
                // Color code based on health
                if (queen.isAlive)
                {
                    if (healthPercent > 66)
                        queenHealthText.color = Color.green;
                    else if (healthPercent > 33)
                        queenHealthText.color = Color.yellow;
                    else
                        queenHealthText.color = Color.red;
                }
                else
                {
                    queenHealthText.color = Color.black;
                }
            }
            else
            {
                queenHealthText.text = "Queen: NONE";
                queenHealthText.color = Color.gray;
            }
        }
    }
    
    void UpdateTimer()
    {
        if (timerText != null && evolutionManager != null)
        {
            float timeLeft = evolutionManager.generationDuration - evolutionManager.generationTimer;
            if (timeLeft < 0) timeLeft = 0;
            
            int minutes = Mathf.FloorToInt(timeLeft / 60f);
            int seconds = Mathf.FloorToInt(timeLeft % 60f);
            
            timerText.text = $"Next Gen: {minutes:00}:{seconds:00}";
        }
    }
}