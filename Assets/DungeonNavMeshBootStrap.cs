using UnityEngine;
using Unity.AI.Navigation; // for NavMeshSurface
using Dungeon_Generator;  // to see DungeonGenerator

public class DungeonNavmeshBootstrap : MonoBehaviour
{
    public DungeonGenerator dungeonGenerator;
    public NavMeshSurface navMeshSurface;

    private void Start()
    {
        // DungeonGenerator already calls GenerateDungeon() in its own Start().
        // Just build the NavMesh a bit later.
        if (navMeshSurface != null)
        {
            // build at end of frame so dungeon has time to spawn tiles & combine mesh
            StartCoroutine(BuildNavMeshNextFrame());
        }
    }

    private System.Collections.IEnumerator BuildNavMeshNextFrame()
    {
        yield return null; // wait 1 frame
        navMeshSurface.BuildNavMesh();
    }
}