using System;
using System.Collections.Generic;

namespace SnesGFX.SNES
{
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
