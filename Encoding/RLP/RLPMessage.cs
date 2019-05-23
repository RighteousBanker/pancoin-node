using System;
using System.Collections.Generic;
using System.Text;

namespace Encoders
{
    internal class RLPMessage
    {
        public RLPMessage(byte[] input) //modified version of RLP library by Ciaran Jones
        {
            Decoded = new List<byte[]>();
            Remainder = new ArraySegment<byte>(input);
        }

        public List<byte[]> Decoded { get; set; }

        public ArraySegment<byte> Remainder { get; set; }
    }
}
