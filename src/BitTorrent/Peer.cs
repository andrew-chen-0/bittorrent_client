using codecrafters_bittorrent.src.BitTorrent;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;
using static System.Security.Cryptography.SHA1;

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
        TcpClient tcpClient;
        NetworkStream client;

        bool is_initialized = false;

        bool[] has_pieces = [];

        public Peer(IPEndPoint address)
        {
            this.address = address;
            tcpClient = new TcpClient();
        }

        public bool IsInitialized => is_initialized;

        public async Task PrepareForDownload(byte[] info_hash, byte[] peer_hash)
        {
            Handshake(info_hash, peer_hash);
            await ReadBitfieldAsync();
            await DeclareInterest();
        }

        // 1. byte length of Protocol header (one byte)
        // 2. Protocol Header string (17 bytes)
        // 3. Info hash of file (20 bytes)
        // 4. Our peer hash (20 bytes)
        public byte[] Handshake(byte[] info_hash, byte[] peer_hash)
        {
            try
            {
                byte[] request = new byte[68];
                request[0] = (byte)PROTOCOL_HEADER.Length;
                byte[] string_in_bytes = Encoding.ASCII.GetBytes(PROTOCOL_HEADER);
                string_in_bytes.CopyTo(request, 1);
                info_hash.CopyTo(request, 28);
                peer_hash.CopyTo(request, 48);

                tcpClient.Connect(address);
                client = tcpClient.GetStream();
                client.Write(request);

                var buffer = new byte[68];
                client.Read(buffer);
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
            has_pieces = new bool[bitfield_message.Length * 8];
            for (int i = 0; i< bitfield_message.Length * 8; i++)
            {
                var byte_index = i % 8;
                var index = i / 8;
                has_pieces[i] = CheckBooleanAtPoint(bitfield_message[index], byte_index);
            }
            return bitfield_message;
        }

        /// <summary>
        /// Download file through from concurrent Queue of files
        /// </summary>
        /// <param name="pieces_queue"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        public async Task DownloadFile(ConcurrentQueue<Piece> pieces_queue)
        {
            while (pieces_queue.TryDequeue(out var piece))
            {
                try
                {
                    if (!has_pieces[piece.Index])
                    {
                        pieces_queue.Enqueue(piece);
                        continue;
                    }
                    var result = await DownloadPieceAsync(piece);
                    piece.RecieveData(result);
                }
                catch (Exception)
                {
                    piece.AttemptedDownload();
                    pieces_queue.Enqueue(piece);
                } finally
                {
                    if (piece.Attempts >= Piece.MAX_RETRIES)
                    {
                        throw new InvalidOperationException("Piece failed Max Retries");
                    }
                }
            }
        }

        public async Task<byte[]> DownloadPieceAsync(Piece piece_obj)
        {
            byte[] piece = new byte[piece_obj.Length];
            for (int i = 0; i * BLOCK_SIZE < piece_obj.Length; i++)
            {
                var begin_offset = i * BLOCK_SIZE;
                var block_size = ((i + 1) * BLOCK_SIZE > piece_obj.Length) ? (int)(piece_obj.Length % BLOCK_SIZE) : BLOCK_SIZE;
                var piece_block = await DownloadPieceBlockAsync(piece_obj.Index, begin_offset, block_size);
                piece_block.CopyTo(piece, begin_offset);
            }
            if (piece_obj.Hash == HashData(piece))
            {
                throw new InvalidOperationException($"Piece hash did not match {Convert.ToHexString(piece_obj.Hash)}");
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
            var piece_message = await ReceivePeerMessageAsync(PeerMessageID.PIECE);
            if (piece_message != null) {
                var index = ConvertToInt(piece_message[0..4]);
                if (index != piece_index)
                {
                    throw new InvalidOperationException($"Responding piece index was not same as requested piece index");
                }

                var offset = ConvertToInt(piece_message[4..8]);
                if (offset != begin_offset)
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
            await client.WriteAsync(request);
        }

        private async Task<byte[]> ReceivePeerMessageAsync(PeerMessageID expectedMessageID)
        {
            var data_buffer = new byte[5];
            await client.ReadExactlyAsync(data_buffer);
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
                await client.ReadExactlyAsync(buffer);
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

        private bool CheckBooleanAtPoint(byte bit_array, int index)
        {
            if (index < 0 && index > 7)
            {
                throw new IndexOutOfRangeException("Index was out of range of a byte array");
            }
            int max_int = 2;
            max_int = max_int << 6;
            for(int i = 1; i <= index; i++)
            {
                max_int = max_int >> 1;
            }
            return (bit_array & max_int) == max_int;
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
            tcpClient.Dispose();
            client.Dispose();
        }
    }
}
