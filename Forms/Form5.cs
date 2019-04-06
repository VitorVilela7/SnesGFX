using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Drawing.Imaging;

namespace SnesGFX.Forms
{
    public partial class Form5 : Form
    {
        /// <summary>
        /// likely to have 256 entries.
        /// </summary>
        public Color[] table;
        Bitmap bmp;
        Label[] labelTable;

        int source = -1;
        int source2 = -1;

        public bool cancelled = true;

        public Form5(Color[] tbl)
        {
            InitializeComponent();
            this.table = new Color[256];
            this.labelTable = new Label[256];
            tbl.CopyTo(table, 0);
            Application.UseWaitCursor = false;
            using (var b = new Bitmap(1, 1, PixelFormat.Format32bppArgb))
            {
                this.bmp = Program.ScaleRatio(b, new Size(16, 16));
            }

            for (int i = 0; i < 256; ++i)
            {
                int x = i << 4 & 0xF0;
                int y = i & 0xF0;

                Label label = new Label();
                if (table[i].A != 0)
                {
                    label.BackColor = table[i];
                }
                else
                {
                    label.BackColor = Color.Transparent;
                    label.Image = bmp;
                }

                if (table[i].IsEmpty)
                {
                    label.Image = Properties.Resources.empty;
                }
                label.Size = new Size(16, 16);
                label.Location = new Point(x, y);
                label.MouseClick += new MouseEventHandler(label_MouseClick);
                label.Tag = i;
                panel1.Controls.Add(label);
                labelTable[i] = label;
            }
        }

        void label_MouseClick(object sender, MouseEventArgs e)
        {
            Text = "Arrange Colors: Manual";
            if (e.Button == System.Windows.Forms.MouseButtons.Left)
            {
                if (source == -1)
                {
                    source = (int)(((Label)sender).Tag);
                }
                else
                {
                    int dest = (int)(((Label)sender).Tag);

                    Color a = table[source];
                    Color b = table[dest];

                    changeColor(b, source);
                    changeColor(a, dest);
                    source = -1;
                }
            }
            else if (e.Button == System.Windows.Forms.MouseButtons.Right)
            {
                if (source2 == -1)
                {
                    source2 = (int)(((Label)sender).Tag) & 0xF0;
                }
                else
                {
                    int dest = (int)(((Label)sender).Tag) & 0xF0;

                    for (int i = 0; i < 16; ++i)
                    {
                        int n1 = source2 + i;
                        int n2 = dest + i;

                        Color a = table[n1];
                        Color b = table[n2];

                        changeColor(b, n1);
                        changeColor(a, n2);
                    }

                    source2 = -1;
                }
            }
            else
            {
                Text = "Hey, use a saner mouse button please!";
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            var data = Properties.Resources.mw3;
            Color[] tbl = new Color[256];

            for (int i = 0; i < 256; ++i)
            {
                int snes = data[i << 1] | (data[(i << 1) + 1] << 8);
                tbl[i] = Color.FromArgb((snes & 0x1F) << 3, snes >> 2 & 0xF8, snes >> 7 & 0xF8);

                if (table[i].A == 0)
                {
                    changeColor(tbl[i], i);
                }
            }
        }

        private void changeColor(Color c, int n)
        {
            table[n] = c;
            labelTable[n].BackColor = c;
            if (c.A != 0)
            {
                labelTable[n].Image = null;
            }
            else
            {
                labelTable[n].Image = bmp;
            }

            if (c.IsEmpty)
            {
                labelTable[n].Image = Properties.Resources.empty;
            }
        }

        private void button4_Click(object sender, EventArgs e)
        {
            List<Color> list = new List<Color>(table);
            list.Sort(Program.SortColorsByHue);
            for (int i = 0; i < 256; ++i)
            {
                changeColor(list[i], i);
            }
        }

        private void button5_Click(object sender, EventArgs e)
        {
            List<Color> list = new List<Color>(table);
            list.Sort(Program.SortColorsByBrightness);
            for (int i = 0; i < 256; ++i)
            {
                changeColor(list[i], i);
            }
        }

        private void button6_Click(object sender, EventArgs e)
        {
            List<Color> list = new List<Color>(table);
            list.Sort(Program.SortColorsBySaturation);
            for (int i = 0; i < 256; ++i)
            {
                changeColor(list[i], i);
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            cancelled = false;
            Close();
        }

        private void Form5_FormClosed(object sender, FormClosedEventArgs e)
        {
            if (bmp != null)
            {
                bmp.Dispose();
                bmp = null;
            }
        }
    }
}
