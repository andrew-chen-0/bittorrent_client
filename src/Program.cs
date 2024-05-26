#nullable enable
using codecrafters_bittorrent.src;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

// Parse arguments
var (command, param) = args.Length switch
{
    0 => throw new InvalidOperationException("Usage: your_bittorrent.sh <command> <param>"),
    1 => throw new InvalidOperationException("Usage: your_bittorrent.sh <command> <param>"),
    _ => (args[0], args[1])
};

// Parse command and act accordingly
if (command == "decode")
{
    if (param == null)
    {
        throw new InvalidOperationException("Provide encoded value");
    }
    var encodedValue = new BencodeEncodedString(param);

    var decoded_value = Bencode.Decode(encodedValue);
    Console.WriteLine(JsonSerializer.Serialize(decoded_value));
}
else if (command == "info")
{
    if (param == null)
    {
        throw new InvalidOperationException("Provide filename");
    }

    using var file = File.Open(param, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);

    var decoded_info = (Dictionary<string, object>) Bencode.Decode(new BencodeEncodedString(file));
    
    if (decoded_info.TryGetValue("announce", out object? tracker_url)) 
    {
        Console.WriteLine($"Tracker URL: {tracker_url}");
    } else
    {
        throw new InvalidOperationException($"\"announce\" field missing in dictionary");
    }
    if (decoded_info.TryGetValue("info", out object? info_dictionary) && 
        info_dictionary is Dictionary<string, object> dict && 
        dict.TryGetValue("length", out object? length))
    {
        Console.WriteLine($"Length: {length}");
        var encoded_dictionary = Bencode.Encode(dict);
        var hash = Convert.ToHexString(SHA1.HashData(Encoding.UTF8.GetBytes(encoded_dictionary)));
        Console.WriteLine($"Info Hash: {hash}");
    }
    else
    {
        throw new InvalidOperationException($"\"info: length\" field missing in dictionary");
    }
}
else
{
    throw new InvalidOperationException($"Invalid command: {command}");
}
