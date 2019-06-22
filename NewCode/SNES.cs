using System.Text;
using System.Runtime.InteropServices;

namespace SnesGFX.SNES
{
    static class Generic
    {
        public static byte[] linearToBlocks(byte[] input, int width, int tileWidth)
        {
            int size = input.Length;

            byte[] output = new byte[size];
            int blockSize = tileWidth * tileWidth;
            int a8x8 = size / blockSize;
            int x = 0, y = 0, i = 0;

            int r = 0;

            do
            {
                i = ((i % width) == 0 && (i > 0)) ? i + (width * (tileWidth - 1)) : i;
                y = 0;
                do
                {
                    r = i + (y & (tileWidth - 1)) + (width * (y / tileWidth));
                    if (r >= size) continue;

                    output[x * blockSize + y] = input[r];
                } while (++y < blockSize);
                i += tileWidth;
            } while (++x < a8x8);

            input = null;
            return output;
        }

        public static byte[] blocksToLinear(byte[] input, int width, int tileWidth)
        {
            int size = input.Length;

            byte[] output = new byte[size];
            int blockSize = tileWidth * tileWidth;
            int a8x8 = size / blockSize;
            int x = 0, y = 0, i = 0;

            int r = 0;

            do
            {
                i = ((i % width) == 0 && (i > 0)) ? i + (width * (tileWidth - 1)) : i;

                y = 0;
                do
                {
                    r = i + (y & (blockSize - 1)) + (width * (y / tileWidth));
                    if (r >= size) continue;

                    output[r] = input[x * blockSize + y];
                } while (++y < blockSize);
                i += blockSize;
            } while (++x < a8x8);

            return output;
        }
    }

    unsafe class Mode7 : IBitformat
    {
        public int BitsPerPixel { get { return 8; } }
        public int Colors { get { return 256; } }
        public BitformatType Type { get { return BitformatType.BITFORMAT_PLANAR; } }
        public string Name { get { return "Mode 7"; } }
        public bool AlignBy8x8 { get { return true; } }
        public int FixedWidth { get { return 128; } }

        public byte[] Encode(byte[] input)
        {
            return Generic.linearToBlocks(input, 128, 8);
        }

        public byte[] Decode(byte[] input)
        {
            return Generic.blocksToLinear(input, 128, 8);
        }
    }

    unsafe class _2BPP : IBitformat
    {
        public int BitsPerPixel { get { return 2; } }
        public int Colors { get { return 4; } }
        public BitformatType Type { get { return BitformatType.BITFORMAT_PLANAR; } }
        public string Name { get { return "2BPP GB"; } }
        public bool AlignBy8x8 { get { return true; } }
        public int FixedWidth { get { return 128; } }

        public byte[] Encode(byte[] bitmap)
        {
            return _encode(Generic.linearToBlocks(bitmap, 128, 8));
        }

        private byte[] _encode(byte[] input)
        {
            int bits = 0;
            int sizeFix = input.Length / 4; //64/16
            if ((sizeFix % 16) != 0)
            {
                sizeFix += 16 - (sizeFix % 16);
            }
            byte[] data = new byte[sizeFix];

            fixed (byte* output = &data[0], pinput = &input[0])
            {
                byte* ptr1 = output - 1;
                byte* ptr2 = pinput;
                byte* ptr3 = ptr2 + input.Length;

                while (ptr2 < ptr3)
                {
                    bits = 0;
                    for (int y = 0; y < 8; ++y)
                    {
                        bits |= (ptr2[y] & 1) << (7 - y);
                    }
                    *++ptr1 = (byte)bits;

                    bits = 0;
                    for (int y = 0; y < 8; ++y)
                    {
                        bits |= (ptr2[y] & 2) >> 1 << (7 - y);
                    }
                    *++ptr1 = (byte)bits;

                    ptr2 += 8;
                }
            }

            return data;
        }

        private byte decodePixel(byte* ptr2, int shift)
        {
            return (byte)(
                (ptr2[1] >> shift << 1 & 2)
              | (ptr2[0] >> shift      & 1));
        }

        public byte[] Decode(byte[] data)
        {
            return Generic.blocksToLinear(_decode(data), 128, 8);
        }

        public byte[] _decode(byte[] input)
        {
            int size = input.Length;
            int sizeFix = size * 4;
            if (sizeFix % 64 != 0)
            {
                sizeFix += 64 - (sizeFix % 64);
            }

            byte[] output = new byte[sizeFix]; // 64/16 = 4

            fixed (byte* poutput = &output[0], pinput = &input[0])
            {
                byte* ptr1 = poutput - 1;
                byte* ptr2 = pinput;
                byte* ptr3 = pinput + size;

                while (ptr2 < ptr3)
                {
                    *++ptr1 = decodePixel(ptr2, 7);
                    *++ptr1 = decodePixel(ptr2, 6);
                    *++ptr1 = decodePixel(ptr2, 5);
                    *++ptr1 = decodePixel(ptr2, 4);
                    *++ptr1 = decodePixel(ptr2, 3);
                    *++ptr1 = decodePixel(ptr2, 2);
                    *++ptr1 = decodePixel(ptr2, 1);
                    *++ptr1 = decodePixel(ptr2, 0);
                    
                    ptr2 += 2;
                }
            }
            
            return output;
        }
    }

    unsafe class _3BPP : IBitformat
    {
        public int BitsPerPixel { get { return 3; } }
        public int Colors { get { return 8; } }
        public BitformatType Type { get { return BitformatType.BITFORMAT_PLANAR; } }
        public string Name { get { return "3BPP SNES"; } }
        public bool AlignBy8x8 { get { return true; } }
        public int FixedWidth { get { return 128; } }
        
        public byte[] Encode(byte[] bitmap)
        {
            return _encode(Generic.linearToBlocks(bitmap, 128, 8));
        }

        private byte[] _encode(byte[] input)
        {
            int bits = 0;
            int sizeFix = input.Length * 3 / 8; //64/24 = 8/3
            if ((sizeFix % 24) != 0)
            {
                sizeFix += 24 - (sizeFix % 24);
            }
            byte[] data = new byte[sizeFix];

            fixed (byte* output = &data[0], pinput = &input[0])
            {
                byte* ptr1 = output - 1;
                byte* ptr2 = pinput;
                byte* ptr3 = ptr2 + input.Length;

                while (ptr2 < ptr3)
                {
                    for (int i = 0; i < 64; i += 8)
                    {
                        bits = 0;
                        for (int y = 0; y < 8; ++y)
                        {
                            bits |= (ptr2[i + y] & 1) << (7 - y);
                        }
                        *++ptr1 = (byte)bits;

                        bits = 0;
                        for (int y = 0; y < 8; ++y)
                        {
                            bits |= (ptr2[i + y] & 2) >> 1 << (7 - y);
                        }
                        *++ptr1 = (byte)bits;
                    }
                    for (int i = 0; i < 64; i += 8)
                    {
                        bits = 0;
                        for (int y = 0; y < 8; ++y)
                        {
                            bits |= (ptr2[i + y] & 4) >> 2 << (7 - y);
                        }
                        *++ptr1 = (byte)bits;
                    }

                    ptr2 += 64;
                }
            }

            return data;
        }

        private byte decodePixel(byte* ptr2, byte* ptr4, int shift)
        {
            return (byte)(
                (ptr4[16] >> shift << 2 & 4)
              | (ptr2[1 ] >> shift << 1 & 2)
              | (ptr2[0 ] >> shift & 1));
        }

        public byte[] Decode(byte[] data)
        {
            return Generic.blocksToLinear(_decode(data), 128, 8);
        }

        public byte[] _decode(byte[] input)
        {
            int size = input.Length;
            int sizeFix = size * 8 / 3;
            if (sizeFix % 64 != 0)
            {
                sizeFix += 64 - (sizeFix % 64);
            }

            byte[] output = new byte[sizeFix]; // 64/24 = 8/3

            fixed (byte* poutput = &output[0], pinput = &input[0])
            {
                byte* ptr1 = poutput - 1;
                byte* ptr2 = pinput;
                byte* ptr3 = pinput + size;
                byte* ptr4 = pinput;
                
                while (ptr2 < ptr3)
                {
                    for (int i = 0; i < 16; i += 2, ptr2 += 2, ++ptr4)
                    {
                        *++ptr1 = decodePixel(ptr2, ptr4, 7);
                        *++ptr1 = decodePixel(ptr2, ptr4, 6);
                        *++ptr1 = decodePixel(ptr2, ptr4, 5);
                        *++ptr1 = decodePixel(ptr2, ptr4, 4);
                        *++ptr1 = decodePixel(ptr2, ptr4, 3);
                        *++ptr1 = decodePixel(ptr2, ptr4, 2);
                        *++ptr1 = decodePixel(ptr2, ptr4, 1);
                        *++ptr1 = decodePixel(ptr2, ptr4, 0);
                    }
                    ptr2 += 8; //24-16
                    ptr4 += 16;
                }
            }

            return output;
        }
    }

    unsafe class _4BPP : IBitformat
    {
        public int BitsPerPixel { get { return 4; } }
        public int Colors { get { return 16; } }
        public BitformatType Type { get { return BitformatType.BITFORMAT_PLANAR; } }
        public string Name { get { return "4BPP SNES"; } }
        public bool AlignBy8x8 { get { return true; } }
        public int FixedWidth { get { return 128; } }

        public byte[] Encode(byte[] bitmap)
        {
            return _encode(Generic.linearToBlocks(bitmap, 128, 8));
        }

        private byte[] _encode(byte[] input)
        {
            int bits = 0;
            int sizeFix = input.Length / 2; //64/32 = 2
            if ((sizeFix % 32) != 0)
            {
                sizeFix += 32 - (sizeFix % 32);
            }
            byte[] data = new byte[sizeFix];

            fixed (byte* output = &data[0], pinput = &input[0])
            {
                byte* ptr1 = output - 1;
                byte* ptr2 = pinput;
                byte* ptr3 = ptr2 + input.Length;

                while (ptr2 < ptr3)
                {
                    for (int i = 0; i < 64; i += 8)
                    {
                        bits = 0;
                        for (int y = 0; y < 8; ++y)
                        {
                            bits |= (ptr2[i + y] & 1) << (7 - y);
                        }
                        *++ptr1 = (byte)bits;

                        bits = 0;
                        for (int y = 0; y < 8; ++y)
                        {
                            bits |= (ptr2[i + y] & 2) >> 1 << (7 - y);
                        }
                        *++ptr1 = (byte)bits;
                    }
                    for (int i = 0; i < 64; i += 8)
                    {
                        bits = 0;
                        for (int y = 0; y < 8; ++y)
                        {
                            bits |= (ptr2[i + y] & 4) >> 2 << (7 - y);
                        }
                        *++ptr1 = (byte)bits;

                        bits = 0;
                        for (int y = 0; y < 8; ++y)
                        {
                            bits |= (ptr2[i + y] & 8) >> 3 << (7 - y);
                        }
                        *++ptr1 = (byte)bits;
                    }

                    ptr2 += 64;
                }
            }

            return data;
        }

        private byte decodePixel(byte* ptr2, int shift)
        {
            return (byte)(
                (ptr2[17] >> shift << 3 & 8)
              | (ptr2[16] >> shift << 2 & 4)
              | (ptr2[ 1] >> shift << 1 & 2)
              | (ptr2[ 0] >> shift      & 1));
        }

        public byte[] Decode(byte[] data)
        {
            return Generic.blocksToLinear(_decode(data), 128, 8);
        }

        public byte[] _decode(byte[] input)
        {
            int size = input.Length;
            int sizeFix = size * 2;
            if (sizeFix % 64 != 0)
            {
                sizeFix += 64 - (sizeFix % 64);
            }

            byte[] output = new byte[sizeFix]; // 64/32 = 2

            fixed (byte* poutput = &output[0], pinput = &input[0])
            {
                byte* ptr1 = poutput - 1;
                byte* ptr2 = pinput;
                byte* ptr3 = pinput + size;
                
                while (ptr2 < ptr3)
                {
                    for (int i = 0; i < 16; i += 2, ptr2 += 2)
                    {
                        *++ptr1 = decodePixel(ptr2, 7);
                        *++ptr1 = decodePixel(ptr2, 6);
                        *++ptr1 = decodePixel(ptr2, 5);
                        *++ptr1 = decodePixel(ptr2, 4);
                        *++ptr1 = decodePixel(ptr2, 3);
                        *++ptr1 = decodePixel(ptr2, 2);
                        *++ptr1 = decodePixel(ptr2, 1);
                        *++ptr1 = decodePixel(ptr2, 0);
                    }
                    ptr2 += 16; //32-16
                }
            }

            return output;
        }
    }

    unsafe class _8BPP : IBitformat
    {
        public int BitsPerPixel { get { return 8; } }
        public int Colors { get { return 256; } }
        public BitformatType Type { get { return BitformatType.BITFORMAT_PLANAR; } }
        public string Name { get { return "8BPP SNES"; } }
        public bool AlignBy8x8 { get { return true; } }
        public int FixedWidth { get { return 128; } }

        public byte[] Encode(byte[] bitmap)
        {
            return _encode(Generic.linearToBlocks(bitmap, 128, 8));
        }

        private byte[] _encode(byte[] input)
        {
            int bits = 0;
            int sizeFix = input.Length; //64/64
            if ((sizeFix % 64) != 0)
            {
                sizeFix += 64 - (sizeFix % 64);
            }
            byte[] data = new byte[sizeFix];

            fixed (byte* output = &data[0], pinput = &input[0])
            {
                byte* ptr1 = output - 1;
                byte* ptr2 = pinput;
                byte* ptr3 = ptr2 + input.Length;

                while (ptr2 < ptr3)
                {
                    for (int i = 0; i < 64; i += 8)
                    {
                        bits = 0;
                        for (int y = 0; y < 8; ++y)
                        {
                            bits |= (ptr2[i + y] & 1) << (7 - y);
                        }
                        *++ptr1 = (byte)bits;

                        bits = 0;
                        for (int y = 0; y < 8; ++y)
                        {
                            bits |= (ptr2[i + y] & 2) >> 1 << (7 - y);
                        }
                        *++ptr1 = (byte)bits;
                    }
                    for (int i = 0; i < 64; i += 8)
                    {
                        bits = 0;
                        for (int y = 0; y < 8; ++y)
                        {
                            bits |= (ptr2[i + y] & 4) >> 2 << (7 - y);
                        }
                        *++ptr1 = (byte)bits;

                        bits = 0;
                        for (int y = 0; y < 8; ++y)
                        {
                            bits |= (ptr2[i + y] & 8) >> 3 << (7 - y);
                        }
                        *++ptr1 = (byte)bits;
                    }
                    for (int i = 0; i < 64; i += 8)
                    {
                        bits = 0;
                        for (int y = 0; y < 8; ++y)
                        {
                            bits |= (ptr2[i + y] & 16) >> 4 << (7 - y);
                        }
                        *++ptr1 = (byte)bits;

                        bits = 0;
                        for (int y = 0; y < 8; ++y)
                        {
                            bits |= (ptr2[i + y] & 32) >> 5 << (7 - y);
                        }
                        *++ptr1 = (byte)bits;
                    }
                    for (int i = 0; i < 64; i += 8)
                    {
                        bits = 0;
                        for (int y = 0; y < 8; ++y)
                        {
                            bits |= (ptr2[i + y] & 64) >> 6 << (7 - y);
                        }
                        *++ptr1 = (byte)bits;

                        bits = 0;
                        for (int y = 0; y < 8; ++y)
                        {
                            bits |= (ptr2[i + y] & 128) >> 7 << (7 - y);
                        }
                        *++ptr1 = (byte)bits;
                    }

                    ptr2 += 64;
                }
            }

            return data;
        }

        private byte decodePixel(byte* ptr2, int shift)
        {
            return (byte)(
                (ptr2[49] >> shift << 7 & 128)
              | (ptr2[48] >> shift << 6 &  64)
              | (ptr2[33] >> shift << 5 &  32)
              | (ptr2[32] >> shift << 4 &  16)
              | (ptr2[17] >> shift << 3 &   8)
              | (ptr2[16] >> shift << 2 &   4)
              | (ptr2[ 1] >> shift << 1 &   2)
              | (ptr2[ 0] >> shift      &   1));
        }

        public byte[] Decode(byte[] data)
        {
            return Generic.blocksToLinear(_decode(data), 128, 8);
        }

        public byte[] _decode(byte[] input)
        {
            int size = input.Length;
            int sizeFix = size;
            if (sizeFix % 64 != 0)
            {
                sizeFix += 64 - (sizeFix % 64);
            }

            byte[] output = new byte[sizeFix]; // 64/64

            fixed (byte* poutput = &output[0], pinput = &input[0])
            {
                byte* ptr1 = poutput - 1;
                byte* ptr2 = pinput;
                byte* ptr3 = pinput + size;

                while (ptr2 < ptr3)
                {
                    for (int i = 0; i < 16; i += 2, ptr2 += 2)
                    {
                        *++ptr1 = decodePixel(ptr2, 7);
                        *++ptr1 = decodePixel(ptr2, 6);
                        *++ptr1 = decodePixel(ptr2, 5);
                        *++ptr1 = decodePixel(ptr2, 4);
                        *++ptr1 = decodePixel(ptr2, 3);
                        *++ptr1 = decodePixel(ptr2, 2);
                        *++ptr1 = decodePixel(ptr2, 1);
                        *++ptr1 = decodePixel(ptr2, 0);
                    }
                    ptr2 += 48; //64-16
                }
            }

            return output;
        }
    }
}
