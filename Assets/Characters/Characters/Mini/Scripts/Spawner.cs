using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Spawner : MonoBehaviour
{
    [SerializeField] private GameObject characterPrefab;
    [SerializeField] private Mesh[] characterMeshes;

    private float spacing = 2f;
    private string[] anims = new string[] { "Base Layer.Walk", "Base Layer.Walk 1", "Base Layer.Walk 2", "Base Layer.Jump" };

    void Start()
    {
        var meshes = characterMeshes.ToList();

        meshes = meshes.OrderBy(i => System.Guid.NewGuid()).ToList();

        int count = characterMeshes.Length;
        int sqrt = Mathf.RoundToInt(Mathf.Sqrt(count));
        int x = 0;
        int z = 0;

        for (int i = 0; i  < count; i ++)
        {
            z++;
            if (z > sqrt)
            {
                z = 0;
                x++;
            }

            var go = Instantiate(characterPrefab, new Vector3((x - sqrt / 2) * spacing, 0, (z - sqrt / 2) * spacing), Quaternion.identity);
            var skinnedMeshRenderer = go.GetComponentInChildren<SkinnedMeshRenderer>();
            skinnedMeshRenderer.sharedMesh = meshes[i];
            var animator = go.GetComponent<Animator>();
            animator.Play(anims[Random.Range(0, anims.Length)], 0, Random.Range(0f, 1f));
        }
    }
}
