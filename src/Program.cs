#nullable enable
using codecrafters_bittorrent.src;
using System.Collections.Generic;
using System.Numerics;
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
else
{
    throw new InvalidOperationException($"Invalid command: {command}");
}
