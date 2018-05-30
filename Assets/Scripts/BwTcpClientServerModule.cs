using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Task = System.Threading.Tasks.Task;

namespace Assets.Scripts
{
    public abstract class BwTcpClientServerModule
    {
        protected Stream Stream;
        protected int BufferSize;
        public event Action<byte[]> ReadData;

        private readonly byte[] _delimiter = { 2, 3, 5, 8, 4 };

        public void SendData(byte[] buffer)
        {
            buffer = buffer.Concat(_delimiter).ToArray();

            Stream.Write(buffer, 0, buffer.Length);
            Debug.WriteLine("sent " + buffer.Length);
        }

        public void SendText(string text)
        {
            SendData(Encoding.ASCII.GetBytes(text));
        }

        protected void Reader()
        {
            Debug.WriteLine("reader started");

            var result = new List<byte>(BufferSize);
            var buffer = new byte[BufferSize];
            while (true)
            {
                var bytesRead = Stream.Read(buffer, 0, BufferSize);
                if (bytesRead == 0)
                    break;
                
                for (var i = 0; i < bytesRead; i++)
                {
                    var b = buffer[i];

                    result.Add(b);
                    if (b == _delimiter.Last() && result.ToArray().Reverse().Take(_delimiter.Length).Reverse().ToArray().SequenceEqual(_delimiter))
                    {
                        result.RemoveRange(result.Count - _delimiter.Length, _delimiter.Length);

                        Debug.WriteLine("tcp got data " + result.Count);
                        ReadData?.Invoke(result.ToArray());
                        result.Clear();
                    }
                }
            }

            Debug.WriteLine("reader ended");
        }
    }

    public class BwTcpClient : BwTcpClientServerModule
    {
        public BwTcpClient(string serverIp, int port)
        {
            var client = new TcpClient(serverIp, port);
            Stream = client.GetStream();
            client.SendBufferSize = client.ReceiveBufferSize = 1 * 1024 * 1024;
            BufferSize = client.ReceiveBufferSize;

            Task.Run(() => Reader());
        }
    }

    public class BwTcpServer : BwTcpClientServerModule
    {
        public BwTcpServer(string serverIp, int port)
        {
            Task.Run(() =>
            {
                var address = IPAddress.Parse(serverIp);
                var listener = new TcpListener(address, port);
                listener.Start();

                while (true)
                {
                    var server = listener.AcceptTcpClient();
                    Stream = server.GetStream();
                    server.SendBufferSize = server.ReceiveBufferSize = 1 * 1024 * 1024;
                    BufferSize = server.ReceiveBufferSize;

                    Task.Run(() => Reader());
                }
            });
        }
    }
}