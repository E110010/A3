using UnityEngine;
public class AntGenome
{
    //Pheromone strengths
    public float foodPheromoneWeight = 1f;
    public float nestPheromoneWeight = 1f;
    public float dangerPheromoneWeight = 1f;

    //Health thresholds
    public float healthSwitchThreshold = 0.6f; // Below this health, the ant prioritizes food pheromones
    public float foodDepositAmount = 50f;
    public float nestDepositAmount = 50f;

    //Default constructor creates a random genome
    public AntGenome()
    {
        Randomize();
    }


    //Copy genome
    public AntGenome(AntGenome parent)
    {
        foodPheromoneWeight = parent.foodPheromoneWeight;
        nestPheromoneWeight = parent.nestPheromoneWeight;
        dangerPheromoneWeight = parent.dangerPheromoneWeight;
        healthSwitchThreshold = parent.healthSwitchThreshold;
        foodDepositAmount = parent.foodDepositAmount;
        nestDepositAmount = parent.nestDepositAmount;
    }
    public AntGenome(AntGenome parent, bool mutate)
    {
        //Copy parent genome with mutation
        foodPheromoneWeight = Mutate(parent.foodPheromoneWeight, 0.1f, 5f);
        nestPheromoneWeight = Mutate(parent.nestPheromoneWeight, 0.1f, 5f);
        dangerPheromoneWeight = Mutate(parent.dangerPheromoneWeight, 0.1f, 5f);
        healthSwitchThreshold = Mutate(parent.healthSwitchThreshold, 0.1f, 1f);
        foodDepositAmount = Mutate(parent.foodDepositAmount, 10f, 100f);
        nestDepositAmount = Mutate(parent.nestDepositAmount, 10f, 100f);
    }

    //Randomize genome values
    void Randomize()
    {
        foodPheromoneWeight = Random.Range(0.1f, 5f);
        nestPheromoneWeight = Random.Range(0.1f, 5f);
        dangerPheromoneWeight = Random.Range(0.1f, 5f);
        healthSwitchThreshold = Random.Range(0.1f, 0.9f);
        foodDepositAmount = Random.Range(10f, 100f);
        nestDepositAmount = Random.Range(10f, 100f);
    }
    //Mutate a value with a random mutation within a specified range
    float Mutate(float value, float min, float max)
    {
        //random mutation
        float mutation = Random.Range(-0.2f, 0.2f) * (max - min);
        return Mathf.Clamp(value + mutation, min, max);
    }
}
