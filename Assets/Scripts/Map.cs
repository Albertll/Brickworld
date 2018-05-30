using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Assets.Scripts
{
    [Serializable]
    public class Map
    {
        private readonly Dictionary<Point, Chunk> _chunks = new Dictionary<Point, Chunk>();

        public Point Position { get; set; } = new Point(0, 0, 50);
        
        public Chunk GetChunk(int a, int b)
        {
            var point = new Point(a, b);

            if (!_chunks.ContainsKey(point))
                _chunks.Add(point, new Chunk(a, b));

            return _chunks[point];
        }

        public void AddChunk(int a, int b, Chunk chunk)
        {
            _chunks.Add(new Point(a, b), chunk);
        }

        public bool ChunkCreated(int a, int b)
        {
            return _chunks.ContainsKey(new Point(a, b));
        }

        public void GetChunkCoords(int x, int y, out int a, out int b)
        {
            a = (int)Mathf.Floor((float)x / Chunk.ChunkSize);
            b = (int)Mathf.Floor((float)y / Chunk.ChunkSize);
        }

        public Chunk GetChunkXy(int x, int y)
        {
            int a, b;
            GetChunkCoords(x, y, out a, out b);
            return GetChunk(a, b);
        }

        public HeightBricks GetHeightBricks(int x, int y, bool createNewChunk = true)
        {
            int a, b;
            GetChunkCoords(x, y, out a, out b);
            x -= a * Chunk.ChunkSize;
            y -= b * Chunk.ChunkSize;

            if (!createNewChunk && !ChunkCreated(a, b)) return null;

            return GetChunk(a, b).Bricks[x][y];
        }

        public IEnumerable<Tuple<int, int>> GetNearestNonexistedChunks(int x, int y)
        {
            const int neighbourLength = 3;

            int a, b;
            GetChunkCoords(x, y, out a, out b);

            for (var aa = a - neighbourLength; aa <= a + neighbourLength; aa++)
            for (var bb = b - neighbourLength; bb <= b + neighbourLength; bb++)
                if (!ChunkCreated(aa, bb))
                    yield return new Tuple<int, int>(aa, bb);
        }

        public IEnumerable<Chunk> GetNearestChunks(int x, int y)
        {
            const int neighbourLength = 3;

            int a, b;
            GetChunkCoords(x, y, out a, out b);

            for (var aa = a - neighbourLength; aa <= a + neighbourLength; aa++)
            for (var bb = b - neighbourLength; bb <= b + neighbourLength; bb++)
                if (ChunkCreated(aa, bb))
                    yield return GetChunk(aa, bb);
        }

        public IEnumerable<Chunk> GetFarChunks(int x, int y)
        {
            const int minDistance = 6;

            int a, b;
            GetChunkCoords(x, y, out a, out b);

            return _chunks
                .Where(kvp => GetDistance(kvp.Key, new Point(a, b)) > minDistance)
                .Select(kvp => kvp.Value);
        }

        private static double GetDistance(Point a, Point b)
        {
            return Math.Sqrt(
                (a.X - b.X) * (a.X - b.X) +
                (a.Y - b.Y) * (a.Y - b.Y) +
                (a.Z - b.Z) * (a.Z - b.Z));
        }
    }
}