﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Web;


namespace codecrafters_bittorrent
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
            var file = new TorrentFile(filename);
            Console.WriteLine($"Tracker URL: {file.TrackerURL}");
            Console.WriteLine($"Length: {file.Length}");

            var hash = Convert.ToHexString(file.InfoHash);
            Console.WriteLine($"Info Hash: {hash.ToLower()}");
            Console.WriteLine($"Piece Length: {file.PieceLength}");
            Console.WriteLine("Pieces Hashes:");
            file.PieceHashes.ForEach(hash => Console.WriteLine(Convert.ToHexString(hash).ToLower()));      
        }

        public static void DecodeFileAndFindPeers(string filename)
        {
            var file = new TorrentFile(filename);
            var addresses = file.FindPeers();
            addresses.ForEach(address => Console.WriteLine(address.ToString()));
        }

        public static void HandshakePeer(string filename, string address)
        {
            var file = new TorrentFile(filename);
            var peer_id = Encoding.UTF8.GetBytes("00112233445566778899");
            var endpoint = Util.CreateIPEndPoint(address);
            using var peer = new Peer(endpoint);
            var result = peer.HandshakeAsync(file.InfoHash, peer_id);
            result.Wait();
            Console.WriteLine("Peer ID: " + Convert.ToHexString(result.Result[48..68]).ToLower());
        }

        public static void DownloadPiece(string temp_filename, string torrent_filename, int index)
        {
            var file = new TorrentFile(torrent_filename);
            if (index >= file.PieceHashes.Count || index < 0)
            {
                throw new InvalidOperationException($"Index was out of range for length: {file.PieceHashes.Count}");
            }
            var addresses = file.FindPeers();
            var peer_id = Encoding.UTF8.GetBytes("00112233445566778899");
            using var peer = new Peer(addresses[0]);
            var result = peer.HandshakeAsync(file.InfoHash, peer_id);
            result.Wait();

            var end_index = (int)(file.Length / file.PieceLength);
            if (index < 0 || index > end_index)
            {
                throw new InvalidOperationException($"Index out of range for Piece Count: {file.PieceLength}");
            }

            var piece_size = index == end_index ? file.Length % file.PieceLength : file.PieceLength;
            var piece_bytes = peer.DownloadPieceAsync(index, piece_size);
            piece_bytes.Wait();

            Console.WriteLine(Convert.ToHexString(file.PieceHashes[0]));
            Console.WriteLine(Convert.ToHexString(System.Security.Cryptography.SHA1.HashData(piece_bytes.Result)));
            File.WriteAllBytes(temp_filename, piece_bytes.Result);
            Console.WriteLine($"Piece {index} downloaded to {temp_filename}");
        }
    }
}
