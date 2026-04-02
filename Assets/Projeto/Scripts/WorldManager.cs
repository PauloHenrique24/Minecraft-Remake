using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class WorldManager : MonoBehaviour
{
    [Header("Referências")]
    [SerializeField] private Transform player;
    [SerializeField] private Chunk chunkPrefab;

    [Header("Mundo")]
    [SerializeField] private int chunkSize = 16;
    [SerializeField] private int worldHeight = 64;
    [SerializeField] private int renderDistance = 3;

    [Header("Noise")]
    [SerializeField] private float noiseScale = 0.05f;
    [SerializeField] private int groundHeight = 20;
    [SerializeField] private int terrainHeight = 20;
    [SerializeField] private int seed = 12345;

    private readonly Dictionary<Vector2Int, Chunk> chunks = new();
    private Vector2Int currentPlayerChunk;
    private bool updatingChunks;

    public float NoiseScale => noiseScale;
    public int GroundHeight => groundHeight;
    public int TerrainHeight => terrainHeight;
    public Vector2 SeedOffset { get; private set; }

    [Header("Blocks Properties")]
    public List<BlockProperties> blocks = new();

    void Start()
    {
        Random.InitState(seed);
        SeedOffset = new Vector2(
            Random.Range(-100000f, 100000f),
            Random.Range(-100000f, 100000f)
        );

        currentPlayerChunk = GetPlayerChunkCoord();
        StartCoroutine(UpdateChunksRoutine());
    }

    void Update()
    {
        Vector2Int newPlayerChunk = GetPlayerChunkCoord();

        if (newPlayerChunk != currentPlayerChunk && !updatingChunks)
        {
            currentPlayerChunk = newPlayerChunk;
            StartCoroutine(UpdateChunksRoutine());
        }
    }

    Vector2Int GetPlayerChunkCoord()
    {
        return new Vector2Int(
            Mathf.FloorToInt(player.position.x / chunkSize),
            Mathf.FloorToInt(player.position.z / chunkSize)
        );
    }

    IEnumerator UpdateChunksRoutine()
    {
        updatingChunks = true;

        HashSet<Vector2Int> neededCoords = new();

        List<Vector2Int> coordsToCreate = new();

        for (int x = -renderDistance; x <= renderDistance; x++)
        {
            for (int z = -renderDistance; z <= renderDistance; z++)
            {
                Vector2Int coord = new Vector2Int(currentPlayerChunk.x + x, currentPlayerChunk.y + z);
                neededCoords.Add(coord);

                if (!chunks.ContainsKey(coord))
                    coordsToCreate.Add(coord);
            }
        }

        coordsToCreate = coordsToCreate
            .OrderBy(c => (c - currentPlayerChunk).sqrMagnitude)
            .ToList();

        foreach (Vector2Int coord in coordsToCreate)
        {
            CreateChunk(coord);
            yield return null;
        }

        foreach (Chunk chunk in chunks.Values)
        {
            if (neededCoords.Contains(chunk.Coord))
            {
                chunk.BuildMesh();
                yield return null;
            }
        }

        List<Vector2Int> toRemove = new();

        foreach (var pair in chunks)
        {
            if (!neededCoords.Contains(pair.Key))
                toRemove.Add(pair.Key);
        }

        foreach (Vector2Int coord in toRemove)
        {
            Destroy(chunks[coord].gameObject);
            chunks.Remove(coord);
            yield return null;
        }

        updatingChunks = false;
    }

    void CreateChunk(Vector2Int coord)
    {
        Chunk newChunk = Instantiate(
            chunkPrefab,
            new Vector3(coord.x * chunkSize, 0f, coord.y * chunkSize),
            Quaternion.identity,
            transform
        );

        newChunk.name = $"Chunk_{coord.x}_{coord.y}";
        newChunk.gameObject.layer = 6;
        newChunk.Initialize(this, coord, chunkSize, worldHeight);

        chunks.Add(coord, newChunk);
    }

    public bool IsSolid(int worldX, int worldY, int worldZ)
    {
        return GetBlock(worldX, worldY, worldZ) != BlockType.Air;
    }

    public BlockType GetBlock(int worldX, int worldY, int worldZ)
    {
        if (worldY < 0 || worldY >= worldHeight)
            return BlockType.Air;

        Vector2Int chunkCoord = new Vector2Int(
            Mathf.FloorToInt((float)worldX / chunkSize),
            Mathf.FloorToInt((float)worldZ / chunkSize)
        );

        if (!chunks.TryGetValue(chunkCoord, out Chunk chunk))
            return BlockType.Air;

        int localX = worldX - chunkCoord.x * chunkSize;
        int localZ = worldZ - chunkCoord.y * chunkSize;

        return chunk.GetBlockLocal(localX, worldY, localZ);
    }

    public void SetBlock(int worldX, int worldY, int worldZ, BlockType type)
    {
        Vector2Int chunkCoord = new Vector2Int(
            Mathf.FloorToInt((float)worldX / chunkSize),
            Mathf.FloorToInt((float)worldZ / chunkSize)
        );

        if (!chunks.TryGetValue(chunkCoord, out Chunk chunk))
            return;

        int localX = worldX - chunkCoord.x * chunkSize;
        int localZ = worldZ - chunkCoord.y * chunkSize;

        chunk.SetBlock(localX, worldY, localZ, type);

        // atualiza vizinhos se mexeu na borda
        if (localX == 0)
            RebuildChunk(chunkCoord + Vector2Int.left);

        if (localX == chunkSize - 1)
            RebuildChunk(chunkCoord + Vector2Int.right);

        if (localZ == 0)
            RebuildChunk(chunkCoord + new Vector2Int(0, -1));

        if (localZ == chunkSize - 1)
            RebuildChunk(chunkCoord + new Vector2Int(0, 1));
    }

    public void RebuildChunk(Vector2Int coord)
    {
        if (chunks.TryGetValue(coord, out Chunk chunk))
        {
            chunk.BuildMesh();
        }
    }
}