using System.Collections.Concurrent;
using System.Text;
using static System.Security.Cryptography.SHA1;

namespace codecrafters_bittorrent.src.BitTorrent
{
    internal class TorrentDownload
    {
        TorrentFile file;
        List<Peer> peers;
        ConcurrentQueue<Piece> pieces_to_download;
        List<Piece> all_pieces;
        string peer_id;

        public TorrentDownload(TorrentFile torrentFile)
        {
            peers = new List<Peer>();
            peer_id = Guid.NewGuid().ToString().Substring(0, 20);
            file = torrentFile;
            file.FindPeers(peer_id).ForEach(ip => peers.Add(new Peer(ip)));
            pieces_to_download = new ConcurrentQueue<Piece>();
            all_pieces = new List<Piece>();
            var piece_hashes = torrentFile.PieceHashes;
            for (int i = 0; i < piece_hashes.Count; i++)
            {
                var piece_size = i == piece_hashes.Count - 1 ? file.Length % file.PieceLength : file.PieceLength;
                var piece = new Piece(i, piece_size, piece_hashes[i]);
                pieces_to_download.Enqueue(piece);
                all_pieces.Add(piece);
            }
        }

        public void DownloadFile(string filename)
        {
            var initializing_peers = peers.Select(p => p.PrepareForDownload(file.InfoHash, Encoding.UTF8.GetBytes(peer_id))).ToArray();
            Task.WaitAll(initializing_peers);

            var remove_peers = new List<Peer>();
            for (int i = 0; i < initializing_peers.Length; i++)
            {
                if (!initializing_peers[i].IsCompletedSuccessfully)
                {
                    remove_peers.Add(peers[i]);
                }
            }
            peers = peers.Except(remove_peers).ToList();

            var downloading_peers = peers.Select(peer => peer.DownloadFile(pieces_to_download)).ToArray();
            Task.WaitAll(downloading_peers);
            byte[] file_bytes = new byte[file.Length];
            all_pieces.ForEach(piece =>
            {
                piece.Data.CopyTo(file_bytes, piece.Index * file.PieceLength);
            });
            File.WriteAllBytes(filename, file_bytes);
        }
    }
}
