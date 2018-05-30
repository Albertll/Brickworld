using System.IO;
using System.IO.Compression;
using System.Runtime.Serialization.Formatters.Binary;

namespace Assets.Scripts
{
    public static class Serializer
    {
        public static byte[] Serialize<T>(this T obj)
        {
            using (var stream = new MemoryStream())
            {
                var bformatter = new BinaryFormatter();

                bformatter.Serialize(stream, obj);

                return Compress(stream.GetBuffer());
            }
        }

        public static T Deserialize<T>(this byte[] data)
        {
            using (var stream = new MemoryStream(Decompress(data)))
            {
                var bformatter = new BinaryFormatter();

                return (T)bformatter.Deserialize(stream);
            }
        }

        public static byte[] Compress(byte[] data)
        {
            MemoryStream output = new MemoryStream();
            using (DeflateStream dstream = new DeflateStream(output, CompressionLevel.Optimal))
            {
                dstream.Write(data, 0, data.Length);
            }
            return output.ToArray();
        }

        public static byte[] Decompress(byte[] data)
        {
            MemoryStream input = new MemoryStream(data);
            MemoryStream output = new MemoryStream();
            using (DeflateStream dstream = new DeflateStream(input, CompressionMode.Decompress))
            {
                dstream.CopyTo(output);
            }
            return output.ToArray();
        }

        public static void SerializeToFile<T>(T obj, string filePath)
        {
            using (var stream = File.Open(filePath, FileMode.Create))
            {
                var bformatter = new BinaryFormatter();

                bformatter.Serialize(stream, obj);
                stream.Close();
            }
        }

        public static T DeserializeFromFile<T>(string filePath)
        {
            using (var stream = File.Open(filePath, FileMode.Open))
            {
                var bformatter = new BinaryFormatter();

                return (T)bformatter.Deserialize(stream);
            }
        }
    }
}