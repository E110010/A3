using UnityEngine;
using Antymology.Terrain;

public class Ant : MonoBehaviour
{

    //genome for evolution
    public AntGenome genome;
    private float actionCooldown = 0.5f; //action frequency in seconds
    private float actionTimer = 0f;

    //health
    public float health = 100f;
    //can't heal above max
    public float maxHealth = 100f;
    //rate they lose health
    public float healthDrainRate = 1f;
    //bool to designate queen
    public bool isQueen = false;
    
    private WorldManager worldManager;
    private ConfigurationManager config;
    
    void Start()
    {
        worldManager = WorldManager.Instance;
        config = ConfigurationManager.Instance;
        if (genome == null)
        {
            genome = new AntGenome();
        }
    }
    
    void Update()
    {
        actionTimer -= Time.deltaTime;
        if (actionTimer <= 0f)
        {
            actionTimer = actionCooldown; // reset timer
            //drain health over time
            float drainMultiplier = 3f;
            
            //Check if standing on acidic block
            AbstractBlock blockBelow = GetBlockBelow();
            if (blockBelow is AcidicBlock)
            {
                Vector3 pos = transform.position;
                int x = Mathf.RoundToInt(pos.x);
                int y = Mathf.RoundToInt(pos.y);
                int z = Mathf.RoundToInt(pos.z);
                AbstractBlock block = WorldManager.Instance.GetBlock(x,y,z);
                if (block is AirBlock air){
                    air.dangerPheromone += 1f; // Leave a pheromone trail to warn other ants
                    air.dangerPheromone = Mathf.Clamp(air.dangerPheromone, 0f, 100f); // Cap the pheromone level
                }
                //If yes, die faster
                drainMultiplier = 2f;
            }
            //adjust health
            health -= healthDrainRate * drainMultiplier * Time.deltaTime;
            
            //If health hits 0
            if (health <= 0)
            {
                //die
                Destroy(gameObject);
                return;
            }
            
            //If need healing (under half health)
            if (health < (maxHealth * genome.healthSwitchThreshold))
            {
                //attempt to heal
                TryEatMulch();
            }
            
            
                
            // Queen builds if health is high and moves to avoid getting stuck
            if (isQueen)
            {
                if (health > maxHealth * 0.45f){
                    BuildNest();
                    MoveRandomly();
                }
                else if (Random.value < 0.1f) //Occasionally move even if not building
                {
                    MoveRandomly();
                }
            }

            // Sometimes dig randomly
            // if (Random.value < 0.01f)
            // {
            //     TryDig();
            // }

            // Try to share health with nearby ants
            TryShareHealth();

            // Currently move randomly once in a while
            if (!isQueen){
                MoveUsingPheromones();
                if (Random.value < 0.02f) // 2% dig (for fun?)
                {
                    TryDig();
                }
            }
            // Emit pheromones if queen to attract workers
            if(isQueen){
                emitNestPheromones();
            }
        }
    }
    
    void MoveRandomly()
    {
        //use rng to pick a direction
        int dir = Random.Range(0, 4);
        Vector3 targetPos = transform.position;
        
        if (dir == 0) targetPos.x += 1;
        else if (dir == 1) targetPos.x -= 1;
        else if (dir == 2) targetPos.z += 1;
        else targetPos.z -= 1;
        
        // Check height difference to ensure legit move
        int currentX = Mathf.RoundToInt(transform.position.x);
        int currentZ = Mathf.RoundToInt(transform.position.z);
        int targetX = Mathf.RoundToInt(targetPos.x);
        int targetZ = Mathf.RoundToInt(targetPos.z);
        
        int currentHeight = GetGroundHeight(currentX, currentZ);
        int targetHeight = GetGroundHeight(targetX, targetZ);
        
        // if legit move
        if (Mathf.Abs(targetHeight - currentHeight) <= 2)
        {
            //move
            targetPos.y = targetHeight + 1;
            transform.position = targetPos;
        }
    }
    
    void TryEatMulch()
    {
        if(isQueen) return; // Queens don't eat mulch, otherwise she can just eat and build by herself
        int x = Mathf.RoundToInt(transform.position.x);
        int y = Mathf.RoundToInt(transform.position.y) - 1;
        int z = Mathf.RoundToInt(transform.position.z);
        if (y < 0) return; // Out of bounds check
        
        AbstractBlock block = worldManager.GetBlock(x, y, z);
        
        if (block is MulchBlock)
        {
            // Check if another ant is on this same block
            Collider[] nearbyColliders = Physics.OverlapSphere(new Vector3(x, y + 1, z), 0.5f);
            int antCount = 0;
            
            foreach (Collider col in nearbyColliders)
            {
                if (col.GetComponent<Ant>() != null)
                    antCount++;
            }
            
            // Only eat if we're the only ant here
            if (antCount == 1)
            {
                health = maxHealth;
                AirBlock air = new AirBlock();
                air.foodPheromone = genome.foodDepositAmount; // Leave a strong pheromone trail
                worldManager.SetBlock(x, y, z, air); // Replace mulch with air
                Debug.Log("Ant ate mulch!");
            }
        }
    }
    
    void BuildNest()
    {
        //Check to ensure we have enough health
        if (health < maxHealth / 3f) return;
        
        int x = Mathf.RoundToInt(transform.position.x);
        int y = Mathf.RoundToInt(transform.position.y);
        int z = Mathf.RoundToInt(transform.position.z);
        
        if (y < 0 || y >= (config.World_Height * config.Chunk_Diameter)) return;
        if (x < 0 || x >= config.World_Diameter * config.Chunk_Diameter) return;
        if (z < 0 || z >= config.World_Diameter * config.Chunk_Diameter) return;
        
        // Pick a random adjacent horizontal direction
        int dir = Random.Range(0, 4);
        int buildX = x;
        int buildZ = z;
        
        if (dir == 0) buildX += 1;
        else if (dir == 1) buildX -= 1;
        else if (dir == 2) buildZ += 1;
        else buildZ -= 1;
        
        // Build in targer direction
        AbstractBlock adjacentBlock = worldManager.GetBlock(buildX, y, buildZ);
        //Don't replace blocks
        if (adjacentBlock is AirBlock)
        {
            worldManager.SetBlock(buildX, y, buildZ, new NestBlock());
            //Update counter and health
            worldManager.nestCount++;
            health -= maxHealth / 3f;
            Debug.Log("Queen built a nest block! Health now: " + health);
            
        }
    }
    
    AbstractBlock GetBlockBelow()
    {
        // Get the block directly below the ant
        int x = Mathf.RoundToInt(transform.position.x);
        int y = Mathf.RoundToInt(transform.position.y) - 1;
        int z = Mathf.RoundToInt(transform.position.z);
        
        return worldManager.GetBlock(x, y, z);
    }
    
    int GetGroundHeight(int x, int z)
    {
        // Find the highest non-air block at this x,z column to determine the ground height
        for (int y = config.World_Height * config.Chunk_Diameter - 1; y >= 0; y--)
        {
            AbstractBlock block = worldManager.GetBlock(x, y, z);
            if (!(block is AirBlock))
            {
                return y;
            }
        }
        return 0;
    }

        void TryDig()
    {
        int x = Mathf.RoundToInt(transform.position.x);
        int y = Mathf.RoundToInt(transform.position.y) - 1; // Block below
        int z = Mathf.RoundToInt(transform.position.z);
        if (y < 0) return; // Out of bounds check
        
        AbstractBlock block = worldManager.GetBlock(x, y, z);
        
        // Can't dig container blocks or air, don't remove nest progress
        if (block is ContainerBlock || block is AirBlock || block is NestBlock) return;
        
        // Dig it up
        worldManager.SetBlock(x, y, z, new AirBlock());
        transform.position = new Vector3(x, y + 1, z); // Move down to where block was
        Debug.Log("Ant dug a block!");
    }

    void TryShareHealth()
    {
        if (isQueen) return; // Queens don't share health
        // Find other ants at the same position
        Collider[] nearbyColliders = Physics.OverlapSphere(transform.position, 0.9f);
        
        foreach (Collider col in nearbyColliders)
        {
            Ant otherAnt = col.GetComponent<Ant>();
            
            // If theres another ant that need health
            if (otherAnt != null && otherAnt != this && health > maxHealth * genome.healthSwitchThreshold && otherAnt.health < otherAnt.maxHealth * 0.4f)
            {
                // Transfer health
                float transferAmount = 30f;
                health -= transferAmount;
                otherAnt.health += transferAmount;
                
                Debug.Log("Ant shared health!");
                break; // Only share with one ant per check
            }
        }
    }

    float EvaluateCell(int x, int z)
    {
        // Get height difference to ensure cell is walkable
        int targetHeight = GetGroundHeight(x, z);
        int currentHeight = GetGroundHeight(
            Mathf.RoundToInt(transform.position.x),
            Mathf.RoundToInt(transform.position.z)
        );

        int heightDiff = targetHeight - currentHeight;

        // Too steep = unwalkable
        if (Mathf.Abs(heightDiff) > 2)
            return float.NegativeInfinity;

        AbstractBlock block = worldManager.GetBlock(x, targetHeight + 1, z);
        //Can't walk into non-air blocks
        if (block is not AirBlock air)
            return float.NegativeInfinity;

        float score = 0f;
        //Score according to pheromone concentration and weights
        //If healthy, prioritize nest pheromones, if low health prioritize food pheromones
        if (health > maxHealth * genome.healthSwitchThreshold)
            score += air.nestPheromone * genome.nestPheromoneWeight;
        else
            score += air.foodPheromone * genome.foodPheromoneWeight;
        //Always avoid danger
        score -= air.dangerPheromone * genome.dangerPheromoneWeight;

        return score;
    }

    
    void MoveUsingPheromones()
    {
        //Vector of possible movement directions (8 surrounding cells)
        Vector3[] directions = {
            new Vector3(1, 0, 0), new Vector3(-1, 0, 0),
            new Vector3(0, 0, 1), new Vector3(0, 0, -1),
            new Vector3(1, 0, 1), new Vector3(1, 0, -1),
            new Vector3(-1, 0, 1), new Vector3(-1, 0, -1),
        };

        // Evaluate each direction and move towards the one with the highest score
        Vector3 bestDirection = Vector3.zero;
        float bestScore = float.NegativeInfinity;

        int cx = Mathf.RoundToInt(transform.position.x);
        int cz = Mathf.RoundToInt(transform.position.z);
        // Loop through each direction and evaluate the cell
        foreach (Vector3 dir in directions)
        {
            int nx = cx + Mathf.RoundToInt(dir.x);
            int nz = cz + Mathf.RoundToInt(dir.z);

            if (nx < 0 || nx >= config.World_Diameter * config.Chunk_Diameter ||
                nz < 0 || nz >= config.World_Diameter * config.Chunk_Diameter)
                continue;

            float score = EvaluateCell(nx, nz);
            // If this cell is better than our best option so far, remember it
            if (score > bestScore)
            {
                bestScore = score;
                bestDirection = dir;
            }
        }
        // If we found a not bad direction, move there
        if (bestScore > float.NegativeInfinity)
        {
            int tx = cx + Mathf.RoundToInt(bestDirection.x);
            int tz = cz + Mathf.RoundToInt(bestDirection.z);
            int ty = GetGroundHeight(tx, tz) + 1;

            transform.position = new Vector3(tx, ty, tz);
        }
    }


    void emitNestPheromones()
    {
        //Get current block and emit pheromones
        int x = Mathf.RoundToInt(transform.position.x);
        int y = Mathf.RoundToInt(transform.position.y);
        int z = Mathf.RoundToInt(transform.position.z);
        AbstractBlock block = worldManager.GetBlock(x, y, z);
        if (block is AirBlock air)
        {
            air.nestPheromone += genome.nestDepositAmount; // Emit nest pheromones to attract other ants
            air.nestPheromone = Mathf.Clamp(air.nestPheromone, 0f, 100f); // Cap the pheromone level
        }
    }
}