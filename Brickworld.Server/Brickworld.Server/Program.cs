using System;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Assets.Scripts;

namespace Brickworld.Server
{
    public class Program
    {
        private const int PortNumber = 5000;
        private const string ServerIp = "127.0.0.1";
        private BwTcpServer _server;

        private Map _map;

        private static void Main()
        {
            Debug.Listeners.Add(new ConsoleTraceListener());
            new Program().RunServer();
        }

        private void RunServer()
        {
            try
            {
                _map = Serializer.DeserializeFromFile<Map>(@"C:\Users\Albert\Desktop\map.map");
            }
            catch (Exception)
            { }

            if (_map == null)
                _map = new Map();
            else
                Console.WriteLine("Map loaded");
            
            
            _server = new BwTcpServer(ServerIp, PortNumber);

            _server.ReadData += Server_ReadData; 
            
            Console.WriteLine("Press any key to end server");

            Console.ReadLine();

            Serializer.SerializeToFile(_map, @"C:\Users\Albert\Desktop\map.map");
        }

        private void Server_ReadData(byte[] buff)
        {
            Console.WriteLine("got tcp data " + buff.Length);
            var data = Encoding.ASCII.GetString(buff, 0, Math.Min(buff.Length, 100));
            Console.WriteLine(data);
            if (data.StartsWith("get_map"))
            {
                _server.SendData(_map.Serialize());
            }

            if (data.StartsWith("get_chunk"))
            {
                var lines = data.Split(' ');

                var newChunk = _map.GetChunk(int.Parse(lines[1]), int.Parse(lines[2]));

                _server.SendData(newChunk.Serialize());
            }

            if (data.StartsWith("position"))
            {
                var lines = data.Split(' ');

                _map.Position = new Point(int.Parse(lines[1]), int.Parse(lines[2]), int.Parse(lines[3]));
            }

            if (data.StartsWith("create_block"))
            {
                var lines = data.Split(' ');

                var hb = _map.GetHeightBricks(int.Parse(lines[1]), int.Parse(lines[2]));
                hb.Bricks.Add(new Brick(int.Parse(lines[3]), int.Parse(lines[4])));
            }

            if (data.StartsWith("drop_block"))
            {
                var lines = data.Split(' ');

                var hb = _map.GetHeightBricks(int.Parse(lines[1]), int.Parse(lines[2]));
                var brick = hb.Bricks.FirstOrDefault(b => b.Z == int.Parse(lines[3]));
                if (brick != null)
                hb.Bricks.Remove(brick);
            }
        }
    }
}
