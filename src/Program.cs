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
    Command.DecodeTextAndPrint(param);
}
else if (command == "info")
{
    if (param == null)
    {
        throw new InvalidOperationException("Provide filename");
    }
    Command.DecodeFileAndPrintInfo(param);
}
else if (command == "peers")
{
    if (param == null)
    {
        throw new InvalidOperationException("Provide filename");
    }
    Command.DecodeFileAndFindPeers(param);
}
else if (command == "handshake")
{
    if (param == null || args[2] == null)
    {
        throw new InvalidOperationException("Provide filename and peer address");
    }
    Command.HandshakePeer(param, args[2]);
}
else
{
    throw new InvalidOperationException($"Invalid command: {command}");
}
