﻿using codecrafters_bittorrent.src.BitTorrent;
using System.Text;
using System.Text.Json;


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
            var addresses = file.FindPeers(Guid.NewGuid().ToString().Substring(0, 20));
            addresses.ForEach(address => Console.WriteLine(address.ToString()));
        }

        public static void HandshakePeer(string filename, string address)
        {
            var file = new TorrentFile(filename);
            var peer_id = Encoding.UTF8.GetBytes("00112233445566778899");
            var endpoint = Util.CreateIPEndPoint(address);
            using var peer = new Peer(endpoint);
            var result = peer.Handshake(file.InfoHash, peer_id);
            Console.WriteLine("Peer ID: " + Convert.ToHexString(result[48..68]).ToLower());
        }

        public static void DownloadPiece(string temp_filename, string torrent_filename, int index)
        {
            var file = new TorrentFile(torrent_filename);
            if (index >= file.PieceHashes.Count || index < 0)
            {
                throw new InvalidOperationException($"Index was out of range for length: {file.PieceHashes.Count}");
            }
            var addresses = file.FindPeers(Guid.NewGuid().ToString().Substring(0, 20));
            var peer_id = Encoding.UTF8.GetBytes("00112233445566778899");
            using var peer = new Peer(addresses[0]);
            _ = peer.PrepareForDownload(file.InfoHash, peer_id);

            var end_index = (int)(file.Length / file.PieceLength);
            if (index < 0 || index > end_index)
            {
                throw new InvalidOperationException($"Index out of range for Piece Count: {file.PieceLength}");
            }

            var piece_size = index == end_index ? file.Length % file.PieceLength : file.PieceLength;
            var piece_bytes = peer.DownloadPieceAsync(new Piece(index, piece_size, file.PieceHashes[index]));
            piece_bytes.Wait();
            File.WriteAllBytes(temp_filename, piece_bytes.Result);
            Console.WriteLine($"Piece {index} downloaded to {temp_filename}");
        }

        public static void DownloadFile(string temp_filename, string torrent_filename)
        {
            var file = new TorrentFile(torrent_filename);
            var torrent_downloader = new TorrentDownload(file);
            DateTime start = DateTime.Now;
            torrent_downloader.DownloadFile(temp_filename);
            TimeSpan timeItTook = DateTime.Now - start;
            Console.WriteLine($"Downloaded {torrent_filename} to {temp_filename} in {timeItTook.TotalSeconds} seconds.");
        }
    }
}
