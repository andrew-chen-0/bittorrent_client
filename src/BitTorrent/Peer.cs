using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace codecrafters_bittorrent.src
{
    internal class Peer : IDisposable
    {
        readonly static string PROTOCOL_HEADER = "BitTorrent protocol";

        IPEndPoint address;
        Socket client;

        public Peer(IPEndPoint address)
        {
            this.address = address;
            client = new Socket(address.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
        }


        public async Task<byte[]> HandshakeAsync(byte[] info_hash, byte[] peer_hash)
        {
            byte[] request = new byte[68];
            request[0] = (byte)PROTOCOL_HEADER.Length;
            byte[] string_in_bytes = Encoding.ASCII.GetBytes(PROTOCOL_HEADER);
            string_in_bytes.CopyTo(request, 1);
            info_hash.CopyTo(request, 28);
            peer_hash.CopyTo(request, 48);

            await client.ConnectAsync(address);
            await client.SendAsync(request, SocketFlags.None);

            var buffer = new byte[68];
            _ = await client.ReceiveAsync(buffer, SocketFlags.None);

            return buffer;

        }

        public void Dispose()
        {
            client.Dispose();
        }
    }
}
