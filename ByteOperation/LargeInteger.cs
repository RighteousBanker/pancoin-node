using NeinMath;
using System;
using System.Linq;

namespace ByteOperation
{
    [Serializable]
    public class LargeInteger
    {
        private Integer value;

        public LargeInteger()
        {
            value = 0;
        }

        public LargeInteger(int number)
        {
            value = Integer.Parse(number.ToString());
        }

        public LargeInteger(ulong number)
        {
            value = Integer.Parse(number.ToString());
        }

        public LargeInteger(string decimalNumber)
        {
            if (decimalNumber[0] == '-')
            {
                throw new Exception("Cannot contain negative numbers");
            }

            value = IntegerConverter.FromDecimalString(decimalNumber);
        }

        public LargeInteger(byte[] bytes)
        {
            if (bytes != null)
            {
                if (bytes[0] > 127)
                {
                    bytes = ByteManipulator.BigEndianTruncate(bytes, bytes.Length + 1); //add big endian padding to get only positive numbers
                }

                if (BitConverter.IsLittleEndian)
                {
                    bytes = bytes.Reverse().ToArray();
                }

                value = IntegerConverter.FromByteArray(bytes);
            }
            else
            {
                value = 0;
            }
        }

        private LargeInteger(Integer integer)
        {
            value = integer;
        }

        public static LargeInteger MaxValue(int byteLenght)
        {
            byte[] maxNumber = new byte[byteLenght];

            for (int i = 0; i < byteLenght; i++)
            {
                maxNumber[i] = 255;
            }

            return new LargeInteger(maxNumber);
        }

        public static bool operator >(LargeInteger a, LargeInteger b) => a.value > b.value;

        public static bool operator <(LargeInteger a, LargeInteger b) => a.value < b.value;

        public static bool operator ==(LargeInteger a, LargeInteger b)
        {
            var ret = false;

            if (a is null && b is null)
            {
                ret = true;
            }
            else if (a is null)
            {
                ret = false;
            }
            else if (b is null)
            {
                ret = false;
            }
            else
            {
                ret = a.value == b.value;
            }
            return ret;
        }

        public static bool operator !=(LargeInteger a, LargeInteger b)
        {
            var ret = false;

            if (a is null && b is null)
            {
                ret = false;
            }
            else if (a is null)
            {
                ret = true;
            }
            else if (b is null)
            {
                ret = true;
            }
            else
            {
                ret = a.value != b.value;
            }
            return ret;
        }

        public static LargeInteger operator +(LargeInteger a, LargeInteger b) => new LargeInteger(a.value + b.value);

        public static LargeInteger operator -(LargeInteger a, LargeInteger b) => new LargeInteger(a.value - b.value);

        public static LargeInteger operator *(LargeInteger a, LargeInteger b) => new LargeInteger(a.value * b.value);

        public static LargeInteger operator /(LargeInteger a, LargeInteger b) => new LargeInteger(a.value / b.value);

        public static LargeInteger operator +(LargeInteger a, int b) => new LargeInteger(a.value + b);

        public static implicit operator LargeInteger(int integer)
        {
            return new LargeInteger(integer);
        }

        public static implicit operator LargeInteger(long integer)
        {
            return new LargeInteger(integer);
        }

        public byte[] GetBytes() => value.ToByteArray().Reverse().ToArray();

        public override string ToString()
        {
            return value.ToDecimalString();
        }

        public string ToHexString()
        {
            return HexConverter.ToPrefixString(GetBytes());
        }
    }
}
