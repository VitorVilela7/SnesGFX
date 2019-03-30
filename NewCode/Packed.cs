using System;
using System.Collections.Generic;
using System.Text;

namespace SnesGFX.Packed
{
    class _4BPP : IBitformat
    {
        public int BitsPerPixel
        {
            get { return 4; }
        }

        public int Colors
        {
            get { return 16; }
        }

        public BitformatType Type
        {
            get { return BitformatType.BITFORMAT_PACKED; }
        }

        public string Name
        {
            get { return "4BPP Packed"; }
        }

        public bool AlignBy8x8
        {
            get { return false; }
        }

        public int FixedWidth
        {
            get { return 0; }
        }

        public byte[] Encode(byte[] input)
        {
            byte[] output = new byte[input.Length >> 1];

            for (int x = 0, y = 0, j = input.Length >> 1; x < j; ++x, y += 2)
            {
                output[x] = (byte)((input[y] & 15) | ((input[y + 1] & 15) << 4));
            }

            return output;
        }

        public byte[] Decode(byte[] input)
        {
            byte[] output = new byte[input.Length << 1];

            for (int i = 0, j = 0, s = input.Length; i < s; ++i, j += 2)
            {
                output[j] = (byte)(input[i] & 15);
                output[j + 1] = (byte)(input[i] >> 4);
            }

            return output;
        }
    }

    class _2BPP : IBitformat
    {
        public int BitsPerPixel
        {
            get { return 2; }
        }

        public int Colors
        {
            get { return 4; }
        }

        public BitformatType Type
        {
            get { return BitformatType.BITFORMAT_PACKED; }
        }

        public string Name
        {
            get { return "2BPP Packed"; }
        }

        public bool AlignBy8x8
        {
            get { return false; }
        }

        public int FixedWidth
        {
            get { return 0; }
        }

        public byte[] Encode(byte[] input)
        {
            byte[] output = new byte[input.Length >> 2];

            for (int x = 0, y = 0, j = input.Length >> 2; x < j; ++x, y += 4)
            {
                output[x] =  (byte)( input[y    ] & 3);
                output[x] |= (byte)((input[y + 1] & 3) << 2);
                output[x] |= (byte)((input[y + 2] & 3) << 4);
                output[x] |= (byte)((input[y + 3] & 3) << 6);
            }

            return output;
        }

        public byte[] Decode(byte[] input)
        {
            byte[] output = new byte[input.Length << 2];

            for (int i = 0, j = 0, s = input.Length; i < s; ++i, j += 4)
            {
                output[j    ] = (byte)( input[i]       & 3);
                output[j + 1] = (byte)((input[i] >> 2) & 3);
                output[j + 2] = (byte)((input[i] >> 4) & 3);
                output[j + 3] = (byte)((input[i] >> 6) & 3);
            }

            return output;
        }
    }
}
