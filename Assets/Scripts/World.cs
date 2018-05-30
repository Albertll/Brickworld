using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using UnityEngine;

namespace Assets.Scripts
{
    public class World
    {
        private readonly Dictionary<Brick, GameObject> _brickGameObjects = new Dictionary<Brick, GameObject>();
        private readonly Collection<Point> _tcpAskedBricks = new Collection<Point>();

        private readonly IBrickWorldEngine _engine;
        private readonly BwTcpClient _client;

        public Map Map { get; set; }

        private readonly int[][] _sidesDirections =
        {
            new[] {0, 0, 1},
            new[] {-1, 0, 0},
            new[] {0, 1, 0},
            new[] {0, 0, -1},
            new[] {1, 0, 0},
            new[] {0, -1, 0}
        };

        private readonly int[][] _neighboursDirections =
        {
            new[] {0, 1},
            new[] {0, -1},
            new[] {1, 0},
            new[] {-1, 0}
        };

        public World(IBrickWorldEngine engine, Map map, BwTcpClient client)
        {
            _engine = engine;
            _client = client;
            Map = map;
        }
        
        public void Refresh(int x, int y, int z)
        {
            foreach (var coords in Map.GetNearestNonexistedChunks(x, y))
            {
                var p = new Point(coords.Item1, coords.Item2);
                if (_tcpAskedBricks.Contains(p))
                    continue;

                _tcpAskedBricks.Add(p);
                Debug.Log($"sending get chunk { coords.Item1} { coords.Item2}");
                _client.SendText($"get_chunk {coords.Item1} {coords.Item2} ");
            }

            foreach (var chunk in Map.GetNearestChunks(x, y).Where(c => !c.Loaded))
            {
                LoadChunk(chunk);
                return;
            }

            foreach (var chunk in Map.GetFarChunks(x, y).Where(c => c.Loaded))
            {
                UnloadChunk(chunk);
                return;
            }
        }

        private void LoadChunk(Chunk chunk)
        {
            Debug.Log("Loading chunk: " + chunk.A + ", " + chunk.B);

            for (var x = 0; x < Chunk.ChunkSize; x++)
            for (var y = 0; y < Chunk.ChunkSize; y++)
            {
                foreach (var brick in chunk.Bricks[x][y].SortedBricks.ToList())
                {
                    AddNewBrick(
                        chunk.A * Chunk.ChunkSize + x,
                        chunk.B * Chunk.ChunkSize + y,
                        brick.Z, brick.Type);
                }
            }

            chunk.Loaded = true;
        }

        private void UnloadChunk(Chunk chunk)
        {
            Debug.Log("Unloading chunk: " + chunk.A + ", " + chunk.B);

            for (var x = 0; x < Chunk.ChunkSize; x++)
            for (var y = 0; y < Chunk.ChunkSize; y++)
            {
                foreach (var brick in chunk.Bricks[x][y].Bricks)
                {
                    _engine.DestroyBrick(_brickGameObjects[brick]);
                    _brickGameObjects.Remove(brick);
                }
            }

            chunk.Loaded = false;
        }

        private void DigWorld(int x, int y, int z, bool dig = true)
        {
            Debug.Log("dig " + x + ", " + y + ", " + z);

            var heightBricks = Map.GetHeightBricks(x, y);

            while (heightBricks.Height >= z)
            {
                Debug.Log("dig dig " + heightBricks.Height + ", " + z + ",     " + heightBricks.Bricks.Count);
                //_engine.SetBrickAsFloor(_brickGameObjects[heightBricks.SortedBricks.First()], false);
                AddNewBrick(x, y, heightBricks.Height - 1, 1);
            }

            if (!dig) return;

            foreach (var direction in _neighboursDirections)
            {
                var x2 = x + direction[0];
                var y2 = y + direction[1];

                heightBricks = Map.GetHeightBricks(x2, y2);
                if (heightBricks != null && z <= heightBricks.Height)
                {
                    DigWorld(x2, y2, z, false);
                }
            }
        }

        private bool ShouldDig(int x, int y, int z)
        {
            foreach (var direction in _neighboursDirections)
            {
                var heightBricks = Map.GetHeightBricks(x + direction[0], y + direction[1]);
                if (heightBricks != null && z <= heightBricks.Height)
                {
                    return true;
                }
            }

            return false;
        }

        public void RemoveBrick(int x, int y, int z)
        {
            Debug.Log("remove: " + x + ", " + y + ", " + z);
            
            var heightBricks = Map.GetHeightBricks(x, y);
            heightBricks.Bricks.Remove(heightBricks.Bricks.First(b => b.Z == z));
            _client.SendText($"drop_block {x} {y} {z}");

            for (var sideId = 0; sideId < 6; sideId++)
            {
                var direction = _sidesDirections[sideId];
                
                var x2 = x + direction[0];
                var y2 = y + direction[1];
                var z2 = z + direction[2];

                heightBricks = Map.GetHeightBricks(x2, y2);
                var brick = heightBricks?.Bricks.FirstOrDefault(b => b.Z == z2);
                if (brick == null)
                    continue;
                if (_brickGameObjects.ContainsKey(brick))
                    _engine.SetSideVisible(_brickGameObjects[brick], (sideId + 3) % 6);
            }

            if (!ShouldDig(x, y, z))
                return;

            Debug.Log("digging");
            
            if (!Map.GetHeightBricks(x, y).Bricks.Any(b => b.Z == z - 1))
                AddNewBrick(x, y, z - 1, 1);

            DigWorld(x, y, z);
        }

        public void AddNewBrick(int x, int y, int z, int type)
        {
            var heightBricks = Map.GetHeightBricks(x, y);

            var brick = heightBricks.Bricks.FirstOrDefault(b => b.Z == z);
            if (brick == null)
            {
                brick = new Brick(z, type);
                heightBricks.Bricks.Add(brick);
                _client.SendText($"create_block {x} {y} {z} {type}");
            }
            else
            {
                if (_brickGameObjects.ContainsKey(brick))
                    return;
            }

            var gameObject = _engine.InsertBrick(x, y, z, brick.Type);

            //Debug.Log("added brick " + x + ", " + y + ", " + z);

            _brickGameObjects.Add(brick, gameObject);

            for (var sideId = 0; sideId < 6; sideId++)
            {
                var direction = _sidesDirections[sideId];

                var x2 = x + direction[0];
                var y2 = y + direction[1];
                var z2 = z + direction[2];

                heightBricks = Map.GetHeightBricks(x2, y2, false);
                if (heightBricks == null)
                {
                    _engine.SetSideVisible(gameObject, sideId);
                    continue;
                }

                // no floor from ground
                if (z2 < z && heightBricks.Height == z)
                    continue;

                brick = heightBricks.Bricks.FirstOrDefault(b => b.Z == z2);

                if (brick == null && z == z2 && z > heightBricks.Height + 1 &&
                    !Map.GetHeightBricks(x, y).Bricks.Any(b => b.Z <= z - 1))
                    AddNewBrick(x, y, z - 1, 1);

                if (brick != null || z2 <= heightBricks.Height)
                    continue;
                
                _engine.SetSideVisible(gameObject, sideId);
            }
        }
    }
}