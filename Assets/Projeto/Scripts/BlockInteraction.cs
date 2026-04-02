using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class BlockInteraction : MonoBehaviour
{
    [SerializeField] private Camera cam;
    [SerializeField] private float reach = 6f;

    private WorldManager world;

    [SerializeField] private Image breakProgress;
    [SerializeField] private GameObject prefab_block;
    [SerializeField] private LayerMask layer_map;

    void Start()
    {
        world = FindFirstObjectByType<WorldManager>();
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
            BreakBlock();
        else if (Input.GetMouseButtonUp(0))
        {
            StopAllCoroutines();
            breakProgress.fillAmount = 0;
            breakProgress.gameObject.SetActive(false);
        }

        if (Input.GetMouseButtonDown(1))
            PlaceBlock();
    }

    void BreakBlock()
    {
        if (GetTargetBlock(out Vector3Int block, out Vector3 normal))
        {
            BlockType b = world.GetBlock(block.x, block.y, block.z);

            if(b == BlockType.bedrock)
            return;

            BlockProperties block_properties = null;
            foreach(var i in world.blocks)
            {
                if(i.type == b)
                {
                    block_properties = i;
                    break;
                }
            }
            
            StartCoroutine(DestroyBlock(block, block_properties));
        }
    }

    IEnumerator DestroyBlock(Vector3Int block, BlockProperties bp)
    {
        float timer = 0;

        breakProgress.fillAmount = 0;
        breakProgress.gameObject.SetActive(true);

        while (timer < bp.destroy_timer)
        {
            timer += Time.deltaTime;
            breakProgress.fillAmount = timer / bp.destroy_timer;
            yield return null;
        }

        breakProgress.fillAmount = 0;
        breakProgress.gameObject.SetActive(false);

        //Criar o item no chão
        if(bp.dropHand){ //Drop item quebrado com a mão
            var b = Instantiate(prefab_block,new Vector3(block.x + .5f,block.y + .5f, block.z + .5f),Quaternion.identity);
            b.GetComponent<MeshRenderer>().material = bp.mat_block;
        }

        world.SetBlock(block.x, block.y, block.z, BlockType.Air);
    }

    void PlaceBlock()
    {
        if (GetTargetBlock(out Vector3Int block, out Vector3 normal))
        {
            Vector3 placePos = block + Vector3Int.RoundToInt(normal);

            world.SetBlock(
                Mathf.FloorToInt(placePos.x),
                Mathf.FloorToInt(placePos.y),
                Mathf.FloorToInt(placePos.z),
                BlockType.TableWood
            );
        }
    }

    bool GetTargetBlock(out Vector3Int blockPos, out Vector3 normal)
    {
        Ray ray = cam.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out RaycastHit hit, reach,layer_map))
        {
            Vector3 pos = hit.point - hit.normal * 0.01f;

            blockPos = new Vector3Int(
                Mathf.FloorToInt(pos.x),
                Mathf.FloorToInt(pos.y),
                Mathf.FloorToInt(pos.z)
            );

            normal = hit.normal;

            return true;
        }

        blockPos = Vector3Int.zero;
        normal = Vector3.zero;
        return false;
    }
}