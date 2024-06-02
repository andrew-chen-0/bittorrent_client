using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace codecrafters_bittorrent.src
{
    internal static class BitTorrent
    {

        public static Dictionary<string, object> OpenTorrentFile(string filename) {
            using var file = File.Open(filename, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
            return (Dictionary<string, object>)Bencode.Decode(new BencodeEncodedString(file));
        }

        public static List<IPEndPoint> FindPeers(string tracker_url, Dictionary<string, object> info_dictionary)
        {
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
                return GetPeers(peers_dict.GetValue<byte[]>("peers"));
            }
            throw new InvalidOperationException("Find peers failed");
        }

        public static byte[] GetInfoHash(Dictionary<string, object> dict)
        {
            var memory_stream = new MemoryStream();
            Bencode.Encode(dict, memory_stream);
            var encoded_dict = memory_stream.ToArray();
            return System.Security.Cryptography.SHA1.HashData(encoded_dict);
        }

        public static List<IPEndPoint> GetPeers(byte[] bytes)
        {
            if (bytes.Length % 6 != 0)
            {
                throw new InvalidOperationException("Bytes array should be divisible by 6");
            }
            var peers = new List<IPEndPoint>();
            for (int i = 0; i < bytes.Length; i += 6)
            {
                var ip = new IPAddress(bytes[i..(i + 4)]);
                var port = ((int)bytes[i + 4] << 8) + (int)bytes[i + 5];
                peers.Add(new IPEndPoint(ip, port));
            }
            return peers;
        }
    }
}
