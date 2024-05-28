using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Web;

namespace codecrafters_bittorrent.src
{
    internal class Command
    {

        public static void DecodeTextAndPrint(string param)
        {
            var encodedValue = new BencodeEncodedString(param);

            var decoded_value = Bencode.Decode(encodedValue, true);
            Console.WriteLine(JsonSerializer.Serialize(decoded_value));
        }

        public static void DecodeFileAndPrintInfo(string filename)
        {
            using var file = File.Open(filename, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);

            var decoded_info = (Dictionary<string, object>)Bencode.Decode(new BencodeEncodedString(file));

            if (decoded_info.TryGetValue("announce", out byte[] tracker_url))
            {
                Console.WriteLine($"Tracker URL: {Encoding.UTF8.GetString(tracker_url)}");
            }
            else
            {
                throw new InvalidOperationException($"\"announce\" field missing in dictionary");
            }
            if (decoded_info.TryGetValue("info", out Dictionary<string, object> dict))
            {
                Console.WriteLine($"Length: {dict["length"]}");

                var hash = Convert.ToHexString(GetInfoHash(dict));
                Console.WriteLine($"Info Hash: {hash.ToLower()}");

                var piece_length = (long)dict["piece length"];
                Console.WriteLine($"Piece Length: {piece_length}");

                Console.WriteLine("Pieces:");
                var byte_array = (byte[])dict["pieces"];
                for (int i = 0; i < byte_array.Length; i += 20) // 20 is hash size
                {
                    Console.WriteLine(Convert.ToHexString(byte_array[i..(i + 20)]).ToLower());
                }
                
            }
            else
            {
                throw new InvalidOperationException($"\"info: length\" field missing in dictionary");
            }
        }

        public static void DecodeFileAndFindPeers(string filename)
        {
            using var file = File.Open(filename, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);

            var decoded_info = (Dictionary<string, object>)Bencode.Decode(new BencodeEncodedString(file));
            var info_dictionary = decoded_info.GetDictionary("info");
            var tracker_url = Encoding.UTF8.GetString(decoded_info.GetValue<byte[]>("announce"));

            var get_params = new Dictionary<string, object>
            {
                { "info_hash", HttpUtility.UrlEncode(GetInfoHash(info_dictionary)) },
                { "peer_id", Guid.NewGuid().ToString().Substring(0,20) },
                { "port", 6881 },
                { "uploaded", 0 },
                { "downloaded", 0 },
                { "left", info_dictionary["length"] },
                { "compact", 1 }
            };

            HttpService service = new HttpService();
            var task = service.GetAsync(tracker_url, get_params);
            task.Wait();

            if (task.IsCompletedSuccessfully)
            {
                var stream = new MemoryStream(task.Result);
                var peers_dict = (Dictionary<string, object>)Bencode.Decode(new BencodeEncodedString(stream));
                var addresses = GetPeers(peers_dict.GetValue<byte[]>("peers"));
                addresses.ForEach(address => Console.WriteLine(address.ToString()));
            }

        }

        public static byte[] GetInfoHash(Dictionary<string, object> dict)
        {
            var memory_stream = new MemoryStream();
            Bencode.Encode(dict, memory_stream);
            var encoded_dict = memory_stream.ToArray();
            return SHA1.HashData(encoded_dict);
        }

        public static List<IPEndPoint> GetPeers(byte[] bytes)
        {
            if (bytes.Length % 6 != 0)
            {
                throw new InvalidOperationException("Bytes array should be divisible by 6");
            }
            var peers = new List<IPEndPoint>();
            for(int i = 0; i < bytes.Length; i += 6)
            {
                var ip = BitConverter.ToInt32(bytes, i);
                var port = BitConverter.ToUInt16(bytes, i + 4);
                peers.Add(new IPEndPoint(ip, port));
            }
            return peers;
        }
    }
}
