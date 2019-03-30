using System;
using System.Collections.Generic;
using System.Text;

namespace SnesGFX
{
    interface IBitformat
    {
        int BitsPerPixel { get; }               // 2, 3, 4 and 8
        int Colors { get; }                     // obvious, according to BitsPerPixel.
        BitformatType Type { get; }             // packed, linear or snes?
        string Name { get; }                    // known name? 2bpp snes..? 8bpp linear?
        bool AlignBy8x8 { get; }                // image must have a align of 8x8 blocks..?
        int FixedWidth { get; }                 // if n != 0, width = n.

        byte[] Encode(byte[] input);            // Encodes a bitmap.
        byte[] Decode(byte[] input);            // Decodes to a bitmap.
    }

    enum BitformatType : byte
    {
        BITFORMAT_PACKED = 0,                   // "Packed" format
        BITFORMAT_LINEAR = 1,                   // "Linear" format
        BITFORMAT_PLANAR = 2,                   // "SNES"   format
    }
}
