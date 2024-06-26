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
    /// 1. Handshake (send)
    /// 2. Bitfield message (read)
    /// 3. Interested Message (send)
    /// </summary>
    internal class Peer : IDisposable
    {
        readonly static string PROTOCOL_HEADER = "BitTorrent protocol";

        public readonly static int BLOCK_SIZE = 16 * 1024;

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

        public async Task DeclareInterest()
        {
            await SendPeerMessageAsync(PeerMessageID.INTERESTED, []);
            await ReceivePeerMessageAsync(PeerMessageID.UNCHOKE);
        }

        public async Task<byte[]> ReadBitfieldAsync()
        {
            var bitfield_message = await ReceivePeerMessageAsync(PeerMessageID.BITFIELD);
            
            return bitfield_message[0..bitfield_message.Length];
        }

        public async Task<byte[]> DownloadPieceAsync(int piece_index, long piece_length)
        {
            await ReadBitfieldAsync();
            await DeclareInterest();
            byte[] piece = new byte[piece_length];
            for (int i = 0; i * BLOCK_SIZE < piece_length; i++)
            {
                var begin_offset = i * BLOCK_SIZE;
                var block_size = ((i + 1) * BLOCK_SIZE > piece_length) ? (int)(piece_length % BLOCK_SIZE) : BLOCK_SIZE;
                var piece_block = await DownloadPieceBlockAsync(piece_index, begin_offset, block_size);
                piece_block.CopyTo(piece, begin_offset);
            }
            return piece;
        }

        private async Task<byte[]> DownloadPieceBlockAsync(int piece_index, int begin_offset, int block_size)
        {
            
            byte[] payload = new byte[4 * 3]; // int index, begin, payload

            // Copy to payload
            ConvertToBytes(piece_index).CopyTo(payload, 0);
            ConvertToBytes(begin_offset).CopyTo(payload, 4);
            ConvertToBytes(block_size).CopyTo(payload, 8);

            await SendPeerMessageAsync(PeerMessageID.REQUEST, payload);
            Thread.Sleep(1000);
            var piece_message = await ReceivePeerMessageAsync(PeerMessageID.PIECE);
            if (piece_message != null) {
                if (ConvertToInt(piece_message[0..4]) != piece_index)
                {
                    throw new InvalidOperationException($"Responding piece index was not same as requested piece index");
                }

                if (ConvertToInt(piece_message[4..8]) != begin_offset)
                {
                    throw new InvalidOperationException($"Responding offset was not same as requested offset");
                }

                if (piece_message.Length - 8 != block_size)
                {
                    throw new InvalidOperationException($"Message Length was expected to be {block_size} bytes but got {piece_message.Length - 8} bytes");
                }
                
                return piece_message[8..piece_message.Length];
            }
            throw new InvalidOperationException("Failed to receive piece message");
        }

        // 1. Length of payload (4 bytes)
        // 2. Message ID (1 byte)
        // 3. Payload (Variable bytes)
        private async Task SendPeerMessageAsync(PeerMessageID messageID, byte[] payload)
        {
            if (!is_initialized)
            {
                throw new InvalidOperationException("Peer has not been handshaked");
            }
            byte[] request = new byte[payload.Length + 5];
            var length = ConvertToBytes(payload.Length + 1);
            if (length.Length != 4)
            {
                throw new InvalidOperationException("Byte array length should be 4");
            }
            length.CopyTo(request, 0);
            request[4] = (byte)messageID;
            payload.CopyTo(request, 5);
            await client.SendAsync(request, SocketFlags.None);
        }

        private async Task<byte[]> ReceivePeerMessageAsync(PeerMessageID expectedMessageID)
        {
            var data_buffer = new byte[5];
            _ = await client.ReceiveAsync(data_buffer, SocketFlags.None);
            int length = ConvertToInt(data_buffer[0..4]);
            int messageID = data_buffer[4];
            if (messageID != (int)expectedMessageID)
            {
                throw new InvalidOperationException($"Expected message id: {expectedMessageID} got {messageID}");
            }


            byte[] buffer = [];
            if (length - 1 > 0)
            {
                buffer = new byte[length - 1];
                _ = await client.ReceiveAsync(buffer, SocketFlags.None);
            }

            return buffer;
        }

        // Integers have to be Big Endian
        private byte[] ConvertToBytes(int value)
        {
            int intValue;
            byte[] intBytes = BitConverter.GetBytes(value);
            if (BitConverter.IsLittleEndian)
                Array.Reverse(intBytes);

            return intBytes;
        }

        private int ConvertToInt(byte[] bytes)
        {
            if (bytes.Length != 4)
            {
                throw new InvalidOperationException("Bytes has to be 4 to become int32");
            }
            if (BitConverter.IsLittleEndian)
                Array.Reverse(bytes);
            return BitConverter.ToInt32(bytes);
        }

        public void Dispose()
        {
            client.Dispose();
        }
    }
}
