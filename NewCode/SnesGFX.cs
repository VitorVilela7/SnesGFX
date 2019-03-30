using System;
using System.Collections.Generic;
using System.Text;

namespace SnesGFX
{
    class SnesGFX
    {
        static IBitformat[] formats;

        public static IBitformat[] AvaiableFormats
        {
            get
            {
                return formats;
            }
        }

        public static void Init()
        {
            if (formats != null) { return; }
            formats = new IBitformat[] {
                new SNES._2BPP(),   // 0
                new SNES._3BPP(),   // 1
                new SNES._4BPP(),   // 2
                new SNES._8BPP(),   // 3
                new Linear._2BPP(), // 4
                new Linear._4BPP(), // 5
                new Linear._8BPP(), // 6
                new Packed._2BPP(), // 7
                new Packed._4BPP(), // 8
                new SNES.Mode7(),   // 9
            };
        }
    }
}
