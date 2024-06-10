using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace codecrafters_bittorrent
{
    internal class TorrentFile
    {
        Dictionary<string, object> metadata;

        public TorrentFile(string filename)
        {
            metadata = OpenTorrentFile(filename);
        }
        private Dictionary<string, object> InfoDictionary => metadata.GetDictionary("info");

        public string TrackerURL => Encoding.UTF8.GetString(metadata.GetValue<byte[]>("announce"));
        public long Length => InfoDictionary.GetValue<long>("length");
        public long PieceLength => InfoDictionary.GetValue<long>("piece length");

        private List<byte[]> pieceHashes = new List<byte[]>();
        public List<byte[]> PieceHashes
        {
            get
            {
                if (pieceHashes.Count == 0)
                {
                    var piece_bytes = InfoDictionary.GetValue<byte[]>("pieces");
                    for (int i = 0; i < piece_bytes.Length; i += 20) // 20 is hash size
                    {
                        pieceHashes.Add(piece_bytes[i..(i + 20)]);
                    }
                }
                
                return pieceHashes;
            }
        }

        private byte[]? infoHash;
        public byte[] InfoHash
        {
            get
            {
                if(infoHash == null)
                {
                    infoHash = GetInfoHash();
                }
                return infoHash;
            }
        }

        private Dictionary<string, object> OpenTorrentFile(string filename)
        {
            using var file = File.Open(filename, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
            return (Dictionary<string, object>)Bencode.Decode(new BencodeEncodedString(file));
        }

        private byte[] GetInfoHash()
        {
            var dict = metadata.GetDictionary("info");
            var memory_stream = new MemoryStream();
            Bencode.Encode(dict, memory_stream);
            var encoded_dict = memory_stream.ToArray();
            return System.Security.Cryptography.SHA1.HashData(encoded_dict);
        }

        

        public List<IPEndPoint> FindPeers()
        {
            var get_params = new Dictionary<string, object>
            {
                { "info_hash", HttpUtility.UrlEncode(GetInfoHash()) },
                { "peer_id", Guid.NewGuid().ToString().Substring(0,20) },
                { "port", 6881 },
                { "uploaded", 0 },
                { "downloaded", 0 },
                { "left", metadata["length"] },
                { "compact", 1 }
            };

            HttpService service = new HttpService();
            var task = service.GetAsync(TrackerURL, get_params);
            task.Wait();

            if (task.IsCompletedSuccessfully)
            {
                var stream = new MemoryStream(task.Result);
                var peers_dict = (Dictionary<string, object>)Bencode.Decode(new BencodeEncodedString(stream));
                return InterpretPeers(peers_dict.GetValue<byte[]>("peers"));
            }
            throw new InvalidOperationException("Find peers failed");
        }

        private List<IPEndPoint> InterpretPeers(byte[] bytes)
        {
            if (bytes.Length % 6 != 0)
            {
                throw new InvalidOperationException("Bytes array should be divisible by 6");
            }
            var peers = new List<IPEndPoint>();
            for (int i = 0; i < bytes.Length; i += 6)
            {
                var ip = new IPAddress(bytes[i..(i + 4)]);
                var port = (bytes[i + 4] << 8) + bytes[i + 5];
                peers.Add(new IPEndPoint(ip, port));
            }
            return peers;
        }
    }
}
