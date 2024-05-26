using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

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

            if (decoded_info.TryGetValue("announce", out object? tracker_url))
            {
                Console.WriteLine($"Tracker URL: {tracker_url}");
            }
            else
            {
                throw new InvalidOperationException($"\"announce\" field missing in dictionary");
            }
            if (decoded_info.TryGetValue("info", out object? info_dictionary) &&
                info_dictionary is Dictionary<string, object> dict &&
                dict.TryGetValue("length", out object? length))
            {
                Console.WriteLine($"Length: {length}");

                var memory_stream = new MemoryStream();
                Bencode.Encode(dict, memory_stream);
                var encoded_dict = memory_stream.ToArray();
                var hash = Convert.ToHexString(SHA1.HashData(encoded_dict));
                Console.WriteLine($"Info Hash: {hash}");
            }
            else
            {
                throw new InvalidOperationException($"\"info: length\" field missing in dictionary");
            }
        }
    }
}
