using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using SnesGFX.Properties;
using System.Text;

namespace SnesGFX
{
    unsafe static class Program
    {
        public static readonly Color transparent = Color.FromArgb(0, 0, 0, 0);

        public static Forms.Preview view;
        public static Size bitmapInfo;
        public static RegexOptions ropts = RegexOptions.ECMAScript |
            RegexOptions.IgnoreCase | RegexOptions.Compiled;
        public static bool reloadflag = false;

        /// <summary>
        /// 0=first pixel;1=last pixel;2=#00000000;3=#00FFFFFF;4=custom
        /// </summary>
        public static int colorMode = 0;
        public static Color customTransparent = Color.Magenta;

        /// <summary>
        /// 0=hue;1=brightness;2=saturation;3=manual
        /// </summary>
        public static int arrangeMethod = 0;

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            SnesGFX.Init();
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Forms.Main());
        }

        /// <summary>
        /// Tries to delete a file.
        /// </summary>
        /// <param name="filename">The file to delete</param>
        /// <returns>true if succesful deleted, else it's failed.</returns>
        public static bool TryDelete(string filename)
        {
            if (!File.Exists(filename))
            {
                return true;
            }

            try
            {
                File.Delete(filename);
                return true;
            }
            catch
            {
                return false;
            }
        }
        
        /// <summary>
        /// Converts a bitmap to 24-bit Rgb
        /// </summary>
        /// <param name="input">input/output bit-map</param>
        /// <param name="scale">scale factor. 1.0 = 100%</param>
        /// <returns>Transparency color</returns>
        public static Color PrepareBitmap(ref Bitmap input, double scale)
        {
            bool flag = false;
            Color transparency = transparent;
            int width = (int)(input.Width * scale);
            int height = (int)(input.Height * scale);

            if (scale != 1)
            {
                using (Bitmap scaled = new Bitmap(width, height, PixelFormat.Format32bppArgb))
                {
                    using (Graphics g = Graphics.FromImage(scaled))
                    {
                        g.PageUnit = GraphicsUnit.Pixel;

                        if (!Options.HiQuality)
                        {
                            g.InterpolationMode = InterpolationMode.NearestNeighbor;
                            g.PixelOffsetMode = PixelOffsetMode.HighSpeed;
                            g.SmoothingMode = SmoothingMode.HighSpeed;
                        }

                        else
                        {
                            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                            g.SmoothingMode = SmoothingMode.HighQuality;
                        }

                        g.DrawImage(input, 0, 0, width, height);
                    }

                    input = new Bitmap(scaled, new Size(width, height));
                    bitmapInfo.Width = input.Width;
                    bitmapInfo.Height = input.Height;
                }
            }

            if (width < 128)
            {
                width = 128;
            }
            else if (width % 8 != 0)
            {
                width = width + (8 - (width % 8));
            }

            if (height % 8 != 0)
            {
                height = height + (8 - (height % 8));
            }

            int mpx = width / 128;

            if ((width % 128) != 0)
            {
                mpx++;
            }

            int truewidth = 128;
            int trueheight = height * mpx;

            // hora de descobri se o formato de saída é diferente de SNES
            // isto é importante para corrigir o width...
            if (SnesGFX.AvaiableFormats[Settings.Default.Codec].Type != BitformatType.BITFORMAT_PLANAR)
            {
                width = input.Width;
                height = input.Height;

                if (width % 8 != 0) width = width + (8 - (width % 8));
                if (height % 8 != 0) height = height + (8 - (height % 8));

                bitmapInfo.Width = truewidth = width;
                bitmapInfo.Height = trueheight = height;

                flag = true;
            }

            using (Bitmap compatible = new Bitmap(truewidth, trueheight, PixelFormat.Format32bppArgb))
            {
                using (Graphics g = Graphics.FromImage(compatible))
                {
                    g.PageUnit = GraphicsUnit.Pixel;
                    g.InterpolationMode = InterpolationMode.NearestNeighbor;

                    if (!Options.HiQuality)
                    {
                        g.PixelOffsetMode = PixelOffsetMode.HighSpeed;
                        g.SmoothingMode = SmoothingMode.HighSpeed;
                    }
                    else
                    {
                        g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                        g.SmoothingMode = SmoothingMode.HighQuality;
                    }

                    if (Options.AllowTransparency)
                        switch (colorMode)
                        {
                            case 0:
                                g.Clear(transparency = input.GetPixel(0, 0));
                                break;

                            case 1:
                                g.Clear(transparency = input.GetPixel(input.Width - 1, input.Height - 1));
                                break;

                            case 2:
                                g.Clear(transparency = Color.FromArgb(0, 0, 0, 0));
                                break;

                            case 3:
                                g.Clear(transparency = Color.FromArgb(0, 255, 255, 255));
                                break;

                            case 4:
                                g.Clear(transparency = customTransparent);
                                break;
                        }

                    if (!flag)
                    {
                        for (int x = 0; x < mpx; x++)
                        {
                            g.DrawImage(input, new Rectangle(0, height * x, 128, height), new Rectangle(128 * x, 0, 128, height), GraphicsUnit.Pixel);
                        }
                    }
                    else
                    {
                        g.DrawImage(input, 0, 0, truewidth, trueheight);
                    }
                }
                input.Dispose();
                input = new Bitmap(compatible);
                if (Options.AllowTransparency)
                    input.MakeTransparent(transparency);
            }

            return transparency;
        }

        /// <summary>
        /// Converts a color channel to snes 15-rgb with 4-bit luma.
        /// </summary>
        /// <param name="i">The color (r/g/b)</param>
        /// <returns>New color.</returns>
        public static byte SnesBitChannel(byte i)
        {
            i >>= 3;
            return (byte)((i << 3) | (i >> 2));
        }

		/// <summary>
		/// Converts a bit-map to 15-bit snes w/ 4-bit luma
		/// </summary>
		/// <param name="bmp">input/output bit-map</param>
		public static unsafe void Snes15BitRgb(ref Bitmap bmp)
		{
			int width = bmp.Width;
			int height = bmp.Height;

			BitmapData d = bmp.LockBits(new Rectangle(0, 0, width, height),
				ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);

			int* ptr = (int*)d.Scan0 - 1;
			int size = width * height;

			for (int x = 0; x < size; ++x)
			{
				if (*++ptr != 0)
				{
					byte* p = (byte*)ptr;
					*p = SnesBitChannel(*p++);
					*p = SnesBitChannel(*p++);
					*p = SnesBitChannel(*p++);
				}
			}
			bmp.UnlockBits(d);
		}

		public static double cgadsubTolerance = 2; // [0,8]
		public static int maxCgasubTransparency = 224; // [0,255]

		/// <summary>
		/// Converts a bitmap to CGADSUB mode.
		/// </summary>
		/// <param name="bmp">input/output bit-map</param>
		public static unsafe void CGADSUB(ref Bitmap bmp)
		{
			int width = bmp.Width;
			int height = bmp.Height;

			BitmapData d = bmp.LockBits(new Rectangle(0, 0, width, height),
				ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);

			int* ptr = (int*)d.Scan0 - 1;
			int size = width * height;

			for (int x = 0; x < size; ++x)
			{
				byte* p = (byte*)++ptr;
				byte b = *(p + 0);
				byte g = *(p + 1);
				byte r = *(p + 2);
				byte a = *(p + 3);

				if (a == 0)
				{
					b = g = r = 0;
				}
				else if (a <= maxCgasubTransparency)
				{
					double s = a / cgadsubTolerance;

					r = (byte)(r * s / 255.0);
					g = (byte)(g * s / 255.0);
					b = (byte)(b * s / 255.0);
					a = 255;

				}
				else
				{
					a = 255;
				}

				*(p + 0) = b;
				*(p + 1) = g;
				*(p + 2) = r;
				*(p + 3) = a;
			}
			bmp.UnlockBits(d);
		}
        public static byte[] PaletteToPal(Color[] colors, int size)
        {
            byte[] output = new byte[768];
			int len = Math.Min(colors.Length, 256); //Math.Min(256, size));
            for (int x = 0, ptr = 0 - 1; x < len; ++x)
            {
                output[++ptr] = SnesBitChannel(colors[x].R);
                output[++ptr] = SnesBitChannel(colors[x].G);
                output[++ptr] = SnesBitChannel(colors[x].B);
            }
            return output;
        }

        public static byte[] PaletteToTpl(Color[] colors, int size)
        {
            byte[] output = new byte[512 + 4];
            output[0] = (byte)'T';
            output[1] = (byte)'P';
            output[2] = (byte)'L';
            output[3] = 0x02;

			int len = Math.Min(colors.Length, 256);//Math.Min(256, size));

            for (int x = 0, ptr = 4 - 1; x < len; ++x)
            {
                byte[] col = ColorToSnes(colors[x]);
                output[++ptr] = col[0];
                output[++ptr] = col[1];
            }
            return output;
        }

		public static byte[] SnesASM(byte[] mw3Data)
		{
			StringBuilder output = new StringBuilder();

			for (int row = 0; row < 16; ++row)
			{
				output.AppendFormat("dw ", row);
				for (int line = 0; line < 16; ++line)
				{
					output.AppendFormat("${0:X4},", mw3Data[((row << 4) + line) * 2]
						| (mw3Data[((row << 4) + line) * 2 + 1] << 8));
				}
				output.Remove(output.Length - 1, 1);
				output.AppendFormat(" ; Row {0:X}\r\n", row);
			}

			return Encoding.UTF8.GetBytes(output.ToString());
		}
		public static byte[] SnesBIN(byte[] mw3Data)
		{
			//Array.Resize<byte>(ref mw3Data, 512);
			// TO DO
			Array.Resize(ref mw3Data, 256);
			return mw3Data;
		}

        public static byte[] PaletteToMw3(Color[] colors, int size)
        {
            byte[] output = new byte[512 + 2];
			int len = Math.Min(colors.Length, 256);//Math.Min(256, size));

            for (int x = 0, ptr = 0 - 1; x < len; ++x)
            {
                byte[] col = ColorToSnes(colors[x]);
                output[++ptr] = col[0];
                output[++ptr] = col[1];
            }
            return output;
        }

        public static byte[] ColorToSnes(Color color)
        {
            int col = (color.R >> 3) | (color.G >> 3 << 5) | (color.B >> 3 << 10);
            return new[] { (byte)(col & 0xFF), (byte)(col >> 8) };
        }

        /// <summary>
        /// Start a process with window hidden and waits to exit.
        /// </summary>
        /// <param name="name">application name</param>
        /// <param name="arguments">arguments</param>
        public static void StartProcess(String name, String arguments)
        {
            System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo(name);
            startInfo.WindowStyle = ProcessWindowStyle.Hidden;
            startInfo.Arguments = arguments;
            System.Diagnostics.Process p = Process.Start(startInfo);
            p.WaitForExit();
        }

        /// <summary>
        /// Converts a bitmap to a 8-bit 8x8 align RAW BIT-MAP.
        /// Also scales and specific maxColors.
        /// </summary>
        /// <param name="bitmap">Bitmap to convert</param>
        /// <param name="maxColors">Maximiuim colors</param>
        /// <param name="scale">Bitmap scaling. 1 = 100%/current</param>
        /// <returns>A array that contains: 8x8 align block RAW BIT-MAP and RAW YY-CHR Palette</returns>
        public static void BitmapToRAWSNES(Bitmap bitmap, int maxColors, double scale,
            out byte[] finalBitmap, out byte[] finalPaletteOk, out string paletteExtension, out Color[] finalPalette)
        {
			PrepareBitmap(ref bitmap, scale);
			if (Options.CGADSUB) CGADSUB(ref bitmap);
            if (Options.HiQuality) Snes15BitRgb(ref bitmap);

            if (Options.OptimizeImage)
            {
                using (var bmpData = new MemoryStream())
                {
                    bitmap.Save(bmpData, ImageFormat.Png);
                    bitmap.Dispose();
                    bitmap = null;
                    File.WriteAllBytes("tmp.png", bmpData.ToArray());

                    string args = "--force --ext n.png --speed 1 --verbose --transbug {0} -- tmp.png";

                    var startInfo = new ProcessStartInfo(@".\pngquant.exe");
                    startInfo.UseShellExecute = false;
                    startInfo.ErrorDialog = false;
                    startInfo.RedirectStandardError = false;
                    startInfo.RedirectStandardInput = false;
                    startInfo.RedirectStandardOutput = false;
                    startInfo.CreateNoWindow = true;
                    startInfo.Arguments = string.Format(args, maxColors);

                    var p = Process.Start(startInfo);
                    p.WaitForExit();
                    if (p.ExitCode != 0 && p.ExitCode != 0x10)
                    {
                        string err = p.ExitCode.ToString("X8");
                        p.Dispose();
                        throw new Exception("pngquant has quit with exit code 0x"
                            + err + ".");
                    }
                    p.Dispose();

                    if (!File.Exists("tmpn.png"))
                    {
                        throw new Exception("Couldn't get output image from pngquant.");
                    }

                    using (FileStream fs = new FileStream("tmpn.png", FileMode.Open))
                    {
                        bitmap = new Bitmap(fs);
                    }
                }

                //using (var bmpData = new MemoryStream())
                //{
                //    string args = "--speed 1 {0}";

                //    bitmap.Save(bmpData, ImageFormat.Png);
                //    bitmap.Dispose();
                //    bitmap = null;
                
                //tryAgain:
                //    var startInfo = new ProcessStartInfo(@".\pngquant.exe");
                //    startInfo.UseShellExecute = false;
                //    startInfo.ErrorDialog = false;
                //    startInfo.RedirectStandardInput = true;
                //    startInfo.RedirectStandardOutput = true;
                //    startInfo.CreateNoWindow = true;
                //    startInfo.Arguments = string.Format(args, maxColors);

                //    var p = Process.Start(startInfo);
 
                //    var stdIn = p.StandardInput.BaseStream;
                    
                //    bmpData.Position = 0;
                //    stdIn.Write(bmpData.ToArray(), 0, (int)bmpData.Length);
                //    stdIn.WriteByte((byte)'\r');
                //    stdIn.WriteByte((byte)'\n');

                //    var stdOut = p.StandardOutput.BaseStream;
                    
                //    bitmap = new Bitmap(stdOut);

                //    p.WaitForExit();
                    
                //    if (p.ExitCode == 99)
                //    {
                //        args = "--speed 1 {0}";
                //        goto tryAgain;
                //    }
                //}
            }

            int width = bitmap.Width;
            int height = bitmap.Height;
            byte[] rawBitmap = null;
            PixelFormat pixelFormat = bitmap.PixelFormat;
            BitmapData bitmapData = bitmap.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.ReadOnly, pixelFormat);
            Color[] newPalette = null;
            int bytes = Math.Abs(bitmapData.Stride) * height;

            switch (pixelFormat)
            {
                case PixelFormat.Format1bppIndexed:
                    rawBitmap = new byte[bytes * 8];
                    byte* ptr = (byte*)bitmapData.Scan0;
                    int x = 0;
                    while (x < bytes)
                    {
                        rawBitmap[(x << 3) + 0] = (byte)((ptr[x] & 128) >> 7);
                        rawBitmap[(x << 3) + 1] = (byte)((ptr[x] & 64) >> 6);
                        rawBitmap[(x << 3) + 2] = (byte)((ptr[x] & 32) >> 5);
                        rawBitmap[(x << 3) + 3] = (byte)((ptr[x] & 16) >> 4);
                        rawBitmap[(x << 3) + 4] = (byte)((ptr[x] & 8) >> 3);
                        rawBitmap[(x << 3) + 5] = (byte)((ptr[x] & 4) >> 2);
                        rawBitmap[(x << 3) + 6] = (byte)((ptr[x] & 2) >> 1);
                        rawBitmap[(x << 3) + 7] = (byte)(ptr[x++] & 1);
                    }
                    newPalette = bitmap.Palette.Entries;
                    break;

                case PixelFormat.Format4bppIndexed:
                    rawBitmap = new byte[bytes * 2];
                    ptr = (byte*)bitmapData.Scan0;
                    x = 0;
                    while (x < bytes)
                    {
                        rawBitmap[x << 1] = (byte)((ptr[x] & 0xf0) >> 4);
                        rawBitmap[(x << 1) + 1] = (byte)(ptr[x++] & 0xf);
                    }
                    newPalette = bitmap.Palette.Entries;
                    break;

                case PixelFormat.Format8bppIndexed:
                    rawBitmap = new byte[bytes];
                    Marshal.Copy(bitmapData.Scan0, rawBitmap, 0, bytes);
                    newPalette = bitmap.Palette.Entries;
                    break;

                case PixelFormat.Format24bppRgb:
                    newPalette = new Color[256];
                    ptr = (byte*)bitmapData.Scan0;
                    try
                    {
                        rawBitmap = new byte[bytes / 3];
                        x = 0;
                        int count = 0;

                        while (x < bytes)
                        {
                            Color temp = Color.FromArgb(
                                SnesBitChannel(ptr[x * 3 + 2]),
                                SnesBitChannel(ptr[x * 3 + 1]),
                                SnesBitChannel(ptr[x * 3]));
                            var position = Array.IndexOf(newPalette, temp);

                            if (position == -1)
                            {
                                newPalette[count++] = temp;
                                rawBitmap[x++] = (byte)(count - 1);
                            }
                            else
                            {
                                rawBitmap[x++] = (byte)position;
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        if (!Options.OptimizeImage)
                        {
                            throw new NullReferenceException("This image contains too many colors.", e);
                        }
                    }
                    break;

                case PixelFormat.Format32bppArgb:
                    newPalette = new Color[256];
                    ptr = (byte*)bitmapData.Scan0;
                    try
                    {
                        rawBitmap = new byte[bytes >>= 2];
                        x = 0;
                        int count = 0;

                        while (x < bytes)
                        {
                            Color temp = Color.FromArgb(
                                ptr[x * 4 + 3],
                                SnesBitChannel(ptr[(x << 2) + 2]),
                                SnesBitChannel(ptr[(x << 2) + 1]),
                                SnesBitChannel(ptr[(x << 2)]));

                            var position = Array.IndexOf(newPalette, temp);

                            if (position == -1)
                            {
                                newPalette[count++] = temp;
                                rawBitmap[x++] = (byte)(count - 1);
                            }
                            else
                            {
                                rawBitmap[x++] = (byte)position;
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        if (!Options.OptimizeImage)
                        {
                            throw new NullReferenceException("This image contains too many colors.", e);
                        }
                    }
                    break;

                default:
                    throw new Exception("Unknown image format.");
            }

            TryDelete("tmp.png");
            TryDelete("tmpn.png");

            if (Options.OrderPalette)
            {
                ArrangeColors(ref rawBitmap, ref newPalette, maxColors);
            }

            //YY-CHR Palette
            //Tile Layer Pro Palette
            //Mario World Palette
            //No Palette Output

            byte[] palette = new byte[0];
            string ext = "";

            switch (Options.PaletteOutput)
            {
                case 0: palette = PaletteToPal(newPalette, maxColors); ext = ".pal"; break;
                case 1: palette = PaletteToTpl(newPalette, maxColors); ext = ".tpl"; break;
                case 2: palette = PaletteToMw3(newPalette, maxColors); ext = ".mw3"; break;
				case 3: palette = SnesASM(PaletteToMw3(newPalette, maxColors)); ext = ".asm"; break;
				case 4: palette = SnesBIN(PaletteToMw3(newPalette, maxColors)); ext = "_pal.bin"; break;
            }

            finalBitmap = rawBitmap;
            finalPalette = newPalette;
            finalPaletteOk = palette;
            paletteExtension = ext;
        }

        public static int SortColorsByHue(Color a, Color b)
        {
            if (a.IsEmpty) return 1;
            if (b.IsEmpty) return -1;
            int n = a.GetHue().CompareTo(b.GetHue());
            return n == 0 ? a.ToArgb().CompareTo(b.ToArgb()) : n;
        }
        public static int SortColorsByBrightness(Color a, Color b)
        {
            if (a.IsEmpty) return 1;
            if (b.IsEmpty) return -1;
            int n = a.GetBrightness().CompareTo(b.GetBrightness());
            return n == 0 ? a.ToArgb().CompareTo(b.ToArgb()) : n;
        }
        public static int SortColorsBySaturation(Color a, Color b)
        {
            if (a.IsEmpty) return 1;
            if (b.IsEmpty) return -1;
            int n = a.GetSaturation().CompareTo(b.GetSaturation());
            return n == 0 ? a.ToArgb().CompareTo(b.ToArgb()) : n;
        }

        /// <summary>
        /// Reorder all colors.
        /// </summary>
        /// <param name="transparency">transparency color</param>
        /// <param name="bitmap">RAW BIT-MAP</param>
        /// <param name="palette">palette of RAW BIT-MAP</param>
        public static void ArrangeColors(ref byte[] bitmap, ref Color[] palette, int maxColors)
        {
            List<Color> palette_list = new List<Color>(palette);

            switch (arrangeMethod)
            {
                case 0:
                    palette_list.Sort(SortColorsByHue);
                    break;
                case 1:
                    palette_list.Sort(SortColorsByBrightness);
                    break;
                case 2:
                    palette_list.Sort(SortColorsBySaturation);
                    break;
                case 3:
                    Forms.ColorSortManual form = new Forms.ColorSortManual(palette_list.ToArray());
                    form.ShowDialog();
                    if (form.cancelled)
                    {
                        throw new Exception("The operation was aborted by user.");
                    }
                    palette_list.Clear();
                    palette_list.AddRange(form.table);
                    break;
            }

            byte[] order = new byte[palette.Length];

            // finds the transparent color....
            if (Options.AllowTransparency)
            {
                for (int i = 0, j = palette.Length; i < j; i++)
                {
                    if (palette_list[i] == transparent)
                    {
                        palette_list.RemoveAt(i);
                        palette_list.Insert(0, transparent);
                        break;
                    }
                }
            }

            // detects the ordering....
            for (int i = 0, j = palette.Length; i < j; i++)
            {
                for (int x = 0; x < j; ++x)
                {
                    if (palette_list[i] == palette[x])
                    {
                        order[x] = (byte)i;
                        break;
                    }
                }
            }

            // re-order bitmap
            for (int i = 0, j = bitmap.Length; i < j; ++i)
            {
                bitmap[i] = order[bitmap[i]];
            }

			Color[] paletteListOffset = new Color[256];

            palette = null;
            palette = palette_list.ToArray();
            palette_list.Clear();
            palette_list = null;

			int mask = (1 << SnesGFX.AvaiableFormats[Settings.Default.Codec].BitsPerPixel) - 1;

			for (int x = 0, y = Options.OffsetPalette; x < 256; ++x, y = (y + 1) & 255)
			{
				if (x < palette.Length)
				{
					paletteListOffset[y] = palette[x];
				}
				else
				{
					paletteListOffset[y] = Color.Black;
				}
			}

			for (int i = 0; i < bitmap.Length; ++i)
			{
				bitmap[i] = (byte)((bitmap[i] + Options.OffsetPalette) & mask);
			}

			palette = paletteListOffset;
        }

        /// <summary>
        /// Converts a SNES/PACKED BIT-MAP to Bitmap.
        /// </summary>
        /// <param name="_buffer">The raw data</param>
        /// <param name="_palette">Palette</param>
        /// <param name="bpp">bits-per-pixel</param>
        /// <returns>The Bitmap</returns>
        public static Bitmap ConvertCustomToBitmap(byte[] _buffer, Color[] _palette, int bpp)
        {
            if (SnesGFX.AvaiableFormats[Settings.Default.Codec].Type != BitformatType.BITFORMAT_PLANAR)
            {
                return PackedDecode(_buffer, _palette, bpp);
            }

            long a8x8 = 0;
            long size = _buffer.Length;
            a8x8 = size / ((64 * bpp) / 8);
            int height = (int)((a8x8 / 16) * 8);
            int width = (int)((a8x8 > 16) ? 128 : a8x8 * 8);

            if (height == 0) height = 0;
            if (height % 8 != 0) height += 8 - (height % 8);

            Bitmap bmp = new Bitmap(width, height, PixelFormat.Format8bppIndexed);

            ColorPalette pal = bmp.Palette;
            int x = 0; while (x < _palette.Length) pal.Entries[x] = _palette[x++];
            if (Options.AllowTransparency)
                pal.Entries[0] = Color.Transparent;
            bmp.Palette = pal;


            IBitformat format = SnesGFX.AvaiableFormats[Settings.Default.Codec];
            byte[] output = format.Decode(_buffer);

            var bmpData = bmp.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.ReadWrite,
                PixelFormat.Format8bppIndexed);

            IntPtr ptr = bmpData.Scan0;
            int bytes = Math.Abs(bmpData.Stride) * height;

            //fixed (byte* ptr2 = output)
            //{
            //    byte[] rgbValues = SuperFX.ExGraphics.a8x8ToTilemap(ptr2, width, codec.output.Length);
            //    Marshal.Copy(rgbValues, 0, ptr, bytes);
            //    rgbValues = null;
            //}

            Marshal.Copy(output, 0, ptr, bytes);

            bmp.UnlockBits(bmpData);

            return bmp;
        }

        /// <summary>
        /// Packed Bitmap decoder. Calls from ConvertCustomToBitmap if the bitmap is packed, not snes.
        /// </summary>
        /// <param name="_buffer">Bitmap data</param>
        /// <param name="_palette">palette data</param>
        /// <param name="bpp">bits-per-pixel</param>
        /// <returns>Bitmap</returns>
        private static Bitmap PackedDecode(byte[] _buffer, Color[] _palette, int bpp)
        {
            IBitformat format = SnesGFX.AvaiableFormats[Settings.Default.Codec];

            // let's get the width
            int width = bitmapInfo.Width;
            int height = _buffer.Length;

            if (format.Type == BitformatType.BITFORMAT_PACKED)
            {
                height = height * 8 / format.BitsPerPixel;
            }

            height /= width;

            //int packedtype;

            //if (!Options.PreserveWidth)
            //{
            //    width = 128;
            //}

            //if (Options.PackPacked)
            //{
            //    switch (Settings.Default.Codec)
            //    {
            //        case 4:
            //            packedtype = 2;
            //            height *= 4;
            //            break;

            //        case 5:
            //            packedtype = 4;
            //            height *= 2;
            //            break;

            //        case 6:
            //        case 7:
            //            packedtype = 8;
            //            break;

            //        default:
            //            packedtype = 2;
            //            height /= 4;
            //            break;
            //    }
            //}
            //else
            //{
            //    packedtype = 8;
            //}

            //height /= width;

            Bitmap bmp = new Bitmap(width, height, PixelFormat.Format8bppIndexed);

            ColorPalette pal = bmp.Palette;
            int x = 0;
            while (x < _palette.Length)
            {
                pal.Entries[x] = _palette[x++];
            }
            bmp.Palette = pal;

            byte[] output = format.Decode(_buffer);

            //ExGraphics.CodecInfo codec = new SuperFX.ExGraphics.CodecInfo(-1);
            //codec.Decode = true;
            //codec.SysName = string.Format("Pack{0}BPP", packedtype);
            //codec.Data = _buffer;
            //ExGraphics.StartCodec = codec;

            var bmpData = bmp.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.ReadWrite,
                PixelFormat.Format8bppIndexed);

            IntPtr ptr = bmpData.Scan0;
            int bytes = Math.Abs(bmpData.Stride) * height;
            Marshal.Copy(output, 0, ptr, bytes);
            bmp.UnlockBits(bmpData);
            return bmp;
        }

        /// <summary>
        /// Converts a RAW YY-CHR Palette to Color Array
        /// </summary>
        /// <param name="_buffer">RAW YY-CHR Palette</param>
        /// <param name="pal">number of colors or something</param>
        /// <returns>ColorArray</returns>
        public static Color[] RAWYYCHRToColorArray(byte[] _buffer, int pal)
        {
            Color[] output = new Color[256];

            if (pal == 0)
            {
                for (int x = 0; x < 256; x++)
                {
                    output[x] = Color.FromArgb(
                        _buffer[(x * 3) + (pal * 16 * 3)],
                        _buffer[(x * 3) + 1 + (pal * 16 * 3)],
                        _buffer[(x * 3) + 2 + (pal * 16 * 3)]);
                }
            }
            else
            {
                for (int x = 0; x < 16; x++)
                {
                    output[x] = Color.FromArgb(
                        _buffer[(x * 3) + (pal * 16 * 3)],
                        _buffer[(x * 3) + 1 + (pal * 16 * 3)],
                        _buffer[(x * 3) + 2 + (pal * 16 * 3)]);
                }
            }
            return output;
        }

        /// <summary>
        /// Scales a Bitmap, preserving aspect ratio.
        /// </summary>
        /// <param name="imgPhoto">Input Bitmap</param>
        /// <param name="size">New Size</param>
        /// <returns>New Bitmap</returns>
        public static Bitmap ScaleRatio(Bitmap imgPhoto, Size size)
        {
            int sourceWidth = imgPhoto.Width;
            int sourceHeight = imgPhoto.Height;
            int sourceX = 0;
            int sourceY = 0;
            int destX = 0;
            int destY = 0;

            float nPercent = 0;
            float nPercentW = 0;
            float nPercentH = 0;

            nPercentW = (size.Width / (float)sourceWidth);
            nPercentH = (size.Height / (float)sourceHeight);
            if (nPercentH < nPercentW)
            {
                nPercent = nPercentH;
                destX = Convert.ToInt16((size.Width -
                              (sourceWidth * nPercent)) / 2);
            }
            else
            {
                nPercent = nPercentW;
                destY = Convert.ToInt16((size.Height -
                              (sourceHeight * nPercent)) / 2);
            }

            int destWidth = (int)(sourceWidth * nPercent);
            int destHeight = (int)(sourceHeight * nPercent);

            Bitmap bmPhoto = new Bitmap(size.Width, size.Height,
                              PixelFormat.Format32bppArgb);

            using (Graphics grPhoto = Graphics.FromImage(bmPhoto))
            {
                grPhoto.InterpolationMode = InterpolationMode.NearestNeighbor;

                for (int y = 0; y < size.Height; y += 8)
                {
                    for (int x = 0; x < size.Width; x += 8)
                    {
                        if ((x + y & 8) == 0)
                        {
                            grPhoto.FillRectangle(Brushes.White, x, y, 8, 8);
                        }
                        else
                        {
                            grPhoto.FillRectangle(Brushes.Gray, x, y, 8, 8);
                        }
                    }
                }

                grPhoto.DrawImage(imgPhoto,
                    new Rectangle(destX, destY, destWidth, destHeight),
                    new Rectangle(sourceX, sourceY, sourceWidth, sourceHeight),
                    GraphicsUnit.Pixel);
            }

            return bmPhoto;
        }
    }
}