using System;
using System.Collections.Generic;
using System.Text;

namespace SnesGFX.Linear
{
    class _8BPP : IBitformat
    {
        public int BitsPerPixel
        {
            get { return 8; }
        }

        public int Colors
        {
            get { return 256; }
        }

        public BitformatType Type
        {
            get { return BitformatType.BITFORMAT_LINEAR; }
        }

        public string Name
        {
            get { return "8BPP Linear"; }
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
            return input;
        }

        public byte[] Decode(byte[] input)
        {
            return input;
        }
    }

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
            get { return BitformatType.BITFORMAT_LINEAR; }
        }

        public string Name
        {
            get { return "4BPP Linear"; }
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
            for (int i = 0, j = input.Length; i < j; ++i)
            {
                input[i] &= 15;
            }

            return input;
        }

        public byte[] Decode(byte[] input)
        {
            for (int i = 0, j = input.Length; i < j; ++i)
            {
                input[i] &= 15;
            }

            return input;
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
            get { return BitformatType.BITFORMAT_LINEAR; }
        }

        public string Name
        {
            get { return "2BPP Linear"; }
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
            for (int i = 0, j = input.Length; i < j; ++i)
            {
                input[i] &= 3;
            }

            return input;
        }

        public byte[] Decode(byte[] input)
        {
            for (int i = 0, j = input.Length; i < j; ++i)
            {
                input[i] &= 3;
            }

            return input;
        }
    }
}
