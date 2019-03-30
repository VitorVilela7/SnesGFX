using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace SnesGFX
{
    public partial class Form3 : Form
    {
        public Form3()
        {
            InitializeComponent();
            setColor(Program.customTransparent);

            foreach (Control c in this.Controls[0].Controls)
            {
                if (c is RadioButton)
                {
                    int i = int.Parse((string)((RadioButton)c).Tag);
                    if (i == Program.colorMode)
                    {
                        (c as RadioButton).Checked = true;
                    }

                    c.Click += new EventHandler(c_Click);
                }
            }
        }

        void c_Click(object sender, EventArgs e)
        {
            Program.colorMode = int.Parse((string)((RadioButton)sender).Tag);
            Program.customTransparent = button1.BackColor;
        }

        void setColor(Color color)
        {
            button1.BackColor = color;
            button1.ForeColor = Color.FromArgb(color.ToArgb() ^ 0xFFFFFF);
            button1.Text = color.IsKnownColor ? color.Name : "#" + (color.ToArgb() & 0xFFFFFF).ToString("X6");
            Program.customTransparent = button1.BackColor;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            colorDialog1.Color = button1.BackColor;
            if (colorDialog1.ShowDialog(this) == System.Windows.Forms.DialogResult.OK)
            {
                setColor(colorDialog1.Color);
                radioButton5.Checked = true;
            }
        }
    }
}
