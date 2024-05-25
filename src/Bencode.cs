using System;
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

        private static bool IsEncodedString(BencodeEncodedString encodedValue) => Char.IsDigit(encodedValue.CurrentChar);
        
        // Example: "5:hello" -> "hello"
        private static string DecodeString(BencodeEncodedString encodedValue)
        {
            if (!IsEncodedString(encodedValue))
            {
                throw new InvalidOperationException("Invalid encoded value: " + encodedValue);
            }

            var str_length = ParseInteger(encodedValue);
            if (encodedValue.GetChar() != ':')
            {
                throw new InvalidOperationException("Invalid encoded value: " + encodedValue);
            }

            return encodedValue.GetNextNChars((int) str_length);
        }


        private static bool IsEncodedInteger(BencodeEncodedString encodedValue) => encodedValue.CurrentChar == 'i';

        // Example: "i52e" -> "52"
        private static long DecodeInteger(BencodeEncodedString encodedValue)
        {
            if (!IsEncodedInteger(encodedValue))
            {
                throw new InvalidOperationException("Invalid encoded value: " + encodedValue);
            }
            encodedValue.GetChar(); // Clears 'i' character
            var decoded_int = ParseInteger(encodedValue);

            if (encodedValue.GetChar() != 'e')
            {
                throw new InvalidOperationException("Invalid encoded value: " + encodedValue);
            }

            return decoded_int;
        }

        private static bool IsEncodedList(BencodeEncodedString encodedValue) => encodedValue.CurrentChar == 'l';

        // Example: "le" -> [] "l5:helloi5ee" -> ["hello", "5"]
        private static List<object> DecodeList(BencodeEncodedString encodedValue)
        {
            var decoded_list = new List<object>();
            if (IsEncodedList(encodedValue))
            {
                encodedValue.GetChar(); // Clears 'l' character
                while (encodedValue.CurrentChar != 'e')
                {
                    decoded_list.Add(Decode(encodedValue));
                }
                encodedValue.GetChar(); // Clears 'e' character
            }
            return decoded_list;
        }

        private static bool IsEncodedDictionary(BencodeEncodedString encodedValue) => encodedValue.CurrentChar == 'd';

        // Example: "d3:foo3:bar5:helloi52ee" -> {"hello": 52, "foo":"bar"}
        public static Dictionary<string, object> DecodeDictionary(BencodeEncodedString encodedValue)
        {
            var decoded_dictionary = new Dictionary<string, object>();
            if (IsEncodedDictionary(encodedValue))
            {
                encodedValue.GetChar(); // Clears 'd' character'
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
                if (encodedValue.GetChar() != 'e')
                {
                    throw new InvalidOperationException("Invalid encoded value: " + encodedValue);
                }
            }
            return decoded_dictionary;
        }

        private static long ParseInteger(BencodeEncodedString encodedValue)
        {
            int idx = 0;
            char[] char_str_length = new char[encodedValue.Length];
            while (Char.IsDigit(encodedValue.CurrentChar) || encodedValue.CurrentChar == '-')
            {
                char_str_length[idx] = encodedValue.GetChar();
                idx++;
            }
            return long.Parse(char_str_length);
        }
    }

    internal class BencodeEncodedString
    {
        string encodedString;
        int idx = 0;

        public BencodeEncodedString(string encodedString)
        {
            this.encodedString = encodedString;
        }

        public Char CurrentChar => encodedString[idx];

        private void CheckBounds(int index)
        {
            if (index >= Length)
            {
                throw new IndexOutOfRangeException($"{idx} {index} > {Length}\n {encodedString}");
                throw new ArgumentOutOfRangeException("idx");
            }
        }

        public Char GetChar()
        {
            CheckBounds(idx);
            var character = encodedString[idx];
            idx++;
            return character;
        }

        public string GetNextNChars(int n)
        {
            CheckBounds(idx + n - 1);
            var sub_string = encodedString.Substring(idx, n);
            idx += n;
            return sub_string;
        }

        public int Length => encodedString.Length;
    }
}
