using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;

namespace SnesGFX.SNES
{
    static class Generic
    {
        public static byte[] linearToBlocks(byte[] input, int width)
        {
            int size = input.Length;

            byte[] output = new byte[size];
            int a8x8 = (size / 64);
            int x = 0, y = 0, i = 0;

            int r = 0;

            do
            {
                i = ((i % width) == 0 && (i > 0)) ? i + (width * 7) : i;
                y = 0;
                do
                {
                    r = i + (y & 7) + (width * (y >> 3));
                    if (r >= size) continue;

                    output[x * 64 + y] = input[r];

                    //output[x * 64 + y] = input[i + ((y < 8) ? y :
                    //    (y < 16) ? width + y - 8 :
                    //    (y < 24) ? width * 2 + y - 16 :
                    //    (y < 32) ? width * 3 + y - 24 :
                    //    (y < 40) ? width * 4 + y - 32 :
                    //    (y < 48) ? width * 5 + y - 40 :
                    //    (y < 56) ? width * 6 + y - 48 :
                    //    (y < 64) ? width * 7 + y - 56 : 0)];
                } while (++y < 64);
                i += 8;
            } while (++x < a8x8);

            input = null;
            return output;
        }

        public static byte[] blocksToLinear(byte[] input, int width)
        {
            int size = input.Length;

            byte[] output = new byte[size];
            int a8x8 = (size / 64);
            int x = 0, y = 0, i = 0;

            int r = 0;

            do
            {
                i = ((i % width) == 0 && (i > 0)) ? i + (width * 7) : i;

                y = 0;
                do
                {
                    //output[i + (
                    //    (y < 8) ? y :
                    //    (y < 16) ? (width) + y - 8 :
                    //    (y < 24) ? (width * 2) + y - 16 :
                    //    (y < 32) ? (width * 3) + y - 24 :
                    //    (y < 40) ? (width * 4) + y - 32 :
                    //    (y < 48) ? (width * 5) + y - 40 :
                    //    (y < 56) ? (width * 6) + y - 48 :
                    //    (y < 64) ? (width * 7) + y - 56 : 0)]
                    //    = input[x * 64 + y];

                    r = i + (y & 7) + (width * (y >> 3));
                    if (r >= size) continue;

                    output[r] = input[x * 64 + y];
                } while (++y < 64);
                i += 8;
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
            return Generic.linearToBlocks(input, 128);
        }

        public byte[] Decode(byte[] input)
        {
            return Generic.blocksToLinear(input, 128);
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
            return _encode(Generic.linearToBlocks(bitmap, 128));
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
            return Generic.blocksToLinear(_decode(data), 128);
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
            return _encode(Generic.linearToBlocks(bitmap, 128));
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
            return Generic.blocksToLinear(_decode(data), 128);
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
            return _encode(Generic.linearToBlocks(bitmap, 128));
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
            return Generic.blocksToLinear(_decode(data), 128);
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
            return _encode(Generic.linearToBlocks(bitmap, 128));
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
            return Generic.blocksToLinear(_decode(data), 128);
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

    class Tile
    {
        private readonly byte[] tile8x8;

        /// <summary>
        /// Gets the pixel data of 8x8 block.
        /// </summary>
        public byte[] TileData
        {
            get
            {
                return tile8x8;
            }
        }

        public Tile(byte[] block)
        {
            this.tile8x8 = new byte[64];
            Array.Copy(block, tile8x8, 64);
        }

        public static Tile FlipX(Tile tile)
        {
            byte[] output = new byte[64];
            for (int y = 0; y < 8; ++y)
            {
                for (int x = 0; x < 8; ++x)
                {
                    output[(y << 3) + (x ^ 7)] = tile.tile8x8[(y << 3) + x];
                }
            }
            return new Tile(output);
        }
        public static Tile FlipY(Tile tile)
        {
            byte[] output = new byte[64];
            for (int y = 0; y < 8; ++y)
            {
                for (int x = 0; x < 8; ++x)
                {
                    output[((y ^ 7) << 3) + x] = tile.tile8x8[(y << 3) + x];
                }
            }
            return new Tile(output);
        }
        public static Tile FlipXY(Tile tile)
        {
            byte[] output = new byte[64];
            for (int y = 0; y < 8; ++y)
            {
                for (int x = 0; x < 8; ++x)
                {
                    output[((y ^ 7) << 3) + (x ^ 7)] = tile.tile8x8[(y << 3) + x];
                }
            }
            return new Tile(output);
        }

        public static Tile[] FromBitmap(byte[] bitmap, int width)
        {
            byte[] blocks8x8 = Generic.linearToBlocks(bitmap, width);
            Tile[] output = new Tile[blocks8x8.Length / 64];
            byte[] currentBlock = new byte[64];

            for (int i = 0, j = blocks8x8.Length, y = 0; i < j; i += 64, ++y)
            {
                for (int x = 0; x < 64; ++x)
                {
                    currentBlock[x] = blocks8x8[i + x];
                }

                output[y] = new Tile(currentBlock);
            }

            return output;
        }

        public static Tile[] RemoveRepeatedBlocks(Tile[] blocks, bool allowFlip,
            out int[] result, out bool[] flipx, out bool[] flipy)
        {
            result = new int[blocks.Length];
            flipx = new bool[blocks.Length];
            flipy = new bool[blocks.Length];
            List<Tile> output = new List<Tile>();

            Tile[] xflipped = new Tile[blocks.Length];
            Tile[] yflipped = new Tile[blocks.Length];
            Tile[] xyflipped = new Tile[blocks.Length];

            for (int i = 0; i < blocks.Length; ++i)
            {
                xflipped[i] = Tile.FlipX(blocks[i]);
                yflipped[i] = Tile.FlipY(blocks[i]);
                xyflipped[i] = Tile.FlipXY(blocks[i]);
            }

            for (int i = 0, j = blocks.Length; i < j; ++i)
            {
                for (int x = 0, y = output.Count; x < y; ++x)
                {
                    if (blocks[i] == output[x])
                    {
                        result[i] = x;
                        flipx[i] = false;
                        flipy[i] = false;
                        goto reset;
                    }
                    if (xflipped[i] == output[x] && allowFlip)
                    {
                        result[i] = x;
                        flipx[i] = true;
                        flipy[i] = false;
                        goto reset;
                    }
                    if (yflipped[i] == output[x] && allowFlip)
                    {
                        result[i] = x;
                        flipx[i] = false;
                        flipy[i] = true;
                        goto reset;
                    }
                    if (xyflipped[i] == output[x] && allowFlip)
                    {
                        result[i] = x;
                        flipx[i] = true;
                        flipy[i] = true;
                        goto reset;
                    }
                }

                output.Add(blocks[i]);
                result[i] = output.Count - 1;

            reset:
                continue;
            }

            return output.ToArray();
        }

        public static byte[] Join(Tile[] blocks)
        {
            byte[] output = new byte[blocks.Length * 64];

            for (int i = 0, j = blocks.Length, y = 0; i < j; ++i, y+=64)
            {
                for (int x = 0; x < 64; ++x)
                {
                    output[x + y] = blocks[i].tile8x8[x];
                }
            }

            return output;
        }

        public static byte[] ConvertToBitmap(Tile[] blocks, int width)
        {
            return Generic.blocksToLinear(Join(blocks), width);
        }

        public override bool Equals(object obj)
        {
            if (obj is Tile)
            {
                return this == (Tile)obj;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// This return much like a sum of all values...
        /// </summary>
        /// <returns>The XORSUM of all pixels.</returns>
        public override int GetHashCode()
        {
            int result = 0x55555555;
            int shift = 0;

            foreach (int pixel in tile8x8)
            {
                result ^= pixel << shift;
                shift += 8;

                if (shift == 32)
                {
                    shift = 0;
                }
            }

            return result;
        }

        /// <summary>
        /// Returns true if all pixels in those tiles are equal.
        /// </summary>
        /// <param name="one">First tile to compare</param>
        /// <param name="two">Second tile to compare</param>
        /// <returns>True if all pixels are equal, otherwise, false.</returns>
        public static bool operator ==(Tile one, Tile two)
        {
            for (int i = 0; i < 64; ++i)
            {
                if (one.tile8x8[i] != two.tile8x8[i])
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Returns true if some pixel isn't equal.
        /// </summary>
        /// <param name="one">First tile to compare</param>
        /// <param name="two">Second tile to compare</param>
        /// <returns>True if isn't equal.</returns>
        public static bool operator !=(Tile one, Tile two)
        {
            return !(one == two);
        }
    }
}
