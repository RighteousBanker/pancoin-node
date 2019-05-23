using System;
using System.Collections.Generic;
using System.Text;

namespace Encoders
{
    public interface ISerialCoderLookup
    {
        byte[] ReadByOffset(long offset);
        byte[] ReadByIndex(int index);
        long GetIndexOffset(int index);
        long NextOffset(long start);
    }
}
