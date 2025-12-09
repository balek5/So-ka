using UnityEngine;
using System.Collections.Generic;
using System;

namespace Dungeon_Generator
{
    public enum RoomType
    {
        Normal,
        Boss,
        Shop,
        Shrine
    }

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

        [Header("Spawning (logical positions only)")]
        [Tooltip("Logical spawn position for the player (center of the start room). You can use this from another script to place the player.")]
        public Vector3 playerSpawnPosition;

        [Tooltip("Logical spawn positions for enemies on valid floor tiles. Used by WaveSpawner or other systems.")]
        public List<Vector3> enemySpawnPositions = new List<Vector3>();
        public GameObject bossRoomPrefab;
        public GameObject shopRoomPrefab;

   
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

            // Assign special room types: last is boss, previous is shop, previous is shrine (if enough rooms)
            AssignSpecialRoomTypes();

            // Connect rooms with corridors that can have custom width
            for (int i = 0; i < rooms.Count - 1; i++)
            {
                ConnectRoomsWide(rooms[i], rooms[i + 1]);
            }

            // Place walls and corners
            PlaceWalls();

// Compute logical spawn positions AFTER layout but BEFORE combining meshes
            ComputeSpawnPositions();

            // Remove spawning before combine to avoid deletion
            // SpawnUpgradeStatues();
            // SpawnSpecialRoomPrefabs();

            if (combineIntoSingleMesh)
            {
                CombineDungeonIntoSingleMesh();
            }

            // NOW spawn props so they persist
            SpawnUpgradeStatues();
            SpawnSpecialRoomPrefabs();

            DungeonGenerated?.Invoke();
        }

        private void ComputeSpawnPositions()
        {
            if (rooms.Count == 0)
                return;

            // Player spawn = exact center of start room, aligned to tile center (0.5 offset)
            Room startRoom = rooms[0];
            float centerX = startRoom.x + startRoom.width / 2f + 0.5f;
            float centerZ = startRoom.y + startRoom.height / 2f + 0.5f;
            // Spawn slightly above floor to avoid clipping
            playerSpawnPosition = new Vector3(centerX, 1f, centerZ);

            // Enemy spawns on all floor tiles except the player tile
            for (int x = 0; x < dungeonWidth; x++)
            {
                for (int y = 0; y < dungeonHeight; y++)
                {
                    if (grid[x, y] != 1) continue; // floor only

                    Vector3 pos = new Vector3(x + 0.5f, 0f, y + 0.5f);

                    // Avoid the exact player spawn tile
                    if (Mathf.Approximately(pos.x, playerSpawnPosition.x) && Mathf.Approximately(pos.z, playerSpawnPosition.z))
                        continue;

                    enemySpawnPositions.Add(pos);
                }
            }
        }

        private void AssignSpecialRoomTypes()
        {
            if (rooms.Count == 0) return;

            // Default all to normal
            foreach (var r in rooms)
                r.Type = RoomType.Normal;

            // Boss room: furthest from first room (simple heuristic)
            Room start = rooms[0];
            Room boss = start;
            float maxDist = -1f;
            foreach (var r in rooms)
            {
                float dx = (r.x + r.width / 2f) - (start.x + start.width / 2f);
                float dy = (r.y + r.height / 2f) - (start.y + start.height / 2f);
                float d = dx * dx + dy * dy;
                if (d > maxDist)
                {
                    maxDist = d;
                    boss = r;
                }
            }
            boss.Type = RoomType.Boss;

            // Shop: closest to start but not start and not boss
            Room shop = null;
            maxDist = float.MaxValue;
            foreach (var r in rooms)
            {
                if (r == start || r == boss) continue;
                float dx = (r.x + r.width / 2f) - (start.x + start.width / 2f);
                float dy = (r.y + r.height / 2f) - (start.y + start.height / 2f);
                float d = dx * dx + dy * dy;
                if (d < maxDist)
                {
                    maxDist = d;
                    shop = r;
                }
            }
            if (shop != null)
                shop.Type = RoomType.Shop;

            // Shrine: some other random room that is not start/boss/shop
            List<Room> candidates = new List<Room>();
            foreach (var r in rooms)
            {
                if (r != start && r != boss && r != shop)
                    candidates.Add(r);
            }
            if (candidates.Count > 0)
            {
                Room shrine = candidates[UnityEngine.Random.Range(0, candidates.Count)];
                shrine.Type = RoomType.Shrine;
            }
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
                        // Check the four cardinal directions only to keep walls aligned to tile edges
                        TryPlaceWallOrCorner(x + 1, y, 1, 0);   // east
                        TryPlaceWallOrCorner(x - 1, y, -1, 0);  // west
                        TryPlaceWallOrCorner(x, y + 1, 0, 1);   // north
                        TryPlaceWallOrCorner(x, y - 1, 0, -1);  // south
                    }
                }
            }
        }

        // Decide whether a given empty grid cell should host a straight wall or a corner piece
        private void TryPlaceWallOrCorner(int x, int y, int dx, int dy)
        {
            if (x < 0 || x >= dungeonWidth || y < 0 || y >= dungeonHeight)
                return;

            if (grid[x, y] != 0)
                return; // already used by something else

            // Check if this empty tile borders at least one floor: if not, skip
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
            GameObject inst = Instantiate(prefab, new Vector3(x, 0, y), rot, transform);
            inst.transform.localScale = Vector3.Scale(inst.transform.localScale, scaleFactor);

            // mark as wall so we do not reuse this tile
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

            // Use the material from the first renderer we find
            Material sharedMat = null;
            List<CombineInstance> combines = new List<CombineInstance>();

            foreach (var mf in meshFilters)
            {
                if (mf.sharedMesh == null) continue;
                MeshRenderer mr = mf.GetComponent<MeshRenderer>();
                if (mr != null && sharedMat == null)
                {
                    sharedMat = mr.sharedMaterial;
                }

                CombineInstance ci = new CombineInstance
                {
                    mesh = mf.sharedMesh,
                    transform = mf.transform.localToWorldMatrix
                };
                combines.Add(ci);
            }

            if (combines.Count == 0) return;

            // Remove all old children (individual tiles)
            foreach (Transform child in transform)
            {
                Destroy(child.gameObject);
            }

            // Create the combined object
            GameObject combinedGO = new GameObject("CombinedDungeonMesh");
            combinedGO.transform.SetParent(transform, false);
            int groundLayer = LayerMask.NameToLayer("Ground");
            if (groundLayer != -1)
            {
                combinedGO.layer = groundLayer;
            }
            Mesh combinedMesh = new Mesh();
            combinedMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32; // support large vertex counts
            combinedMesh.CombineMeshes(combines.ToArray(), true, true);

            MeshFilter combinedMF = combinedGO.AddComponent<MeshFilter>();
            combinedMF.sharedMesh = combinedMesh;

            MeshRenderer combinedMR = combinedGO.AddComponent<MeshRenderer>();
            if (sharedMat != null)
            {
                combinedMR.sharedMaterial = sharedMat;
            }

            // Optional: add a single MeshCollider for the whole dungeon
            if (addMeshCollider)
            {
                MeshCollider mc = combinedGO.AddComponent<MeshCollider>();
                mc.sharedMesh = combinedMesh;
                mc.convex = false; // static environment collider
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
