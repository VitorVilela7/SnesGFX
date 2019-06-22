using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

namespace SnesGFX.Codec
{
    class Palette
    {
        public interface Interface
        {
            bool IsGeneric { get; }
            bool TestInput(byte[] input);
            Color[] Decode();
        }
    }
}
