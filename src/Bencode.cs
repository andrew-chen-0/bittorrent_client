﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace codecrafters_bittorrent.src
{

    internal static class Bencode
    {
        public static object Decode(BencodeEncodedString encodedValue)
        {
            switch(encodedValue.CurrentChar)
            {
                case '0':
                case '1':
                case '2':
                case '3':
                case '4':
                case '5':
                case '6':
                case '7':
                case '8':
                case '9':
                    return DecodeString(encodedValue);
                case 'i':
                    return DecodeInteger(encodedValue);
                case 'l':
                    return DecodeList(encodedValue);
                case 'd':
                    return DecodeDictionary(encodedValue);
                default:
                    throw new InvalidOperationException("Unhandled encoded value: " + encodedValue);
            }

        }
        
        // Example: "5:hello" -> "hello"
        private static string DecodeString(BencodeEncodedString encodedValue)
        {
            var str_length = ParseInteger(encodedValue);
            if (encodedValue.ReadNextChar() != ':')
            {
                throw new InvalidOperationException("Invalid encoded value: " + encodedValue);
            }

            var result_bytes = encodedValue.ReadNextNBytes((int) str_length);
            return Encoding.UTF8.GetString(result_bytes);
        }


        // Example: "i52e" -> "52"
        private static long DecodeInteger(BencodeEncodedString encodedValue)
        {
            encodedValue.ReadNextChar();
            var decoded_int = ParseInteger(encodedValue);

            if (encodedValue.ReadNextChar() != 'e')
            {
                throw new InvalidOperationException("Invalid encoded value: " + encodedValue);
            }

            return decoded_int;
        }


        // Example: "le" -> [] "l5:helloi5ee" -> ["hello", "5"]
        private static List<object> DecodeList(BencodeEncodedString encodedValue)
        {
            var decoded_list = new List<object>();
            encodedValue.ReadNextChar(); // Clears 'l' character
            while (encodedValue.CurrentChar != 'e')
            {
                decoded_list.Add(Decode(encodedValue));
            }
            encodedValue.ReadNextChar(); // Clears 'e' character
            return decoded_list;
        }

        // Example: "d3:foo3:bar5:helloi52ee" -> {"hello": 52, "foo":"bar"}
        private static Dictionary<string, object> DecodeDictionary(BencodeEncodedString encodedValue)
        {
            var decoded_dictionary = new Dictionary<string, object>();
            
            encodedValue.ReadNextChar();
            while (encodedValue.CurrentChar != 'e')
            {
                var key = DecodeString(encodedValue);
                var value = Decode(encodedValue);
                if (key is not string)
                {
                    throw new InvalidOperationException("Invalid encoded value: " + encodedValue);
                }
                decoded_dictionary.Add(key, value);
            }
            if (encodedValue.ReadNextChar() != 'e')
            {
                throw new InvalidOperationException("Invalid encoded value: " + encodedValue);
            }
            return decoded_dictionary;
        }

        private static long ParseInteger(BencodeEncodedString encodedValue)
        {
            int idx = 0;
            char[] char_str_length = new char[19];
            while (Char.IsDigit(encodedValue.CurrentChar) || encodedValue.CurrentChar == '-')
            {
                char_str_length[idx] = encodedValue.ReadNextChar();
                idx++;
            }
            return long.Parse(char_str_length);
        }

        public static string Encode(object value)
        {
            if (value is string s)
            {
                return EncodeString(s);
            }
            else if (value is long l)
            {
                return EncodeInteger(l);
            }
            else if (value is List<object> list)
            {
                return EncodeList(list);
            }
            else if(value is Dictionary<string, object> dict)
            {
                return EncodeDictionary(dict);
            }
            else
            {
                throw new InvalidOperationException("Unexpected implelemented type for encoding");
            }
        }

        private static string EncodeString(string value) => $"{value.Length}:{value}";
        private static string EncodeInteger(long value) => $"i{value}e";
        private static string EncodeList(List<object> list)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("l");
            foreach (object item in list)
            {
                sb.Append(Encode(item));
            }
            sb.Append("e");
            return sb.ToString();     
        }

        private static string EncodeDictionary(Dictionary<string, object> dict)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("d");
            foreach(KeyValuePair<string, object> pair in dict)
            {
                sb.Append(Encode(pair.Key));
                sb.Append(Encode(pair.Value));
            }
            sb.Append("e");
            return sb.ToString();
        }
    }

    internal class BencodeEncodedString
    {
        Stream inputStream;

        public BencodeEncodedString(string input_string)
        {
            var bytes = Encoding.UTF8.GetBytes(input_string);
            inputStream = new MemoryStream(bytes);
        }

        public BencodeEncodedString(Stream input_stream)
        {
            this.inputStream = input_stream;
        }

        private void CheckBounds(int offset)
        {
            if (inputStream.CanSeek && inputStream.Position + offset >= inputStream.Length)
            {
                throw new IndexOutOfRangeException($"THe specified index is {inputStream.Position + offset} >= {inputStream.Length}");
            }
        }

        public Char ReadCurrentChar()
        {
            var current_char = ReadNextChar();
            inputStream.Position--;
            return current_char;
        }

        public Char ReadNextChar()
        {
            CheckBounds(0);
            var read_byte = new byte[1];
            inputStream.Read(read_byte, 0, 1);
            return (char) read_byte[0];
        }

        public byte[] ReadNextNBytes(int n)
        {
            CheckBounds(n - 1);
            var bytes = new byte[n];
            inputStream.Read(bytes, 0, n);
            return bytes;
        }

        public Char CurrentChar => ReadCurrentChar();
    }
}
