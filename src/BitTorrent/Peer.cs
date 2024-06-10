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

    /// <summary>
    /// Peer Connection Process
    /// 1. Handshake
    /// 2. Bitfield message
    /// 3. Interested Message
    /// </summary>
    internal class Peer : IDisposable
    {
        readonly static string PROTOCOL_HEADER = "BitTorrent protocol";

        IPEndPoint address;
        Socket client;

        bool is_initialized = false;

        public Peer(IPEndPoint address)
        {
            this.address = address;
            client = new Socket(address.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
        }

        public bool IsInitialized => is_initialized;

        // 1. byte length of Protocol header (one byte)
        // 2. Protocol Header string (17 bytes)
        // 3. Info hash of file (20 bytes)
        // 4. Our peer hash (20 bytes)
        public async Task<byte[]> HandshakeAsync(byte[] info_hash, byte[] peer_hash)
        {
            try
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
                is_initialized = true;

                return buffer;
            } catch (Exception ignored)
            {
                // Ignore Exception
            }

            return new byte[0];
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

        public async Task<byte[]> DownloadPieceAsync(int index, int block_length)
        {
            int begin_offset = index * 16 * 1024;
            byte[] payload = new byte[4 * 3]; // int index, begin, payload

            // Copy to payload
            BitConverter.GetBytes(index).CopyTo(payload, 0);
            BitConverter.GetBytes(begin_offset).CopyTo(payload, 4);
            BitConverter.GetBytes(block_length).CopyTo(payload, 8);

            await SendPeerMessageAsync(PeerMessageID.REQUEST, payload);
            var piece_message = await RecievePeerMessageAsync(block_length + 5);
            if (piece_message != null) {
                int length = BitConverter.ToInt32(piece_message[0..4]);
                int message = (int)piece_message[4];
                if (length != block_length + 5)
                {
                    throw new InvalidOperationException($"Message Length was expected to be {block_length + 5} but got {length}");
                }
                if (message != (int)PeerMessageID.PIECE)
                {
                    throw new InvalidOperationException($"Message ID was not Piece");
                }
                return piece_message[5..piece_message.Length];
            }
            throw new InvalidOperationException("Failed to receive piece message");
        }

        // 1. Length of message (4 bytes)
        // 2. Message ID (1 byte)
        // 3. Payload (Variable bytes)
        private async Task SendPeerMessageAsync(PeerMessageID messageID, byte[] payload)
        {
            if (!is_initialized)
            {
                throw new InvalidOperationException("Peer has not been handshaked");
            }
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
