using UnityEngine;

public static class BlockDatabase
{
    public static Vector2Int GetTexture(BlockType block, FaceDirection face)
    {
        switch (block)
        {
            case BlockType.Grass:
                if (face == FaceDirection.Up)
                    return new Vector2Int(0, 0); // grass top

                if (face == FaceDirection.Down)
                    return new Vector2Int(1, 0); // dirt

                return new Vector2Int(2, 0);     // grass side

            case BlockType.Dirt:
                return new Vector2Int(1, 0);

            case BlockType.Stone:
                return new Vector2Int(3, 0);

            case BlockType.Sand:
                return new Vector2Int(4, 0);

            case BlockType.wood:
                if (face == FaceDirection.Up || face == FaceDirection.Down)
                    return new Vector2Int(6, 0); // wood top

                return new Vector2Int(5, 0);

            case BlockType.leaves:
                return new Vector2Int(7,0);

            case BlockType.coal:
                return new Vector2Int(8,0);

            case BlockType.iron:
                return new Vector2Int(9,0);

            case BlockType.gold:
                return new Vector2Int(10,0);

            case BlockType.diamond:
                return new Vector2Int(11,0);

            case BlockType.bedrock:
                return new Vector2Int(12,0);

            case BlockType.TableWood:
                return new Vector2Int(13,0);
        }

        return Vector2Int.zero;
    }   
}

[System.Serializable]
public class BlockProperties
{
    public BlockType type;
    public float destroy_timer;

    [Space]
    public bool dropHand;
    public Material mat_block;
}