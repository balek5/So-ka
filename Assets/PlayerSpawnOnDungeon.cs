using UnityEngine;
using Dungeon_Generator;  // this matches namespace in DungeonGenerator.cs

public class PlayerSpawnOnDungeon : MonoBehaviour
{
    public DungeonGenerator dungeon;
    public Transform player;

    void OnEnable()
    {
        if (dungeon != null)
            dungeon.DungeonGenerated += OnDungeonGenerated;
    }

    void OnDisable()
    {
        if (dungeon != null)
            dungeon.DungeonGenerated -= OnDungeonGenerated;
    }

    void Start()
    {
        // Start a fallback coroutine to ensure we spawn even if the event was missed
        StartCoroutine(EnsureSpawned());
    }

    private System.Collections.IEnumerator EnsureSpawned()
    {
        // wait until references are set
        while (dungeon == null || player == null)
            yield return null;

        // wait until a valid spawn position is available
        while (dungeon.playerSpawnPosition == Vector3.zero)
            yield return null;

        player.position = dungeon.playerSpawnPosition;
    }

    private void OnDungeonGenerated()
    {
        if (dungeon == null || player == null) return;
        player.position = dungeon.playerSpawnPosition;
    }
}