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
            var decoded_info = BitTorrent.OpenTorrentFile(filename);

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

                var hash = Convert.ToHexString(BitTorrent.GetInfoHash(dict));
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
            var decoded_info = BitTorrent.OpenTorrentFile(filename);
            var info_dictionary = decoded_info.GetDictionary("info");
            var tracker_url = Encoding.UTF8.GetString(decoded_info.GetValue<byte[]>("announce"));

            var addresses = BitTorrent.FindPeers(tracker_url, info_dictionary);
            addresses.ForEach(address => Console.WriteLine(address.ToString()));
        }

        public static void HandshakePeer(string filename, string address)
        {
            var decoded_info = BitTorrent.OpenTorrentFile(filename);
            var info_dictionary = decoded_info.GetDictionary("info");
            var info_hash = BitTorrent.GetInfoHash(info_dictionary);
            var peer_id = Encoding.UTF8.GetBytes("00112233445566778899");

            var endpoint = Util.CreateIPEndPoint(address);
            using var peer = new Peer(endpoint);
            var result = peer.HandshakeAsync(info_hash, peer_id);
            result.Wait();
            Console.WriteLine("Peer ID:" + Convert.ToHexString(result.Result[48..68]).ToLower());
        }
    }
}
