using UnityEngine;
using Antymology.Terrain;
public class EvolutionManager : MonoBehaviour
{
    public int generation = 0;
    public float generationTime = 120f; // Time in seconds for each generation
    private float timer = 0f;
    public AntGenome bestGenome;

    private AntGenome currentGenome;
    public int bestFitness = 0;
    void Start()
    {
        bestGenome = new AntGenome(); // Start with a random genome
        currentGenome = bestGenome;
        Debug.Log("Generation 0 Started");
        LogGenome(bestGenome);
    }

    void Update()
    {
        timer += Time.deltaTime;
        //if times up, end generation and start a new one
        if (timer >= generationTime)
        {
            EndGeneration();
        }
    }

    void EndGeneration()
    {
        int fitness = WorldManager.Instance.nestCount; // Fitness based on number of nest blocks
        Debug.Log("Generation " + generation + " Ended. Fitness: " + fitness);
        //Update best genome if this generation is better, otherwise mutate the best genome for the next generation
        if (fitness > bestFitness)
        {
            bestFitness = fitness;
            bestGenome = new AntGenome(currentGenome); // Copy current genome as the new best
            Debug.Log("New Best Genome Found with Fitness: " + bestFitness);
            LogGenome(bestGenome);
        }
        generation++;
        timer = 0f;
        RestartSimulation();
    }

    void RestartSimulation()
    {
        // Destroy all ants
        Ant[] ants = FindObjectsOfType<Ant>();
        foreach (Ant ant in ants)
        {
            Destroy(ant.gameObject);
        }
        
        // Clear all nest blocks from the world
        WorldManager wm = WorldManager.Instance;
        ConfigurationManager config = ConfigurationManager.Instance;
        
        for (int x = 0; x < config.World_Diameter * config.Chunk_Diameter; x++)
            for (int y = 0; y < config.World_Height * config.Chunk_Diameter; y++)
                for (int z = 0; z < config.World_Diameter * config.Chunk_Diameter; z++)
                {
                    AbstractBlock block = wm.GetBlock(x, y, z);
                    
                    // Remove nest blocks
                    if (block is NestBlock)
                    {
                        wm.SetBlock(x, y, z, new AirBlock());
                    }
                    
                    // Reset pheromones in all air blocks
                    if (block is AirBlock air)
                    {
                        air.foodPheromone = 0f;
                        air.nestPheromone = 0f;
                        air.dangerPheromone = 0f;
                    }
                }
        // Reset nest count
        wm.nestCount = 0;
        
        currentGenome = new AntGenome(bestGenome, true); // Start next generation with the best genome from the previous generation
        // Spawn new generation
        SpawnAntsWithGenome(currentGenome);
        
        Debug.Log("=== Generation " + generation + " Started ===");
    }
    
    void SpawnAntsWithGenome(AntGenome genome)
    {
        Debug.Log("SpawnAntsWithGenome called!");
        WorldManager wm = WorldManager.Instance;
        ConfigurationManager config = ConfigurationManager.Instance;
        System.Random RNG = new System.Random();
        
        // Spawn Queen
        int qx = RNG.Next(10, config.World_Diameter * config.Chunk_Diameter - 10);
        int qz = RNG.Next(10, config.World_Diameter * config.Chunk_Diameter - 10);
        int qy = 20; // Spawn high up
        
        GameObject queen = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        queen.transform.position = new Vector3(qx, qy, qz);
        queen.transform.localScale = Vector3.one * 1.0f;
        queen.GetComponent<Renderer>().material.color = Color.yellow;
        Ant queenAnt = queen.AddComponent<Ant>();
        queenAnt.isQueen = true;
        queenAnt.maxHealth = 100f;
        queenAnt.health = 100f;
        queenAnt.genome = new AntGenome(genome); // Slight variation
        queen.name = "Queen";
        
        wm.queen2 = queenAnt; // Set the queen reference in the world manager

        // Spawn 10 worker ants
        for (int i = 0; i < 20; i++)
        {
            int x = RNG.Next(20, config.World_Diameter * config.Chunk_Diameter - 10);
            int z = RNG.Next(20, config.World_Diameter * config.Chunk_Diameter - 10);
            
            GameObject ant = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            ant.transform.position = new Vector3(x, 20, z);
            ant.transform.localScale = Vector3.one * 0.5f;
            ant.GetComponent<Renderer>().material.color = Color.red;
            Ant antScript = ant.AddComponent<Ant>();
            antScript.genome = new AntGenome(genome); // All share similar genome
            ant.name = "Ant_" + i;
        }
    }
    
    void LogGenome(AntGenome g)
    {
        Debug.Log($"Genome: Food={g.foodPheromoneWeight:F2} Nest={g.nestPheromoneWeight:F2} Danger={g.dangerPheromoneWeight:F2} Switch={g.healthSwitchThreshold:F2}");
    }
}