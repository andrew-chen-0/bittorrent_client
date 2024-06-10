using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace codecrafters_bittorrent.src.BitTorrent
{
    internal class TorrentDownload
    {
        TorrentFile torrentFile;
        List<Peer> peers;



        public TorrentDownload(TorrentFile torrentFile)
        {
            peers = new List<Peer>();
            torrentFile.FindPeers().ForEach(ip => peers.Add(new Peer(ip)));
        }

    }
}
