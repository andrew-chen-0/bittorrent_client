using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace codecrafters_bittorrent.src
{
    internal class Bencode
    {

        // Example: "5:hello" -> "hello"
        public static string DecodeString(string encodedValue)
        {
            var colonIndex = encodedValue.IndexOf(':');
            if (colonIndex != -1 && int.TryParse(encodedValue[..colonIndex], out int strLength))
            {
                return encodedValue.Substring(colonIndex + 1, strLength);
            }
            throw new InvalidOperationException("Invalid encoded value: " + encodedValue);
            
        }


        // Example: "i52e" -> "52"
        public static int DecodeInt(string encodedValue)
        {
            if (encodedValue[0] == 'i' && 
                encodedValue[encodedValue.Length - 1] == 'e' &&
                int.TryParse(encodedValue[1..(encodedValue.Length - 2)], out int decoded_int)) {
                return decoded_int;
            }
            throw new InvalidOperationException("Invalid encoded value: " + encodedValue);
        }
    }
}
