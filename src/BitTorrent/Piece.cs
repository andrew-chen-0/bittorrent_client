using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace codecrafters_bittorrent.src.BitTorrent
{
    internal class Piece
    {
        public static readonly int MAX_RETRIES = 3;

        private int index;
        private long length;
        private byte[] hash;
        private byte[] data;

        public Piece(int index, long length, byte[] hash)
        {
            this.index = index;
            this.length = length;
            this.hash = hash;
            data = [];
        }

        public int Attempts { get; private set; } = 0;

        public bool IsReceived => data.Length == length;
        public long Length => length;
        public byte[] Hash => hash;
        public int Index => index;
        public byte[] Data => data;


        public void AttemptedDownload() { Attempts++; }
        public void RecieveData(byte[] data)
        {
            if (length == data.Length)
            {
                this.data = data;
                return;
            }
            throw new InvalidOperationException("Output length is not as expected");
        }
    }
}
