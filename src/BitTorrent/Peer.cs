using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace codecrafters_bittorrent
{
    internal enum PeerMessageID
    {
        CHOKE = 0,
        UNCHOKE = 1,
        INTERESTED = 2,
        NOT_INTERESTED = 3,
        HAVE = 4,
        BITFIELD = 5,
        REQUEST = 6,
        PIECE = 7,
        CANCEL = 8
    }

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

        // 1. byte length of Protocol header (one byte)
        // 2. Protocol Header string (17 bytes)
        // 3. Info hash of file (20 bytes)
        // 4. Our peer hash (20 bytes)
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

        public async Task<bool> DeclareInterest()
        {
            await SendPeerMessageAsync(PeerMessageID.INTERESTED, []);
            var task = RecievePeerMessageAsync(5);
            if (await Task.WhenAny(task, Task.Delay(1000)) == task)
            {
                // task completed within timeout
                return (int)task.Result[5] == (int)PeerMessageID.UNCHOKE;
            }
            else
            {
                // timeout logic
                return false;
            }
        }

        // 1. Length of message (4 bytes)
        // 2. Message ID (1 byte)
        // 3. Payload (Variable bytes)
        private async Task SendPeerMessageAsync(PeerMessageID messageID, byte[] payload)
        {
            byte[] request = new byte[payload.Length + 5];
            var length = BitConverter.GetBytes(payload.Length + 5);
            if (length.Length != 4)
            {
                throw new InvalidOperationException("Byte array length should be 4");
            }
            request[5] = (byte)messageID;
            payload.CopyTo(request, 6);
            await client.SendAsync(request, SocketFlags.None);

        }

        private async Task<byte[]> RecievePeerMessageAsync(int expected_size = 1024)
        {
            var buffer = new byte[expected_size];
            _ = await client.ReceiveAsync(buffer, SocketFlags.None);
            return buffer;
        }

        public void Dispose()
        {
            client.Dispose();
        }
    }
}
