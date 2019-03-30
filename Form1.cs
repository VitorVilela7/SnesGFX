using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;
using SnesGFX.Properties;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace SnesGFX
{
    /// <summary>
    /// Classe do Form principal
    /// </summary>
    public partial class Main : Form
    {
        /// <summary>
        /// Inicializadora do Windows Forms. Requer a execução do Program.Main();
        /// </summary>
        public Main()
        {
            InitializeComponent();
            this.Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
            this.comboBox3.Enabled = false;
			for (int i = 0x3FF; i >= 0; --i)
			{
				this.domainUpDown1.Items.Add(i.ToString("X"));
			}
			for (int i = 0xFF; i >= 0; --i)
			{
				this.domainUpDown2.Items.Add(i.ToString("X2"));
			}
            LoadSettings();
            Global_DetectCodecs();
        }

        /// <summary>
        /// Rotina de detectar os codecs.
        /// </summary>
        private void Global_DetectCodecs()
        {
            this.comboBox1.BeginUpdate();

            foreach (IBitformat format in SnesGFX.AvaiableFormats)
            {
                this.comboBox1.Items.Add(format.Name);
            }

            this.comboBox1.EndUpdate();
            this.comboBox1.SelectedIndex = Settings.Default.Codec;
        }

        /// <summary>
        /// Detecta as quantidades de cores que um codec suporta.
        /// </summary>
        private void Ripper_DetectColors()
        {
            int maxColors = SnesGFX.AvaiableFormats[comboBox1.SelectedIndex].Colors;

            this.comboBox2.BeginUpdate();
            this.comboBox2.Items.Clear();

            for (int x = 2; x <= maxColors; x++)
                this.comboBox2.Items.Add(x);

            this.comboBox2.SelectedIndex = this.comboBox2.Items.Count - 1;
            this.comboBox2.EndUpdate();
        }

        /// <summary>
        /// Função de interação com o Windows ~~ Importante para loops
        /// </summary>
        private void InteragirWindows()
        {
            while (InteragirFlag)
            {
                Application.DoEvents();
                Thread.Sleep(0);
				Thread.Sleep(1);
            }
        }

        private bool InteragirFlag = false;

        private void EnableOrDisable(bool option)
        {
            button5.Enabled = button2.Enabled = button3.Enabled = option;
        }

        /// <summary>
        /// Rotina de conversão de Imagens para SNES Graphics
        /// </summary>
		private void Ripper_ProcessSystem(bool preview = false)
		{
			StringBuilder report = new StringBuilder();

			try
			{
				if (!init) return;
				EnableOrDisable(false);
				InteragirFlag = true;
				Application.UseWaitCursor = true;
				new Thread(new ThreadStart(InteragirWindows)).Start();

				if (textBox1.Tag == null)
				{
					goto End;
				}

				foreach (var file in (string[])textBox1.Tag)
				{

					string name = Regex.Replace(file.Replace(@"\", @"\\"), "[.]\\w{3,4}", "", Program.ropts);

					if (!Options.SaveOnImageFolder && name.LastIndexOf("\\") != -1)
					{
						name = name.Substring(name.LastIndexOf("\\") + 1);
					}

					Bitmap b = null;

					using (FileStream fs = new FileStream(file, FileMode.Open))
					{
						b = new Bitmap(fs);
						fs.Close();

						Program.bitmapInfo = b.Size;
					}

					double scale = (!checkBox3.Checked) ? 1 : (Int32.Parse(textBox2.Text) / 100D);

					PAR(ref b);

					byte[] result;
					byte[] palette;
					Color[] memPalette;
					string extension;
					byte[] mwl = null;

					Program.BitmapToRAWSNES(b, this.comboBox2.SelectedIndex + 2, scale,
						out result, out palette, out extension, out memPalette);

					b.Dispose();

					IBitformat codec = SnesGFX.AvaiableFormats[comboBox1.SelectedIndex];
					byte[] outputTilemap = null;

					if (Options.RemoveDuplicateTiles)
					{
						int[] tilemap;
						bool[] flipx, flipy;
						int theWidth = codec.FixedWidth == 0 ? Program.bitmapInfo.Width :
							codec.FixedWidth;

						SNES.Tile[] tiles = SNES.Tile.FromBitmap(result, theWidth);
						SNES.Tile[] aTiles = SNES.Tile.RemoveRepeatedBlocks(tiles, Options.RemoveFlippedTiles,
							out tilemap, out flipx, out flipy);
						int nTiles = aTiles.Length;
						// This is needed to some lines don't get cut-off.
						if (nTiles % 0x10 != 0) nTiles += 0x10 - nTiles % 0x10;
						SNES.Tile[] rTiles = new SNES.Tile[nTiles];
						aTiles.CopyTo(rTiles, 0);
						for (int i = aTiles.Length; i < nTiles; ++i) { rTiles[i] = new SNES.Tile(new byte[64]); }
						result = SNES.Tile.ConvertToBitmap(rTiles, theWidth);

						int x_flip_count = 0;
						int y_flip_count = 0;
						int xy_flip_count = 0;

						for (int i = 0; i < flipx.Length; ++i)
						{
							if (flipx[i] && flipy[i])
							{
								++xy_flip_count;
								continue;
							}
							if (flipx[i])
							{
								++x_flip_count;
								continue;
							}
							if (flipy[i])
							{
								++y_flip_count;
								continue;
							}
						}

						string msg =
	@"Total Tiles Before Removing Duplicates:		0x{0:X4}
Total Tiles After Removing Duplicates:		0x{1:X4}

Total X Flipped Tiles:				0x{3:X4}
Total Y Flipped Tiles:				0x{4:X4}
Total X+Y Flipped Tiles:			0x{5:X4}
Total Duplicated Tiles:			0x{6:X4}
Total Removed:				0x{7:X4}

Ratio:					{2:F}%";

						msg = string.Format(CultureInfo.InvariantCulture,
					msg, tiles.Length, rTiles.Length, rTiles.Length / (double)tiles.Length * 100,
					x_flip_count, y_flip_count, xy_flip_count, tiles.Length - rTiles.Length -
					x_flip_count - y_flip_count - xy_flip_count, tiles.Length - rTiles.Length);

						report.AppendLine(name);
						report.AppendLine(msg);
						report.AppendLine();

						if (((string[])textBox1.Tag).Length == 1)
						{
							MessageBox.Show(msg, "Anti-Tile Duplicate Results",
								MessageBoxButtons.OK, MessageBoxIcon.Information);
						}

						for (int i = 0; i < tilemap.Length; ++i)
						{
							tilemap[i] = (tilemap[i] + Options.OffsetTile) & 0x3FF;
						}

						if (rTiles.Length > 1024 && Options.TilemapOutput != 3)//Options.GenerateMap16)
						{
							MessageBox.Show("There are more than 1024 (0x400) tiles, thus " +
								"it's impossible to generate a SNES tilemap.", "Error",
								MessageBoxButtons.OK, MessageBoxIcon.Error);
							goto End;
						}
						else if (Options.TilemapOutput == 0)
						{
							int size = tilemap.Length * 2;
							// 2kB o caralho
							//if (size % 0x800 != 0)
							//{
							//	size += 0x800 - size % 0x800;
							//}
							if (size <= 0x780)
							{
								if (size % 0x780 != 0)
								{
									size += 0x780 - size % 0x780;
								}
							}
							else
							{
								if (size % 0x800 != 0)
								{
									size += 0x800 - size % 0x800;
								}
							}
							outputTilemap = new byte[size];

							for (int i = 0; i < tilemap.Length; ++i)
							{
								int item = tilemap[i] & 0x3FF;
								item += flipy[i] ? 0x8000 : 0;
								item += flipx[i] ? 0x4000 : 0;

								//int mIndex = i;// &~(0x20 | 0x40);
								//mIndex |= (i & 0x20) << 1;
								//mIndex |= (i & 0x40) >> 1;
								//mIndex <<= 1;

								int ii = (i & 0x0F) | ((i & ~0x0F) << 1);
								ii &= 0x3FF;

								ii |= i & ~0x3FF;
								ii |= (i & 0x200) >> 5;

								int mIndex = ((ii & 0x1F) << 1) | (ii & ~0x3F) | ((ii & 0x20) >> 5);
								mIndex <<= 1;

								if (mIndex >= size) continue;

								outputTilemap[mIndex] = (byte)(item & 0xFF);
								outputTilemap[mIndex + 1] = (byte)(item >> 8 & 0xFF);
							}
						}
						else if (Options.TilemapOutput == 1)
						{
							int size = tilemap.Length * 2;
							if (size % 0x800 != 0)
							{
								size += 0x800 - size % 0x800;
							}
							outputTilemap = new byte[size];

							for (int i = 0; i < tilemap.Length; ++i)
							{
								int item = tilemap[i] & 0x3FF;
								item += flipy[i] ? 0x8000 : 0;
								item += flipx[i] ? 0x4000 : 0;

								//int mIndex = i;// &~(0x20 | 0x40);
								//mIndex |= (i & 0x20) << 1;
								//mIndex |= (i & 0x40) >> 1;
								//mIndex <<= 1;

								int ii = (i & 0x0F) | ((i & ~0x0F) << 1);
								ii &= 0x3FF;

								ii |= i & ~0x3FF;
								ii |= (i & 0x200) >> 5;

								int mIndex = ((ii & 0x1F) << 1) | (ii & ~0x3F) | ((ii & 0x20) >> 5);
								mIndex <<= 1;

								if (mIndex >= size) continue;

								outputTilemap[mIndex] = (byte)(item & 0xFF);
								outputTilemap[mIndex + 1] = (byte)(item >> 8 & 0xFF);
							}

							List<int[]> m16opt = new List<int[]>();
							int[] m16index = new int[outputTilemap.Length >> 3];

							for (int i = 0; i < outputTilemap.Length; i += 8)
							{
								int[] group = new int[4];
								group[0] = outputTilemap[i] | (outputTilemap[i + 1] << 8);
								group[1] = outputTilemap[i + 2] | (outputTilemap[i + 3] << 8);
								group[2] = outputTilemap[i + 4] | (outputTilemap[i + 5] << 8);
								group[3] = outputTilemap[i + 6] | (outputTilemap[i + 7] << 8);

								int x = m16opt.FindIndex(p => p[0] == group[0] &&
									p[1] == group[1] && p[2] == group[2] && p[3] == group[3]);

								if (x != -1)
								{
									m16index[i >> 3] = x;
								}
								else
								{
									m16index[i >> 3] = m16opt.Count;
									m16opt.Add(group);
								}
							}

							size = m16opt.Count * 8;
							if (size % 0x800 != 0)
							{
								size += 0x800 - size % 0x800;
							}
							outputTilemap = new byte[size];

							for (int i = 0; i < m16opt.Count; ++i)
							{
								outputTilemap[(i << 3) + 0] = (byte)(m16opt[i][0]);
								outputTilemap[(i << 3) + 1] = (byte)(m16opt[i][0] >> 8);
								outputTilemap[(i << 3) + 2] = (byte)(m16opt[i][1]);
								outputTilemap[(i << 3) + 3] = (byte)(m16opt[i][1] >> 8);
								outputTilemap[(i << 3) + 4] = (byte)(m16opt[i][2]);
								outputTilemap[(i << 3) + 5] = (byte)(m16opt[i][2] >> 8);
								outputTilemap[(i << 3) + 6] = (byte)(m16opt[i][3]);
								outputTilemap[(i << 3) + 7] = (byte)(m16opt[i][3] >> 8);
							}

							// assuming the map16 will get stored at page 0x42...

							//FuSoYa:
							//"Base mwl file attached.  This file is already set to use a custom
							//palette and custom BG.  Insert the BG tilemap at offset 0xD6 (0x800
							//bytes, 16 bit values little endian).  The upper 4 bits in the byte at
							//offset 0xCE should be set to the Map16 bank you want to use (0-3),
							//while the lower 4 bits should not be changed.  Offset 0x8E8 is the
							//palette table (SNES format, 16 bit values little endian, 0x202 bytes
							//as there's an extra entry at the end for the back area color)."

							if (rTiles.Length > 0x300)
							{
								MessageBox.Show("There are more than 768 (0x300) tiles. It means it won't fit even if you " +
									"use all LM slots (FG1-3; BG1-3). Conversion aborted.", "Error",
									MessageBoxButtons.OK, MessageBoxIcon.Error);
								goto End;
							}

							mwl = Resources._base;

							// we need to deinterleave the gfx "blocks" or it will look weird on LM
							int w = Program.bitmapInfo.Width;
							int h = Program.bitmapInfo.Height;
							int h2 = h;
							if (w % 128 != 0) w += 128 - w % 128;
							if (h % 256 != 0) h += 256 - h % 256;

							if (w > 512 || h > 512)
							{
								MessageBox.Show("The output image (" + w + "x" + h + ") is larger than 512x512! Conversion aborted.",
									"Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
								goto End;
							}

							for (int y = 0; y < h; y += 16)
							{
								for (int x = 0; x < w; x += 16)
								{
									// destination: there are 0x400 blocks, each one of 16x16
									// format is, well:
									// XYyyyyxxxx

									int dest = (x >> 4 & 15) | ((y >> 4 & 15) << 4) | ((x >> 8 & 1) << 9) | ((y >> 8 & 1) << 8);
									dest <<= 1;
									dest += 0xD6;

									// source: this one is more fun.
									// format:
									// ...PP pyyyYxxx
									// and it's offset based.
									// oh well.

									if (y >> 4 >= h2 >> 4) continue;

									int fakeY = (y >> 4) + (x >> 7) * (h2 >> 4);
									int source = (x >> 4 & 7) | ((fakeY & 15) << 4) | ((fakeY >> 4 & 1) << 3) | ((fakeY >> 5) << 8);// | ((offset >> 3 & 7) << 4) | ((offset >> 6 & 1) << 3) | ((offset >> 7) << 7);

									if (source >= m16index.Length) continue;

									int m16 = 0x0200 + m16index[source];
									mwl[dest] = (byte)m16;
									mwl[dest + 1] = (byte)(m16 >> 8);
								}
							}

							byte[] mw3 = Program.PaletteToMw3(memPalette, 256);
							mw3[512] = mw3[0];
							mw3[513] = mw3[1];
							mw3[0] = 0;
							mw3[1] = 0;
							mw3.CopyTo(mwl, 0x8E8);
						}
						else if (Options.TilemapOutput == 2)
						{

							// we need to deinterleave the gfx "blocks" or it will look weird on LM
							int w = Program.bitmapInfo.Width;
							int h = Program.bitmapInfo.Height;
							int h2 = h;
							if (w % 128 != 0) w += 128 - w % 128;
							if (h % 256 != 0) h += 256 - h % 256;

							if (w > 512 || h > 512)
							{
								MessageBox.Show("The output image (" + w + "x" + h + ") is larger than 512x512! Conversion aborted.",
									"Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
								goto End;
							}

							//brrrrrrrrrrrrrrrrrrrrrrrrrr!
							if (w <= 256 && h <= 256)
							{
								//outputTilemap = new byte[32 * 32 * 2];
								// attention TO DO
								outputTilemap = new byte[0x780];
							}
							else if (w > 256 && h <= 256)
							{
								outputTilemap = new byte[64 * 32 * 2];
							}
							else
							{
								outputTilemap = new byte[64 * 64 * 2];
							}

							for (int y = 0; y < h; y += 8)
							{
								for (int x = 0; x < w; x += 8)
								{
									// destination: there are 0x1000 blocks, each one of 8x8
									// format is, well:
									// XYyy yyyx xxxx

									int dest = (x >> 3 & 31) | ((y >> 3 & 31) << 5) | ((x >> 8 & 1) << 10) | ((y >> 8 & 1) << 11);
									dest <<= 1;


									// pp pppp xxxx

									if (y >> 3 >= h2 >> 3) continue;

									int fakeY = (y >> 3) + (x >> 7) * (h2 >> 3);
									int source = (x >> 3 & 15) | (fakeY << 4);

									if (source >= tilemap.Length) continue;

									int item = tilemap[source] & 0x3FF;
									item += flipy[source] ? 0x8000 : 0;
									item += flipx[source] ? 0x4000 : 0;

									outputTilemap[dest] = (byte)(item & 0xFF);
									outputTilemap[dest + 1] = (byte)(item >> 8 & 0xFF);
								}
							}

						}
						else if (Options.TilemapOutput == 3 || Options.TilemapOutput == 4)
						{
							// mode 7

							int w = Program.bitmapInfo.Width;
							int h = Program.bitmapInfo.Height;
							int h2 = h;
							if (w % 128 != 0) w += 128 - w % 128;
							if (h % 256 != 0) h += 256 - h % 256;

							if (w > 1024 || h > 1024)
							{
								MessageBox.Show("The output image (" + w + "x" + h + ") is larger than 1024x1024! Conversion aborted.",
									"Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
								goto End;
							}

							if (rTiles.Length > 0x100)
							{
								MessageBox.Show("There are more than 256 (0x100) tiles. It won't fit on a Mode 7 Character Data, " +
									"which is limited to 128x128. Conversion aborted.", "Error",
									MessageBoxButtons.OK, MessageBoxIcon.Error);
								goto End;
							}

							outputTilemap = new byte[128 * 128];

							for (int y = 0; y < h; y += 8)
							{
								for (int x = 0; x < w; x += 8)
								{
									// destination: there are 0x1000 blocks, each one of 8x8
									// format is, well:
									// XYyy yyyx xxxx

									// format:
									// yy yyyy yxxx xxxx

									int dest = (x >> 3 & 1023) | ((y >> 3 & 1023) << 7);

									// pp pppp xxxx

									if (y >> 3 >= h2 >> 3) continue;

									int fakeY = (y >> 3) + (x >> 7) * (h2 >> 3);
									int source = (x >> 3 & 15) | (fakeY << 4);

									if (source >= tilemap.Length) continue;

									if (flipy[source] || flipx[source])
									{
										MessageBox.Show("Mode 7 can't flip tiles! Please disable \"Remove Flipped Tiles\"" +
											"option and try again. Conversion aborted.", "Error",
											MessageBoxButtons.OK, MessageBoxIcon.Error);
										goto End;
									}

									outputTilemap[dest] = (byte)(tilemap[source] & 0xFF);
								}
							}
						}

					}

					byte[] output = codec.Encode(result);

					if (!preview)
					{
						int error_count = 0;
						while (true)
						{
							try
							{
								if (!Options.SplitOutput)
								{
									if (Options.TilemapOutput == 4)
									{
										// kk interleave mode
										byte[] final = new byte[0x8000];

										for (int x = 0, y = 0; x < 0x8000; x += 2, ++y)
										{
											final[x + 0] = outputTilemap[y];
											if (y < output.Length)
											{
												final[x + 1] = output[y];
											}
											else
											{
												final[x + 1] = 0;
											}
										}

										File.WriteAllBytes(name + ".bin", final);
									}
									else
									{
										File.WriteAllBytes(name + ".bin", output);
									}
								}
								else
								{
									if (Options.TilemapOutput == 4)
									{
										MessageBox.Show("Can't split output with Mode 7 Interleaved Tilemap!", "Error",
											MessageBoxButtons.OK, MessageBoxIcon.Error);
										goto End;
									}

									int maxSize = 1 << (Options.SplitOutputIndex + 9);
									int size = output.Length;
									int index = 0;
									int part = 0;

									while (index < size)
									{
										byte[] area = new byte[maxSize];
										if (index + maxSize < size)
										{
											Array.Copy(output, index, area, 0, maxSize);
										}
										else
										{
											Array.Copy(output, index, area, 0, size - index);
										}
										File.WriteAllBytes(name + "_part_" + part.ToString("X2") + ".bin", area);
										index += maxSize;
										++part;
									}
								}

								if (palette.Length != 0)
								{
									File.WriteAllBytes(name + extension, palette);
								}

								if (outputTilemap != null && Options.TilemapOutput != 4)
								{
									File.WriteAllBytes(name + "_map16.bin", outputTilemap);
								}

								if (mwl != null)
								{
									File.WriteAllBytes(name + ".mwl", mwl);
								}
								break;
							}

							catch (Exception e)
							{
								if (error_count++ >= 6)
								{
									MessageBox.Show(e.Message, "Error",
										MessageBoxButtons.OK, MessageBoxIcon.Error);
									goto End;
								}
							}
						}
					}
					else
					{
						Bitmap graphics = Program.ConvertCustomToBitmap(output,
							memPalette, codec.BitsPerPixel);

						if (Program.view == null)
						{
							Program.view = new Form2(graphics, new Size(256, 256));
							Program.view.Owner = this;
							Program.view.Show();
						}

						else if (Program.view.IsDisposed)
						{
							Program.view = new Form2(graphics, new Size(256, 256));
							Program.view.Owner = this;
							Program.view.Show();
						}
						else
							Program.view.CurrrentImage = graphics;
					}
				}
			}
			catch (NullReferenceException)
			{
				MessageBox.Show("This image has too many colors to fit on configuration. " +
					"Please enable \"optimize image\" option to reduce the number of colors.",
					"Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
			}
			catch (Exception e)
			{
				MessageBox.Show(e.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
				goto End;
			}

		End:
			Application.UseWaitCursor = false;
			EnableOrDisable(true);
			InteragirFlag = false;

			File.WriteAllText("convertlog.txt", report.ToString());
		}

		private void PAR(ref Bitmap b)
		{
			var output = new Bitmap(b.Width, b.Height, PixelFormat.Format24bppRgb);

			using (Graphics g = Graphics.FromImage(output))
			{
				g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
				g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
				g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;

				float ratio = 16 / 15f * 3 / 4f;

				var dest = new RectangleF((b.Width - b.Width * ratio) / 2f, 0, b.Width * ratio, b.Height);
				var src = new RectangleF(0, 0, b.Width, b.Height);

				g.DrawImage(b, dest, src, GraphicsUnit.Pixel);
			}

			b = output;
		}

		/// <summary>
		/// Mostra um OpenFileDialog
		/// </summary>
		/// <param name="sender">meh</param>
		/// <param name="e">meh</param>
		private void ShowDialog1(object sender, EventArgs e)
        {
            if (this.openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                try
                {
					foreach (var file in openFileDialog1.FileNames)
					{
						using (FileStream fs = new FileStream(file, FileMode.Open, FileAccess.Read))
						{
							new Bitmap(fs).Dispose();
						}
					}
                }

                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                button5.Enabled = button2.Enabled = button3.Enabled = true;
                textBox1.Tag = openFileDialog1.FileNames;
                textBox1.Text = openFileDialog1.FileName.Substring(openFileDialog1.FileName.LastIndexOf("\\") + 1);
            }
        }

        private void Ripper_ChangeCodec(object sender, EventArgs e)
        {
            Settings.Default.Codec = comboBox1.SelectedIndex;
            Settings.Default.Save();
            Ripper_DetectColors();
        }

        bool init = false;

        private void Main_Load(object sender, EventArgs e)
        {
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint, true);
            init = true;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            Ripper_ProcessSystem();
        }

        private void button5_Click(object sender, EventArgs e)
        {
            Ripper_ProcessSystem(true);
        }

        private void codecToolStripMenuItem_DropDownItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {
            Ripper_ChangeCodec(sender, e);
        }

        private void textBox2_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (!Char.IsNumber(e.KeyChar) && !Char.IsControl(e.KeyChar))
                e.Handled = true;
        }

        /// <summary>
        /// Hi Quality Change
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            Options.HiQuality = checkBox1.Checked;
        }

        /// <summary>
        /// Order Color Changed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void checkBox4_CheckedChanged(object sender, EventArgs e)
        {
            button6.Enabled = Options.OrderPalette = checkBox4.Checked;
        }

        /// <summary>
        /// ScaleImage Changed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Ripper_ScaleOptions_CheckedStateChanged(object sender, EventArgs e)
        {
            Options.ScaleOption = checkBox3.Checked;
            textBox2.Enabled = checkBox3.Checked;
            textBox2.Text = "100";
        }

        /// <summary>
        /// Load Settings
        /// </summary>
        private void LoadSettings()
        {
            checkBox4.Checked = Options.OrderPalette;
            checkBox1.Checked = Options.HiQuality;
            checkBox3.Checked = Options.ScaleOption;
            checkBox2.Checked = Options.RemoveDuplicateTiles;
            checkBox5.Checked = Options.OptimizeImage;
            checkBox6.Checked = Options.SplitOutput;
            checkBox8.Checked = Options.SaveOnImageFolder;
            checkBox7.Checked = Options.AllowTransparency;
			checkBox10.Checked = Options.CGADSUB;

            comboBox4.SelectedIndex = Options.TilemapOutput;
            comboBox5.SelectedIndex = Options.PaletteOutput;

            checkBox9.Checked = Options.RemoveFlippedTiles;

			int a = Options.OffsetTile;
			checkBox11.Checked = a != 0;
			domainUpDown1.SelectedIndex = a ^ 0x3FF;
			Options.OffsetTile = a;
			if (!checkBox11.Checked) { domainUpDown1.Enabled = false; domainUpDown1.SelectedIndex = 0x00 ^ 0x3FF; }

			a = Options.OffsetPalette;
			checkBox12.Checked = a != 0;
			domainUpDown2.SelectedIndex = a ^ 0xFF;
			Options.OffsetPalette = a;
			if (!checkBox12.Checked) { domainUpDown2.Enabled = false; domainUpDown2.SelectedIndex = 0x00 ^ 0xFF; }
        }

		private unsafe void button3_Click(object sender, EventArgs e)
		{
			if (textBox1.Tag == null)
			{
				goto error;
			}

			StringBuilder report = new StringBuilder();

			foreach (var file in (string[])textBox1.Tag)
			{
				if (file == "") goto error;
				if (!File.Exists(file)) goto error;

				byte[] colorBuffer = new byte[16777216];

				try
				{
					using (Bitmap bitmap = new Bitmap(file))
					{
						using (Bitmap pixel3 = new Bitmap(bitmap.Width, bitmap.Height, PixelFormat.Format32bppArgb))
						{
							using (Graphics g = Graphics.FromImage(pixel3))
							{
								g.DrawImage(bitmap, 0, 0, bitmap.Width, bitmap.Height);
							}

							BitmapData data = pixel3.LockBits(
								new Rectangle(0, 0, bitmap.Width, bitmap.Height),
								ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);

							int bitmapsize = Math.Abs(data.Stride * data.Height) / 4;
							uint* ptr = (uint*)data.Scan0;

							int* i_ptr = stackalloc int[1];
							int i = *i_ptr;

							// 16mb buffer for color detect
							Array.Clear(colorBuffer, 0, colorBuffer.Length);

							int colorcounter = 0;

							fixed (byte* p = colorBuffer)
							{
								while (i < bitmapsize)
									p[ptr[i++] & 0xffffff] = 1;

								pixel3.UnlockBits(data);
								data = null;

								i = 0;

								for (; i < 16777216; i++)
									if (p[i] == 1)
										colorcounter++;
							}

							Array.Clear(colorBuffer, 0, colorBuffer.Length);

							int result1 = colorcounter;

							Bitmap tmp = new Bitmap(pixel3);
							double scale = (!checkBox3.Checked) ? 1 : (Int32.Parse(textBox2.Text) / 100D);

							Program.PrepareBitmap(ref tmp, scale);
							if (Options.CGADSUB) Program.CGADSUB(ref tmp);
							if (Options.HiQuality) Program.Snes15BitRgb(ref tmp);

							data = tmp.LockBits(
		new Rectangle(0, 0, tmp.Width, tmp.Height),
		ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);

							bitmapsize = Math.Abs(data.Stride * data.Height) / 4;
							ptr = (uint*)data.Scan0;

							i = *i_ptr;
							
							colorcounter = 0;

							fixed (byte* p = colorBuffer)
							{
								while (i < bitmapsize)
									p[ptr[i++] & 0xffffff] = 1;

								tmp.UnlockBits(data);
								data = null;

								i = 0;

								for (; i < 16777216; i++)
									if (p[i] == 1)
										colorcounter++;
							}

							colorBuffer = null;

							tmp.Dispose();
							tmp = null;

							string result = String.Format("Total colors:\t{0}\r\nAfter filter:\t{1}\r\n",
								result1, colorcounter);

							report.AppendLine(Path.GetFileName(file));
							report.AppendLine(result);
						}
					}
				}
				catch
				{
					goto error;
				}
			}


			File.WriteAllText("colorlog.txt", report.ToString());
			MessageBox.Show(report.ToString(), "Result", MessageBoxButtons.OK,
				MessageBoxIcon.Information);

			return;

		error:
			MessageBox.Show("Please load a proper file before converting!",
				"Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
			return;
		}

        private void checkBox2_CheckedChanged(object sender, EventArgs e)
        {
            Options.RemoveDuplicateTiles = checkBox2.Checked;
            if (!Options.RemoveDuplicateTiles)
            {
                comboBox4.SelectedIndex = 5;
                checkBox9.Checked = false;
                checkBox9.Enabled = false;
            }
            else
            {
                checkBox9.Enabled = true;
                checkBox9.Checked = true;
            }
        }

        private void checkBox5_CheckedChanged(object sender, EventArgs e)
        {
            Options.OptimizeImage = checkBox5.Checked;
        }

        private void checkBox6_CheckedChanged(object sender, EventArgs e)
        {
            comboBox3.Enabled = checkBox6.Checked;
            if (comboBox3.Enabled)
            {
                comboBox3.SelectedIndex = 3;
                Options.SplitOutputIndex = 3;
            }
            Options.SplitOutput = comboBox3.Enabled;
        }

        private void checkBox8_CheckedChanged(object sender, EventArgs e)
        {
            Options.SaveOnImageFolder = checkBox8.Checked;
        }

        private void comboBox3_SelectedIndexChanged(object sender, EventArgs e)
        {
            Options.SplitOutputIndex = comboBox3.SelectedIndex;
        }

        private void comboBox4_SelectedIndexChanged(object sender, EventArgs e)
        {
            Options.TilemapOutput = comboBox4.SelectedIndex;

            if (Options.TilemapOutput != 5)
            {
                checkBox2.Checked = true;
            }
			if (Options.TilemapOutput == 3 || Options.TilemapOutput == 4)
			{
				checkBox9.Checked = false;
			}
			else
			{
				checkBox9.Checked = true;
			}
        }

        private void comboBox5_SelectedIndexChanged(object sender, EventArgs e)
        {
            Options.PaletteOutput = comboBox5.SelectedIndex;
        }

        private void checkBox7_CheckedChanged(object sender, EventArgs e)
        {
            button4.Enabled = Options.AllowTransparency = checkBox7.Checked;
        }

        Form3 opac;
        Form4 csettings;
		Form6 cgadsub;

        private void button4_Click(object sender, EventArgs e)
        {
            if (opac == null)
            {
                opac = new Form3();
                opac.Owner = this;
                opac.Show();
            }
            else if (opac.IsDisposed)
            {
                opac = new Form3();
                opac.Owner = this;
                opac.Show();
            }
            else
            {
                opac.Focus();
            }
        }

        private void button6_Click(object sender, EventArgs e)
        {
            if (csettings == null)
            {
                csettings = new Form4();
                csettings.Owner = this;
                csettings.Show();
            }
            else if (csettings.IsDisposed)
            {
                csettings = new Form4();
                csettings.Owner = this;
                csettings.Show();
            }
            else
            {
                csettings.Focus();
            }
        }

        private void checkBox9_CheckedChanged(object sender, EventArgs e)
        {
            Options.RemoveFlippedTiles = checkBox9.Checked;
        }

		private void checkBox10_CheckedChanged(object sender, EventArgs e)
		{
			button7.Enabled = Options.CGADSUB = checkBox10.Checked;
		}

		private void button7_Click(object sender, EventArgs e)
		{
			if (cgadsub == null)
			{
				cgadsub = new Form6();
				cgadsub.Owner = this;
				cgadsub.Show();
			}
			else if (cgadsub.IsDisposed)
			{
				cgadsub = new Form6();
				cgadsub.Owner = this;
				cgadsub.Show();
			}
			else
			{
				cgadsub.Focus();
			}
		}

		private void domainUpDown1_SelectedItemChanged(object sender, EventArgs e)
		{
			try
			{
				ushort value = Convert.ToUInt16(domainUpDown1.Text, 16);
				if (value > 0x3FF) throw new Exception();
				//domainUpDown1.SelectedIndex = value ^ 0x3FF;
				domainUpDown1.BackColor = SystemColors.Window;
				Options.OffsetTile = value;
			}
			catch
			{
				domainUpDown1.BackColor = Color.Red;
			}
		}

		private void checkBox11_CheckedChanged(object sender, EventArgs e)
		{
			if (checkBox11.Checked)
			{
				domainUpDown1.Enabled = true;
			}
			else
			{
				domainUpDown1.Enabled = false;
				domainUpDown1.SelectedIndex = 0x00 ^ 0x3FF;
				Options.OffsetTile = 0;
			}
		}

		private void checkBox12_CheckedChanged(object sender, EventArgs e)
		{
			if (checkBox12.Checked)
			{
				domainUpDown2.Enabled = true;
			}
			else
			{
				domainUpDown2.Enabled = false;
				domainUpDown2.SelectedIndex = 0x00 ^ 0xFF;
				Options.OffsetPalette = 0;
			}
		}

		private void domainUpDown2_SelectedItemChanged(object sender, EventArgs e)
		{
			try
			{
				byte value = Convert.ToByte(domainUpDown2.Text, 16);
				//domainUpDown2.SelectedIndex = value ^ 0xFF;
				domainUpDown2.BackColor = SystemColors.Window;
				Options.OffsetPalette = value;
			}
			catch
			{
				domainUpDown2.BackColor = Color.Red;
			}
		}
	}
}
