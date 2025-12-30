using UnityEngine;
using System.Collections.Generic;
using System;

namespace Dungeon_Generator
{
    // Add missing enum used throughout this file.
    public enum RoomType
    {
        Normal,
        Boss,
        Shop,
        Shrine
    }

    // Marker component to identify roof instances without requiring a Unity Tag.
    internal sealed class DungeonRoofMarker : MonoBehaviour { }

    [DefaultExecutionOrder(-100)]
    public class DungeonGenerator : MonoBehaviour
    {
        [Header("Dungeon Settings")]
        public int numRooms = 10;
        public int minRoomSize = 4;
        public int maxRoomSize = 10;
        public int dungeonWidth = 50;
        public int dungeonHeight = 50;

        [Header("Corridor Settings")]
        [Tooltip("Width of corridors in tiles (>=1)." )]
        public int corridorWidth = 1;

        [Header("Prefabs")]
        public GameObject wallPrefab;
        public GameObject floorPrefab;
        public GameObject cornerPrefab;
        public GameObject upgradeStatuePrefab; // prefab for upgrade statues in shrine rooms

        // Add the missing prefab references used by SpawnSpecialRoomPrefabs()
        [Tooltip("Optional prefab to place in boss rooms (e.g., boss portal/marker).")]
        public GameObject bossRoomPrefab;

        [Tooltip("Optional prefab to place in shop rooms (e.g., shopkeeper/marker).")]
        public GameObject shopRoomPrefab;

        public event Action DungeonGenerated;
        [Header("Special Room Highlights")]
        [Tooltip("Optional material to highlight boss rooms.")]
        public Material bossRoomMaterial;
        [Tooltip("Optional material to highlight shop rooms.")]
        public Material shopRoomMaterial;
        [Tooltip("Optional material to highlight shrine rooms.")]
        public Material shrineRoomMaterial;

        [Header("Optimization")]
        [Tooltip("Combine all generated tiles into one big mesh after generation to reduce draw calls.")]
        public bool combineIntoSingleMesh = true;

        [Tooltip("Add a MeshCollider to the combined dungeon mesh.")]
        public bool addMeshCollider = true;

        [Header("Optimization - Chunking")]
        [Tooltip("If > 0, the dungeon (non-roof) mesh will be combined into collider chunks of this tile size (recommended for stable collisions). Set 0 to combine into a single mesh.")]
        public int colliderChunkSize = 16;

        [Header("Spawning (logical positions only)")]
        [Tooltip("Logical spawn position for the player (center of the start room). You can use this from another script to place the player.")]
        public Vector3 playerSpawnPosition;

        [Tooltip("Logical spawn positions for enemies on valid floor tiles. Used by WaveSpawner or other systems.")]
        public List<Vector3> enemySpawnPositions = new List<Vector3>();

        [Header("Walls (height)")]
        [Tooltip("How many wall segments to stack vertically. Set this to match your roof/ceiling height in tiles.")]
        [Min(1)]
        public int wallHeightInTiles = 2;

        [Header("Boss Room Settings")]
        [Tooltip("Minimum size (tiles) for the boss room.")]
        public int bossMinRoomSize = 10;

        [Tooltip("Maximum size (tiles) for the boss room.")]
        public int bossMaxRoomSize = 16;

        [Header("Roof / Ceiling")]
        [Tooltip("Generate a roof/ceiling over every floor tile.")]
        public bool generateRoof = true;

        [Tooltip("Prefab used for roof tiles. If null, floorPrefab is reused.")]
        public GameObject roofPrefab;

        [Tooltip("If false, any colliders on roof tiles will be disabled so AI/spawns cannot use the roof as ground.")]
        public bool roofHasCollision = false;

        // New: optional mask to resolve spawn heights against the real floor (and ignore the roof)
        [Header("Spawning (height resolution)")]
        [Tooltip("If set, spawn positions will be raycast down against this mask to find the true floor height. Useful when the roof has colliders.")]
        public LayerMask spawnGroundMask = ~0;

        [Tooltip("Extra Y offset added after resolving the floor height.")]
        public float spawnHeightOffset = 1.15f;

        // New: cache original prefab bounds so we can scale to an exact 1x1 tile
        private Vector3 floorScaleFactor = Vector3.one;
        private Vector3 wallScaleFactor = Vector3.one;
        private Vector3 cornerScaleFactor = Vector3.one;

        private int[,] grid; // 0 = empty, 1 = floor, 2 = wall
        private List<Room> rooms = new List<Room>();

        [HideInInspector]
        public Transform playerSpawnPoint; // where the player should start

        [HideInInspector]
        public List<Transform> enemySpawnPoints = new List<Transform>();

        void Awake()
        {
            // Compute scale factors once at startup based on meshes / renderers
            floorScaleFactor = CalculateTileScale(floorPrefab);
            wallScaleFactor = CalculateTileScale(wallPrefab);
            cornerScaleFactor = CalculateTileScale(cornerPrefab);
        }

        void Start()
        {
            GenerateDungeon();
        }

        private Vector3 CalculateTileScale(GameObject prefab)
        {
            if (prefab == null) return Vector3.one;

            // Create a temporary instance to read its bounds in local space
            GameObject temp = Instantiate(prefab);
            temp.transform.position = new Vector3(99999, 99999, 99999); // move far away just in case it has effects

            Bounds bounds = new Bounds(temp.transform.position, Vector3.zero);
            bool hasBounds = false;

            foreach (Renderer r in temp.GetComponentsInChildren<Renderer>())
            {
                if (!hasBounds)
                {
                    bounds = r.bounds;
                    hasBounds = true;
                }
                else
                {
                    bounds.Encapsulate(r.bounds);
                }
            }

            if (!hasBounds)
            {
                MeshFilter mf = temp.GetComponentInChildren<MeshFilter>();
                if (mf != null && mf.sharedMesh != null)
                {
                    bounds = mf.sharedMesh.bounds;
                    hasBounds = true;
                }
            }

            DestroyImmediate(temp);

            if (!hasBounds || bounds.size.x <= 0.0001f || bounds.size.z <= 0.0001f)
            {
                return Vector3.one; // fallback, do not rescale
            }

            // How much we need to scale so that XZ footprint is exactly 1x1
            float scaleX = 1f / bounds.size.x;
            float scaleZ = 1f / bounds.size.z;
            // Y scaling is kept proportional so pieces keep their height ratio
            float scaleY = Mathf.Min(scaleX, scaleZ);

            return new Vector3(scaleX, scaleY, scaleZ);
        }

        public void GenerateDungeon()
        {
            ClearDungeon();
            enemySpawnPositions.Clear();
            playerSpawnPosition = Vector3.zero;
            grid = new int[dungeonWidth, dungeonHeight];
            rooms.Clear();

            // Generate rooms
            for (int i = 0; i < numRooms; i++)
            {
                Room room = new Room(
                    UnityEngine.Random.Range(1, dungeonWidth - maxRoomSize),
                    UnityEngine.Random.Range(1, dungeonHeight - maxRoomSize),
                    UnityEngine.Random.Range(minRoomSize, maxRoomSize),
                    UnityEngine.Random.Range(minRoomSize, maxRoomSize)
                );

                if (!RoomIntersects(room))
                {
                    rooms.Add(room);
                    CreateRoom(room);
                }
            }

            // Assign special room types
            AssignSpecialRoomTypes();

            // Ensure boss room is bigger (re-carve it after selection)
            EnlargeBossRoomIfAny();

            // Connect rooms with corridors
            for (int i = 0; i < rooms.Count - 1; i++)
            {
                ConnectRoomsWide(rooms[i], rooms[i + 1]);
            }

            // Place walls/corners (stacked to roof height)
            PlaceWalls();

            // Roof/ceiling (placed at y = wallHeightInTiles)
            if (generateRoof)
                PlaceRoof();

            // Compute logical spawn positions AFTER layout but BEFORE combining meshes
            ComputeSpawnPositions();

            if (combineIntoSingleMesh)
            {
                CombineDungeonIntoSingleMesh();
            }

            if (addBoundaryColliders)
                CreateBoundaryColliders();

            // NOW spawn props so they persist
            SpawnUpgradeStatues();
            SpawnSpecialRoomPrefabs();

            DungeonGenerated?.Invoke();
        }

        private void EnlargeBossRoomIfAny()
        {
            if (rooms == null || rooms.Count == 0) return;

            Room boss = null;
            foreach (var r in rooms)
            {
                if (r.Type == RoomType.Boss)
                {
                    boss = r;
                    break;
                }
            }
            if (boss == null) return;

            // If current boss is already large enough, keep it.
            int desiredW = Mathf.Clamp(UnityEngine.Random.Range(bossMinRoomSize, bossMaxRoomSize + 1), 1, dungeonWidth - 2);
            int desiredH = Mathf.Clamp(UnityEngine.Random.Range(bossMinRoomSize, bossMaxRoomSize + 1), 1, dungeonHeight - 2);

            if (boss.width >= desiredW && boss.height >= desiredH)
                return;

            // Recenter around current boss center and clamp to bounds.
            int cx = boss.x + boss.width / 2;
            int cy = boss.y + boss.height / 2;

            int newX = Mathf.Clamp(cx - desiredW / 2, 1, dungeonWidth - desiredW - 1);
            int newY = Mathf.Clamp(cy - desiredH / 2, 1, dungeonHeight - desiredH - 1);

            boss.x = newX;
            boss.y = newY;
            boss.width = desiredW;
            boss.height = desiredH;

            // Carve boss room area into the grid (floors). This can overwrite walls placed earlier by CreateRoom.
            // Note: we do this BEFORE corridors/walls are generated.
            CreateRoom(boss);
        }

        void CreateRoom(Room room)
        {
            for (int x = room.x; x < room.x + room.width; x++)
            {
                for (int y = room.y; y < room.y + room.height; y++)
                {
                    if (x < 0 || x >= dungeonWidth || y < 0 || y >= dungeonHeight)
                        continue;

                    grid[x, y] = 1; // Floor
                    if (floorPrefab != null)
                    {
                        var pos = new Vector3(x, 0, y);
                        GameObject f = Instantiate(floorPrefab, pos, Quaternion.identity, transform);
                        f.transform.localScale = Vector3.Scale(f.transform.localScale, floorScaleFactor);

                        // Highlight based on room type by swapping material if provided
                        ApplyRoomHighlight(room, f);
                    }
                }
            }
        }

        private void ApplyRoomHighlight(Room room, GameObject floorInstance)
        {
            Material mat = null;
            switch (room.Type)
            {
                case RoomType.Boss:
                    mat = bossRoomMaterial;
                    break;
                case RoomType.Shop:
                    mat = shopRoomMaterial;
                    break;
                case RoomType.Shrine:
                    mat = shrineRoomMaterial;
                    break;
            }
            if (mat == null) return;

            var rend = floorInstance.GetComponentInChildren<Renderer>();
            if (rend != null)
            {
                rend.material = mat;
            }
        }

        // Wide corridor connection using corridorWidth
        void ConnectRoomsWide(Room a, Room b)
        {
            int ax = a.x + a.width / 2;
            int ay = a.y + a.height / 2;
            int bx = b.x + b.width / 2;
            int by = b.y + b.height / 2;

            int x = ax;
            int y = ay;

            // First horizontal, then vertical, with width
            while (x != bx)
            {
                CarveCorridorTile(x, y);
                // widen perpendicular to movement (y axis)
                int half = Mathf.Max(0, corridorWidth - 1) / 2;
                for (int wy = -half; wy <= half; wy++)
                {
                    if (wy == 0) continue;
                    CarveCorridorTile(x, y + wy);
                }

                x += x < bx ? 1 : -1;
            }

            while (y != by)
            {
                CarveCorridorTile(x, y);
                // widen perpendicular to movement (x axis)
                int half = Mathf.Max(0, corridorWidth - 1) / 2;
                for (int wx = -half; wx <= half; wx++)
                {
                    if (wx == 0) continue;
                    CarveCorridorTile(x + wx, y);
                }

                y += y < by ? 1 : -1;
            }
        }

        private void CarveCorridorTile(int x, int y)
        {
            if (x < 0 || x >= dungeonWidth || y < 0 || y >= dungeonHeight)
                return;

            if (grid[x, y] == 1) return; // already floor (room etc.)

            grid[x, y] = 1;
            if (floorPrefab != null)
            {
                var pos = new Vector3(x, 0, y);
                GameObject f = Instantiate(floorPrefab, pos, Quaternion.identity, transform);
                f.transform.localScale = Vector3.Scale(f.transform.localScale, floorScaleFactor);
                // corridors are always normal; no special highlight
            }
        }

        void PlaceWalls()
        {
            for (int x = 0; x < dungeonWidth; x++)
            {
                for (int y = 0; y < dungeonHeight; y++)
                {
                    if (grid[x, y] == 1)
                    {
                        TryPlaceWallOrCorner(x + 1, y);   // east
                        TryPlaceWallOrCorner(x - 1, y);   // west
                        TryPlaceWallOrCorner(x, y + 1);   // north
                        TryPlaceWallOrCorner(x, y - 1);   // south
                    }
                }
            }
        }

        // Decide whether a given empty grid cell should host a straight wall or a corner piece
        private void TryPlaceWallOrCorner(int x, int y)
        {
            if (x < 0 || x >= dungeonWidth || y < 0 || y >= dungeonHeight)
                return;

            if (grid[x, y] != 0)
                return;

            if (!HasAdjacentFloor(x, y))
                return;

            bool isCorner = IsCorner(x, y);
            GameObject prefab = null;
            Vector3 scaleFactor = Vector3.one;

            if (isCorner && cornerPrefab != null)
            {
                prefab = cornerPrefab;
                scaleFactor = cornerScaleFactor;
            }
            else if (wallPrefab != null)
            {
                prefab = wallPrefab;
                scaleFactor = wallScaleFactor;
            }

            if (prefab == null) return;

            Quaternion rot = GetWallOrCornerRotation(x, y, isCorner);

            int h = Mathf.Max(1, wallHeightInTiles);
            for (int i = 0; i < h; i++)
            {
                GameObject inst = Instantiate(prefab, new Vector3(x, i, y), rot, transform);
                inst.transform.localScale = Vector3.Scale(inst.transform.localScale, scaleFactor);
            }

            grid[x, y] = 2;
        }



        private bool HasAdjacentFloor(int x, int y)
        {
            int[] dx = { -1, 1, 0, 0 };
            int[] dy = { 0, 0, -1, 1 };
            for (int i = 0; i < 4; i++)
            {
                int nx = x + dx[i];
                int ny = y + dy[i];
                if (nx >= 0 && nx < dungeonWidth && ny >= 0 && ny < dungeonHeight && grid[nx, ny] == 1)
                    return true;
            }
            return false;
        }

        bool IsCorner(int x, int y)
        {
            int floorNeighbors = 0;
            int[] dx = {-1, 1, 0, 0};
            int[] dy = {0, 0, -1, 1};

            for (int i = 0; i < 4; i++)
            {
                int nx = x + dx[i];
                int ny = y + dy[i];
                if (nx >= 0 && nx < dungeonWidth && ny >= 0 && ny < dungeonHeight && grid[nx, ny] == 1)
                    floorNeighbors++;
            }

            return floorNeighbors == 2;
        }

        // New: unified rotation calculation for both walls and corners so that they
        // always face toward their adjacent floor tiles and align with the grid.
        Quaternion GetWallOrCornerRotation(int x, int y, bool isCorner)
        {
            bool hasFloorLeft = x > 0 && grid[x - 1, y] == 1;
            bool hasFloorRight = x < dungeonWidth - 1 && grid[x + 1, y] == 1;
            bool hasFloorUp = y < dungeonHeight - 1 && grid[x, y + 1] == 1;
            bool hasFloorDown = y > 0 && grid[x, y - 1] == 1;

            if (!isCorner)
            {
                // Straight wall: align its long edge along the side of the floor
                if (hasFloorLeft || hasFloorRight) return Quaternion.Euler(0, 90, 0);   // horizontal wall
                if (hasFloorUp || hasFloorDown) return Quaternion.identity;             // vertical wall
                return Quaternion.identity;
            }

            // Corner: choose rotation based on which two sides have floors (L shape)
            if (hasFloorLeft && hasFloorUp) return Quaternion.Euler(0, 0, 0);
            if (hasFloorUp && hasFloorRight) return Quaternion.Euler(0, 90, 0);
            if (hasFloorRight && hasFloorDown) return Quaternion.Euler(0, 180, 0);
            if (hasFloorDown && hasFloorLeft) return Quaternion.Euler(0, 270, 0);

            return Quaternion.identity;
        }

        private void AssignSpecialRoomTypes()
        {
            // rooms can be less than numRooms if many candidates intersect.
            if (rooms == null || rooms.Count == 0) return;

            // Reset
            for (int i = 0; i < rooms.Count; i++)
                rooms[i].Type = RoomType.Normal;

            // Assign one boss room
            int bossRoomIndex = UnityEngine.Random.Range(0, rooms.Count);
            rooms[bossRoomIndex].Type = RoomType.Boss;

            if (rooms.Count <= 1) return;

            // Shop
            int shopRoomIndex;
            do
            {
                shopRoomIndex = UnityEngine.Random.Range(0, rooms.Count);
            } while (shopRoomIndex == bossRoomIndex);
            rooms[shopRoomIndex].Type = RoomType.Shop;

            if (rooms.Count <= 2) return;

            // Shrine
            int shrineRoomIndex;
            do
            {
                shrineRoomIndex = UnityEngine.Random.Range(0, rooms.Count);
            } while (shrineRoomIndex == bossRoomIndex || shrineRoomIndex == shopRoomIndex);
            rooms[shrineRoomIndex].Type = RoomType.Shrine;
        }

        bool RoomIntersects(Room room)
        {
            foreach (Room other in rooms)
            {
                if (room.Intersects(other)) return true;
            }
            return false;
        }

        void ClearDungeon()
        {
            foreach (Transform child in transform)
            {
                Destroy(child.gameObject);
            }
        }

        /// <summary>
        /// Combine all child meshes under this generator into a single mesh object.
        /// Assumes the whole dungeon uses the same material (the material of the first MeshRenderer found).
        /// This drastically reduces draw calls and shadow casters.
        /// </summary>
        private void CombineDungeonIntoSingleMesh()
        {
            // Collect all MeshFilters of generated tiles
            MeshFilter[] meshFilters = GetComponentsInChildren<MeshFilter>();
            if (meshFilters.Length == 0) return;

            Material sharedMat = null;

            // Group non-roof meshes by chunk key, roof meshes separately.
            var chunkCombines = new Dictionary<Vector2Int, List<CombineInstance>>();
            var roofCombines = new List<CombineInstance>();

            foreach (var mf in meshFilters)
            {
                if (mf == null || mf.sharedMesh == null) continue;

                bool isRoof = mf.GetComponentInParent<DungeonRoofMarker>() != null;

                MeshRenderer mr = mf.GetComponent<MeshRenderer>();
                if (mr != null && sharedMat == null)
                    sharedMat = mr.sharedMaterial;

                CombineInstance ci = new CombineInstance
                {
                    mesh = mf.sharedMesh,
                    transform = mf.transform.localToWorldMatrix
                };

                if (isRoof)
                {
                    roofCombines.Add(ci);
                    continue;
                }

                // Determine chunk by world position (tile-based)
                Vector3 p = mf.transform.position;
                int cs = Mathf.Max(0, colliderChunkSize);
                Vector2Int key;
                if (cs <= 0)
                {
                    key = Vector2Int.zero; // single chunk
                }
                else
                {
                    int cx = Mathf.FloorToInt(p.x / cs);
                    int cz = Mathf.FloorToInt(p.z / cs);
                    key = new Vector2Int(cx, cz);
                }

                if (!chunkCombines.TryGetValue(key, out var list))
                {
                    list = new List<CombineInstance>();
                    chunkCombines.Add(key, list);
                }
                list.Add(ci);
            }

            if (chunkCombines.Count == 0 && roofCombines.Count == 0) return;

            // Snapshot children before destroying.
            var children = new List<Transform>();
            foreach (Transform child in transform)
                children.Add(child);

            // Destroy all original tile instances (including roof). We'll recreate them as combined meshes.
            foreach (var child in children)
            {
                if (child == null) continue;
                Destroy(child.gameObject);
            }

            int groundLayer = LayerMask.NameToLayer("Ground");

            // Build chunks
            int chunkIndex = 0;
            foreach (var kvp in chunkCombines)
            {
                var combines = kvp.Value;
                if (combines == null || combines.Count == 0) continue;

                GameObject combinedGo = new GameObject($"CombinedDungeonChunk_{kvp.Key.x}_{kvp.Key.y}_{chunkIndex}");
                combinedGo.transform.SetParent(transform, false);
                if (groundLayer != -1)
                    combinedGo.layer = groundLayer;

                Mesh combinedMesh = new Mesh();
                combinedMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
                combinedMesh.CombineMeshes(combines.ToArray(), true, true);

                var combinedMf = combinedGo.AddComponent<MeshFilter>();
                combinedMf.sharedMesh = combinedMesh;

                var combinedMr = combinedGo.AddComponent<MeshRenderer>();
                if (sharedMat != null)
                    combinedMr.sharedMaterial = sharedMat;

                if (addMeshCollider)
                {
                    var mc = combinedGo.AddComponent<MeshCollider>();
                    mc.sharedMesh = combinedMesh;
                    mc.convex = false;
                }

                chunkIndex++;
            }

            // Combined roof (no collider by default)
            if (roofCombines.Count > 0)
            {
                GameObject combinedRoofGo = new GameObject("CombinedRoofMesh");
                combinedRoofGo.transform.SetParent(transform, false);

                Mesh roofMesh = new Mesh();
                roofMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
                roofMesh.CombineMeshes(roofCombines.ToArray(), true, true);

                var roofMf = combinedRoofGo.AddComponent<MeshFilter>();
                roofMf.sharedMesh = roofMesh;

                var roofMr = combinedRoofGo.AddComponent<MeshRenderer>();
                if (sharedMat != null)
                    roofMr.sharedMaterial = sharedMat;

                if (roofHasCollision)
                {
                    var roofCol = combinedRoofGo.AddComponent<MeshCollider>();
                    roofCol.sharedMesh = roofMesh;
                    roofCol.convex = false;
                }
            }
        }

        private void SpawnUpgradeStatues()
        {
            if (upgradeStatuePrefab == null) return;
            if (rooms == null || rooms.Count == 0) return;
            // Ensure a persistent parent that won't be combined/cleared
            var parent = GameObject.Find("DungeonPropsParent");
            if (parent == null)
            {
                parent = new GameObject("DungeonPropsParent");
                parent.transform.SetParent(transform, false);
            }
            var statuesParent = new GameObject("UpgradeStatues");
            statuesParent.transform.SetParent(parent.transform, false);
            foreach (var room in rooms)
            {
                if (room.Type != RoomType.Shrine) continue;
                float centerX = room.x + room.width / 2f + 0.5f;
                float centerZ = room.y + room.height / 2f + 0.5f;
                var pos = new Vector3(centerX, 0.5f, centerZ);
                Instantiate(upgradeStatuePrefab, pos, Quaternion.identity, statuesParent.transform);
            }
        }

        private void SpawnSpecialRoomPrefabs()
        {
            if (rooms == null || rooms.Count == 0) return;
            var parent = GameObject.Find("DungeonPropsParent");
            if (parent == null)
            {
                parent = new GameObject("DungeonPropsParent");
                parent.transform.SetParent(transform, false);
            }

            Transform bossParent = null;
            Transform shopParent = null;

            if (bossRoomPrefab != null)
            {
                var go = new GameObject("BossRooms");
                go.transform.SetParent(parent.transform, false);
                bossParent = go.transform;
            }

            if (shopRoomPrefab != null)
            {
                var go = new GameObject("ShopRooms");
                go.transform.SetParent(parent.transform, false);
                shopParent = go.transform;
            }

            foreach (var room in rooms)
            {
                float centerX = room.x + room.width / 2f + 0.5f;
                float centerZ = room.y + room.height / 2f + 0.5f;
                var pos = new Vector3(centerX, 0.5f, centerZ);

                if (room.Type == RoomType.Boss && bossRoomPrefab != null)
                    Instantiate(bossRoomPrefab, pos, Quaternion.identity, bossParent);

                if (room.Type == RoomType.Shop && shopRoomPrefab != null)
                    Instantiate(shopRoomPrefab, pos, Quaternion.identity, shopParent);
            }
        }

        private void ComputeSpawnPositions()
        {
            if (rooms == null || rooms.Count == 0)
                return;

            // Player spawn = exact center of start room, aligned to tile center (0.5 offset)
            Room startRoom = rooms[0];
            float centerX = startRoom.x + startRoom.width / 2f + 0.5f;
            float centerZ = startRoom.y + startRoom.height / 2f + 0.5f;

            // Default fallback if we can't resolve height.
            playerSpawnPosition = new Vector3(centerX, 1f, centerZ);

            // Resolve exact floor height (ignoring roof hits) if possible.
            playerSpawnPosition = ResolveSpawnToFloor(playerSpawnPosition);

            // Enemy spawns on all floor tiles except the player tile
            enemySpawnPositions.Clear();
            for (int x = 0; x < dungeonWidth; x++)
            {
                for (int y = 0; y < dungeonHeight; y++)
                {
                    if (grid[x, y] != 1) continue;

                    Vector3 pos = new Vector3(x + 0.5f, 1f, y + 0.5f);
                    pos = ResolveSpawnToFloor(pos);

                    if (Mathf.Approximately(pos.x, playerSpawnPosition.x) && Mathf.Approximately(pos.z, playerSpawnPosition.z))
                        continue;

                    enemySpawnPositions.Add(pos);
                }
            }
        }

        private Vector3 ResolveSpawnToFloor(Vector3 approxPos)
        {
            // Cast from above the roof to guarantee we hit something.
            float rayStartY = Mathf.Max(wallHeightInTiles + 5f, approxPos.y + 10f);
            Vector3 origin = new Vector3(approxPos.x, rayStartY, approxPos.z);

            // Collect all hits so we can choose the highest non-roof surface (the floor),
            // even if the roof has colliders and would be hit first.
            RaycastHit[] hits = Physics.RaycastAll(origin, Vector3.down, 200f, spawnGroundMask, QueryTriggerInteraction.Ignore);
            if (hits == null || hits.Length == 0)
                return approxPos;

            Array.Sort(hits, (a, b) => b.point.y.CompareTo(a.point.y)); // highest first

            for (int i = 0; i < hits.Length; i++)
            {
                var h = hits[i];
                if (h.collider == null) continue;

                // Ignore roof tiles/meshes.
                if (h.collider.GetComponentInParent<DungeonRoofMarker>() != null)
                    continue;

                return h.point + Vector3.up * spawnHeightOffset;
            }

            // If everything we hit was roof, fall back.
            return approxPos;
        }

        private void PlaceRoof()
        {
            // Prefer explicit roofPrefab, otherwise reuse floorPrefab.
            var prefab = roofPrefab != null ? roofPrefab : floorPrefab;

            // If nothing is assigned, fall back to a simple primitive so the roof still generates.
            if (prefab == null)
            {
                Debug.LogWarning("[DungeonGenerator] No roofPrefab or floorPrefab assigned; generating roof using Cube primitives as fallback.");
            }

            float roofY = Mathf.Max(1, wallHeightInTiles);
            int roofCount = 0;

            for (int x = 0; x < dungeonWidth; x++)
            {
                for (int y = 0; y < dungeonHeight; y++)
                {
                    if (grid[x, y] != 1) continue;

                    // Match floor tile coordinate convention used elsewhere: tiles are centered at +0.5f.
                    Vector3 roofPos = new Vector3(x + 0.5f, roofY, y + 0.5f);

                    GameObject r;
                    if (prefab != null)
                    {
                        r = Instantiate(prefab, roofPos, Quaternion.identity, transform);
                        // Reuse floor scaling so roof tiles stay 1x1.
                        r.transform.localScale = Vector3.Scale(r.transform.localScale, floorScaleFactor);
                    }
                    else
                    {
                        r = GameObject.CreatePrimitive(PrimitiveType.Cube);
                        r.transform.SetParent(transform, false);
                        r.transform.position = roofPos;
                        r.transform.localScale = new Vector3(1f, 0.2f, 1f);
                    }

                    r.name = $"Roof_{x}_{y}";

                    // Mark as roof without requiring a Unity Tag.
                    if (r.GetComponent<DungeonRoofMarker>() == null)
                        r.AddComponent<DungeonRoofMarker>();

                    if (!roofHasCollision)
                    {
                        foreach (var col in r.GetComponentsInChildren<Collider>(true))
                            col.enabled = false;
                    }

                    roofCount++;
                }
            }

            if (logRoofGeneration)
                Debug.Log($"[DungeonGenerator] Roof generated: {roofCount} tiles at y={roofY} (generateRoof={generateRoof}).");
        }

        [Header("Debug")]
        public bool logRoofGeneration = false;

        [Header("Collision Helpers")]
        [Tooltip("Creates simple box colliders around the dungeon edges to prevent escaping through corners or over walls.")]
        public bool addBoundaryColliders = true;

        [Tooltip("Height of boundary colliders. Set higher than the maximum jump.")]
        public float boundaryHeight = 6f;

        [Tooltip("Thickness of boundary colliders.")]
        public float boundaryThickness = 1f;

        private void CreateBoundaryColliders()
        {
            var parentGo = GameObject.Find("DungeonCollisionBounds");
            if (parentGo == null)
            {
                parentGo = new GameObject("DungeonCollisionBounds");
                parentGo.transform.SetParent(transform, false);
            }
            else
            {
                // Clear old bounds if regenerating.
                foreach (Transform c in parentGo.transform)
                    Destroy(c.gameObject);
            }

            float minX = 0f;
            float maxX = dungeonWidth;
            float minZ = 0f;
            float maxZ = dungeonHeight;

            float yCenter = boundaryHeight * 0.5f;

            // North
            CreateBoundaryWall(parentGo.transform,
                new Vector3((minX + maxX) * 0.5f, yCenter, maxZ + boundaryThickness * 0.5f),
                new Vector3((maxX - minX) + boundaryThickness * 2f, boundaryHeight, boundaryThickness));

            // South
            CreateBoundaryWall(parentGo.transform,
                new Vector3((minX + maxX) * 0.5f, yCenter, minZ - boundaryThickness * 0.5f),
                new Vector3((maxX - minX) + boundaryThickness * 2f, boundaryHeight, boundaryThickness));

            // East
            CreateBoundaryWall(parentGo.transform,
                new Vector3(maxX + boundaryThickness * 0.5f, yCenter, (minZ + maxZ) * 0.5f),
                new Vector3(boundaryThickness, boundaryHeight, (maxZ - minZ) + boundaryThickness * 2f));

            // West
            CreateBoundaryWall(parentGo.transform,
                new Vector3(minX - boundaryThickness * 0.5f, yCenter, (minZ + maxZ) * 0.5f),
                new Vector3(boundaryThickness, boundaryHeight, (maxZ - minZ) + boundaryThickness * 2f));
        }

        private static void CreateBoundaryWall(Transform parent, Vector3 center, Vector3 size)
        {
            var go = new GameObject("Boundary");
            go.transform.SetParent(parent, false);
            go.transform.position = center;
            var bc = go.AddComponent<BoxCollider>();
            bc.size = size;
        }
    }

    public class Room
    {
        public int x, y, width, height;
        public RoomType Type = RoomType.Normal;

        public Room(int x, int y, int width, int height)
        {
            this.x = x;
            this.y = y;
            this.width = width;
            this.height = height;
        }

        public bool Intersects(Room other)
        {
            return x < other.x + other.width + 1 &&
                   x + width + 1 > other.x &&
                   y < other.y + other.height + 1 &&
                   y + height + 1 > other.y;
        }
    }
    
}
