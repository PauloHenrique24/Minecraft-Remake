using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider))]
public class Chunk : MonoBehaviour
{
    public Vector2Int Coord { get; private set; }
    public int ChunkSize { get; private set; }
    public int WorldHeight { get; private set; }

    private BlockType[,,] blocks;

    private MeshFilter meshFilter;
    private MeshCollider meshCollider;
    private Mesh meshInstance;
    private WorldManager world;

    public void Initialize(WorldManager worldManager, Vector2Int coord, int chunkSize, int worldHeight)
    {
        world = worldManager;
        Coord = coord;
        ChunkSize = chunkSize;
        WorldHeight = worldHeight;

        meshFilter = GetComponent<MeshFilter>();
        meshCollider = GetComponent<MeshCollider>();

        GenerateBlocks();
    }

    void GenerateBlocks()
    {
        blocks = new BlockType[ChunkSize, WorldHeight, ChunkSize];

        for (int x = 0; x < ChunkSize; x++)
        {
            for (int z = 0; z < ChunkSize; z++)
            {
                int worldX = Coord.x * ChunkSize + x;
                int worldZ = Coord.y * ChunkSize + z;

                float noise = Mathf.PerlinNoise(
                    (worldX + world.SeedOffset.x) * world.NoiseScale,
                    (worldZ + world.SeedOffset.y) * world.NoiseScale
                );

                int height = Mathf.FloorToInt(noise * world.TerrainHeight) + world.GroundHeight;
                height = Mathf.Clamp(height, 1, WorldHeight - 1);

                for (int y = 0; y < WorldHeight; y++)
                {
                    if (y > height)
                    {
                        blocks[x, y, z] = BlockType.Air;
                    }
                    else if (y == height)
                    {
                        blocks[x, y, z] = BlockType.Grass;
                        if(Random.Range(0,100) < 1)
                        {
                            blocks[x, y, z] = BlockType.Sand;
                        }
                    }
                    else if (y >= height - 3)
                    {
                        blocks[x, y, z] = BlockType.Dirt;
                    }else if(y == 0)
                    {
                        blocks[x, y, z] = BlockType.bedrock;
                    }
                    else
                    {
                        if(Random.Range(0,100) < 6)
                        {
                            int m = Random.Range(0,4);

                            if(m == 0)
                                blocks[x, y, z] = BlockType.coal;
                            else if(m == 1)
                                blocks[x, y, z] = BlockType.iron;
                            else if(m == 2)
                                blocks[x, y, z] = BlockType.gold;
                            else
                                blocks[x, y, z] = BlockType.diamond;
                        }
                        else
                        {
                            blocks[x, y, z] = BlockType.Stone;
                        }

                        
                    }
                }
            }
        }

        GenerateTrees();
    }

    public void BuildMesh()
    {
        List<Vector3> vertices = new();
        List<int> triangles = new();
        List<Vector2> uvs = new();

        for (int x = 0; x < ChunkSize; x++)
        {
            for (int y = 0; y < WorldHeight; y++)
            {
                for (int z = 0; z < ChunkSize; z++)
                {
                    BlockType block = blocks[x, y, z];

                    if (block == BlockType.Air)
                        continue;

                    int worldX = Coord.x * ChunkSize + x;
                    int worldZ = Coord.y * ChunkSize + z;

                    Vector3 localPos = new Vector3(x, y, z);

                    if (!world.IsSolid(worldX, y, worldZ + 1))
                        AddFace(localPos, block, FaceDirection.Forward, vertices, triangles, uvs);

                    if (!world.IsSolid(worldX, y, worldZ - 1))
                        AddFace(localPos, block, FaceDirection.Back, vertices, triangles, uvs);

                    if (!world.IsSolid(worldX - 1, y, worldZ))
                        AddFace(localPos, block, FaceDirection.Left, vertices, triangles, uvs);

                    if (!world.IsSolid(worldX + 1, y, worldZ))
                        AddFace(localPos, block, FaceDirection.Right, vertices, triangles, uvs);

                    if (!world.IsSolid(worldX, y + 1, worldZ))
                        AddFace(localPos, block, FaceDirection.Up, vertices, triangles, uvs);

                    if (!world.IsSolid(worldX, y - 1, worldZ))
                        AddFace(localPos, block, FaceDirection.Down, vertices, triangles, uvs);
                }
            }
        }

        if (meshInstance != null)
            Destroy(meshInstance);

        meshInstance = new Mesh
        {
            indexFormat = IndexFormat.UInt32
        };

        meshInstance.SetVertices(vertices);
        meshInstance.SetTriangles(triangles, 0);
        meshInstance.SetUVs(0, uvs);
        meshInstance.RecalculateNormals();
        meshInstance.RecalculateBounds();

        meshFilter.sharedMesh = null;
        meshCollider.sharedMesh = null;

        meshFilter.sharedMesh = meshInstance;
        meshCollider.sharedMesh = meshInstance;
    }

    public BlockType GetBlockLocal(int x, int y, int z)
    {
        if (x < 0 || x >= ChunkSize || y < 0 || y >= WorldHeight || z < 0 || z >= ChunkSize)
            return BlockType.Air;

        return blocks[x, y, z];
    }

    void AddFace(
        Vector3 pos,
        BlockType block,
        FaceDirection face,
        List<Vector3> vertices,
        List<int> triangles,
        List<Vector2> uvs)
    {
        int index = vertices.Count;

        switch (face)
        {
            case FaceDirection.Forward:
                vertices.Add(pos + new Vector3(0, 0, 1));
                vertices.Add(pos + new Vector3(1, 0, 1));
                vertices.Add(pos + new Vector3(1, 1, 1));
                vertices.Add(pos + new Vector3(0, 1, 1));
                break;

            case FaceDirection.Back:
                vertices.Add(pos + new Vector3(1, 0, 0));
                vertices.Add(pos + new Vector3(0, 0, 0));
                vertices.Add(pos + new Vector3(0, 1, 0));
                vertices.Add(pos + new Vector3(1, 1, 0));
                break;

            case FaceDirection.Left:
                vertices.Add(pos + new Vector3(0, 0, 0));
                vertices.Add(pos + new Vector3(0, 0, 1));
                vertices.Add(pos + new Vector3(0, 1, 1));
                vertices.Add(pos + new Vector3(0, 1, 0));
                break;

            case FaceDirection.Right:
                vertices.Add(pos + new Vector3(1, 0, 1));
                vertices.Add(pos + new Vector3(1, 0, 0));
                vertices.Add(pos + new Vector3(1, 1, 0));
                vertices.Add(pos + new Vector3(1, 1, 1));
                break;

            case FaceDirection.Up:
                vertices.Add(pos + new Vector3(0, 1, 1));
                vertices.Add(pos + new Vector3(1, 1, 1));
                vertices.Add(pos + new Vector3(1, 1, 0));
                vertices.Add(pos + new Vector3(0, 1, 0));
                break;

            case FaceDirection.Down:
                vertices.Add(pos + new Vector3(0, 0, 0));
                vertices.Add(pos + new Vector3(1, 0, 0));
                vertices.Add(pos + new Vector3(1, 0, 1));
                vertices.Add(pos + new Vector3(0, 0, 1));
                break;
        }

        triangles.Add(index + 0);
        triangles.Add(index + 1);
        triangles.Add(index + 2);

        triangles.Add(index + 0);
        triangles.Add(index + 2);
        triangles.Add(index + 3);

        AddFaceUVs(block, face, uvs);
    }

    void AddFaceUVs(BlockType block, FaceDirection face, List<Vector2> uvs)
    {
        Vector2Int tile = BlockDatabase.GetTexture(block, face);

        float atlasSize = 512f;   // tamanho total da imagem
        float tileSize = 16f;     // tamanho de cada bloco
        float spacing = 1f;       // se tiver espaço entre tiles, muda aqui

        float px = tile.x * (tileSize + spacing);

        // IMPORTANTE: inverter o Y
        float py = atlasSize - tileSize - (tile.y * (tileSize + spacing));

        float uMin = px / atlasSize;
        float vMin = py / atlasSize;
        float uMax = (px + tileSize) / atlasSize;
        float vMax = (py + tileSize) / atlasSize;

        uvs.Add(new Vector2(uMin, vMin));
        uvs.Add(new Vector2(uMax, vMin));
        uvs.Add(new Vector2(uMax, vMax));
        uvs.Add(new Vector2(uMin, vMax));
    }

    public void SetBlock(int x, int y, int z, BlockType type)
    {
        if (x < 0 || x >= ChunkSize ||
            y < 0 || y >= WorldHeight ||
            z < 0 || z >= ChunkSize)
            return;

        blocks[x, y, z] = type;
        BuildMesh();
    }

    void GenerateTrees()
    {
        for (int x = 2; x < ChunkSize - 2; x++)
        {
            for (int z = 2; z < ChunkSize - 2; z++)
            {
                int surfaceY = GetSurfaceY(x, z);

                if (surfaceY <= 0 || surfaceY >= WorldHeight - 6)
                    continue;

                if (blocks[x, surfaceY, z] != BlockType.Grass)
                    continue;

                int worldX = Coord.x * ChunkSize + x;
                int worldZ = Coord.y * ChunkSize + z;

                float treeNoise = Mathf.PerlinNoise(
                    (worldX + world.SeedOffset.x) * 0.1f,
                    (worldZ + world.SeedOffset.y) * 0.1f
                );

                if (treeNoise > 0.72f)
                {
                    PlaceTree(x, surfaceY + 1, z);
                }
            }
        }
    }

    int GetSurfaceY(int x, int z)
    {
        for (int y = WorldHeight - 1; y >= 0; y--)
        {
            if (blocks[x, y, z] != BlockType.Air)
                return y;
        }

        return -1;
    }

    void PlaceTree(int x, int y, int z)
    {
        int trunkHeight = Random.Range(4, 7);
        int crownRadius = Random.Range(2, 4);

        // Tronco
        for (int i = 0; i < trunkHeight; i++)
        {
            if (IsInside(x, y + i, z))
                blocks[x, y + i, z] = BlockType.wood;
        }

        int topY = y + trunkHeight;

        // Copa em camadas
        for (int ly = -crownRadius; ly <= crownRadius; ly++)
        {
            int layerRadius = crownRadius - Mathf.Abs(ly) / 2;

            for (int lx = -layerRadius; lx <= layerRadius; lx++)
            {
                for (int lz = -layerRadius; lz <= layerRadius; lz++)
                {
                    int ax = x + lx;
                    int ay = topY + ly;
                    int az = z + lz;

                    if (!IsInside(ax, ay, az))
                        continue;

                    if (blocks[ax, ay, az] != BlockType.Air)
                        continue;

                    float dist = Mathf.Sqrt(lx * lx + lz * lz);

                    // deixa formato mais orgânico
                    float randomCut = Random.Range(0f, 1f);

                    if (dist <= layerRadius + 0.3f && randomCut > 0.15f)
                    {
                        // evita folhas nos cantos extremos às vezes
                        bool corner = Mathf.Abs(lx) == layerRadius && Mathf.Abs(lz) == layerRadius;

                        if (!corner || Random.value > 0.5f)
                            blocks[ax, ay, az] = BlockType.leaves;
                    }
                }
            }
        }

        // topo extra aleatório
        int extraTopLeaves = Random.Range(1, 4);

        for (int i = 0; i < extraTopLeaves; i++)
        {
            int rx = x + Random.Range(-1, 2);
            int rz = z + Random.Range(-1, 2);
            int ry = topY + crownRadius + Random.Range(0, 2);

            if (IsInside(rx, ry, rz) && blocks[rx, ry, rz] == BlockType.Air)
                blocks[rx, ry, rz] = BlockType.leaves;
        }
    }

    bool IsInside(int x, int y, int z)
    {
        return x >= 0 && x < ChunkSize &&
            y >= 0 && y < WorldHeight &&
            z >= 0 && z < ChunkSize;
    }
}