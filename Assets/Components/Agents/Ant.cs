using UnityEngine;
using Antymology.Terrain;

public class Ant : MonoBehaviour
{
    //health
    public float health = 100f;
    //can't heal above max
    public float maxHealth = 100f;
    //rate they lose health
    public float healthDrainRate = 1f;
    //bool to designate queen
    public bool isQueen = false;
    
    private WorldManager worldManager;
    
    void Start()
    {
        worldManager = WorldManager.Instance;
    }
    
    void Update()
    {
        //drain health over time
        float drainMultiplier = 1f;
        
        //Check if standing on acidic block
        AbstractBlock blockBelow = GetBlockBelow();
        if (blockBelow is AcidicBlock)
        {
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
        if (health < (maxHealth * 0.5f))
        {
            //attempt to heal
            TryEatMulch();
        }
        
        // Currently move randomly once in a while
        if (!isQueen){
            MoveRandomly();
        }

            
        // Queen builds randomly and if health is high
        if (isQueen && (health > maxHealth * 0.4f))
        {
            BuildNest();
        }

        // Sometimes dig randomly
        if (Random.value < 0.01f)
        {
            TryDig();
        }

        // Try to share health with nearby ants
        TryShareHealth();
    }
    
    void MoveRandomly()
    {
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
        int x = Mathf.RoundToInt(transform.position.x);
        int y = Mathf.RoundToInt(transform.position.y) - 1;
        int z = Mathf.RoundToInt(transform.position.z);
        if (y < 0) return; // Out of bounds check
        
        AbstractBlock block = worldManager.GetBlock(x, y, z);
        
        if (block is MulchBlock)
        {
            // Check if another ant is on this same block
            Collider[] nearbyColliders = Physics.OverlapSphere(new Vector3(x, y + 1, z), 0.3f);
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
                worldManager.SetBlock(x, y, z, new AirBlock());
                Debug.Log("Ant ate mulch!");
            }
        }
    }
    
void BuildNest()
{
    if (health < maxHealth / 3f)
    {
        return;
    }
    
    int x = Mathf.RoundToInt(transform.position.x);
    int y = Mathf.RoundToInt(transform.position.y);
    int z = Mathf.RoundToInt(transform.position.z);
    if (y < 0  || y >= worldManager.WorldHeight) return; // Out of bounds check
    if (x < 0  || x >= worldManager.WorldWidth) return; // Out of bounds check
    if (z < 0  || z >= worldManager.WorldDepth) return; // Out of bounds check
    
    // Pick a random adjacent horizontal direction
    int dir = Random.Range(0, 4);
    int buildX = x;
    int buildZ = z;
    
    if (dir == 0) buildX += 1;
    else if (dir == 1) buildX -= 1;
    else if (dir == 2) buildZ += 1;
    else buildZ -= 1;
    
    //Try to build at the same height level
    AbstractBlock adjacentBlock = worldManager.GetBlock(buildX, y, buildZ);
    
    // Only build if that spot is air
    if (adjacentBlock is AirBlock)
    {
        worldManager.SetBlock(buildX, y, buildZ, new NestBlock());
        worldManager.nestCount++;
        health -= maxHealth / 3f;
        Debug.Log("Queen built a nest block! Health now: " + health);
        //Move after building so we dont get stuck in a box
        MoveRandomly();
    }
}
    
    AbstractBlock GetBlockBelow()
    {
        int x = Mathf.RoundToInt(transform.position.x);
        int y = Mathf.RoundToInt(transform.position.y) - 1;
        int z = Mathf.RoundToInt(transform.position.z);
        
        return worldManager.GetBlock(x, y, z);
    }
    
    int GetGroundHeight(int x, int z)
    {
        // Find the highest non-air block
        for (int y = worldManager.WorldHeight - 1; y >= 0; y--)
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
        
        // Can't dig container blocks or air
        if (block is ContainerBlock || block is AirBlock) return;
        
        // Dig it up
        worldManager.SetBlock(x, y, z, new AirBlock());
        transform.position = new Vector3(x, y + 1, z); // Move down to where block was
        Debug.Log("Ant dug a block!");
    }

    void TryShareHealth()
    {
        // Find other ants at the same position
        Collider[] nearbyColliders = Physics.OverlapSphere(transform.position, 0.5f);
        
        foreach (Collider col in nearbyColliders)
        {
            Ant otherAnt = col.GetComponent<Ant>();
            
            // If theres another ant that need health
            if (otherAnt != null && otherAnt != this && health > maxHealth * 0.7f && otherAnt.health < otherAnt.maxHealth * 0.4f)
            {
                // Transfer 10 health
                float transferAmount = 30f;
                health -= transferAmount;
                otherAnt.health += transferAmount;
                
                Debug.Log("Ant shared health!");
                break; // Only share with one ant per action
            }
        }
    }
}

