using System;
using System.Collections.Generic;

namespace SnesGFX.SNES
{
    class Tile
    {
        /// <summary>
        /// Size of tile in pixels.
        /// </summary>
        public int Size { get; private set; }

        /// <summary>
        /// Gets the pixel data of 8x8 block.
        /// </summary>
        public byte[] TileData { get; }

        public Tile(byte[] block, int size)
        {
            Size = size;
            TileData = new byte[size * size];
            Array.Copy(block, TileData, TileData.Length);
        }

        public static Tile FlipX(Tile tile)
        {
            byte[] output = new byte[tile.TileData.Length];
            for (int y = 0; y < tile.Size; ++y)
            {
                for (int x = 0; x < tile.Size; ++x)
                {
                    output[y * tile.Size + (x ^ (tile.Size - 1))] = tile.TileData[y * tile.Size + x];
                }
            }
            return new Tile(output, tile.Size);
        }
        public static Tile FlipY(Tile tile)
        {
            byte[] output = new byte[tile.TileData.Length];
            for (int y = 0; y < tile.Size; ++y)
            {
                for (int x = 0; x < tile.Size; ++x)
                {
                    output[(y ^ (tile.Size - 1)) * tile.Size + x] = tile.TileData[y * tile.Size + x];
                }
            }
            return new Tile(output, tile.Size);
        }
        public static Tile FlipXY(Tile tile)
        {
            byte[] output = new byte[tile.TileData.Length];
            for (int y = 0; y < tile.Size; ++y)
            {
                for (int x = 0; x < tile.Size; ++x)
                {
                    output[(y ^ (tile.Size - 1)) * tile.Size + (x ^ (tile.Size - 1))] = tile.TileData[y * tile.Size + x];
                }
            }
            return new Tile(output, tile.Size);
        }

        public static Tile[] FromBitmap(byte[] bitmap, int width, int blockWidth)
        {
            // FIX ME
            int tileSize = blockWidth * blockWidth;

            byte[] blocks8x8 = Generic.linearToBlocks(bitmap, width, blockWidth);
            Tile[] output = new Tile[blocks8x8.Length / tileSize];
            byte[] currentBlock = new byte[tileSize];

            for (int i = 0, j = blocks8x8.Length, y = 0; i < j; i += tileSize, ++y)
            {
                for (int x = 0; x < tileSize; ++x)
                {
                    currentBlock[x] = blocks8x8[i + x];
                }

                output[y] = new Tile(currentBlock, blockWidth);
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
            int size = blocks[0].TileData.Length;
            byte[] output = new byte[blocks.Length * size];

            for (int i = 0, j = blocks.Length, y = 0; i < j; ++i, y += size)
            {
                for (int x = 0; x < size; ++x)
                {
                    output[x + y] = blocks[i].TileData[x];
                }
            }

            return output;
        }

        public static byte[] ConvertToBitmap(Tile[] blocks, int width)
        {
            return Generic.blocksToLinear(Join(blocks), width, blocks[0].Size);
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

            foreach (int pixel in TileData)
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
            if (one.Size != two.Size)
            {
                return false;
            }

            for (int i = 0; i < one.TileData.Length; ++i)
            {
                if (one.TileData[i] != two.TileData[i])
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
