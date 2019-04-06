using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace SnesGFX.Forms
{
    public partial class Form6 : Form
    {
        public Form6()
        {
            InitializeComponent();
        }

		private void textBox1_TextChanged(object sender, EventArgs e)
		{
			try
			{
				Program.maxCgasubTransparency = byte.Parse(textBox1.Text);
				textBox1.BackColor = SystemColors.Window;
			}
			catch
			{
				textBox1.BackColor = Color.Orange;
				Program.maxCgasubTransparency = 224;
			}
		}

		private void textBox2_TextChanged(object sender, EventArgs e)
		{
			try
			{
				Program.cgadsubTolerance = byte.Parse(textBox2.Text);
				textBox2.BackColor = SystemColors.Window;
				if (Program.cgadsubTolerance < 1 || Program.cgadsubTolerance > 8) throw new Exception();
			}
			catch
			{
				textBox2.BackColor = Color.Orange;
				Program.cgadsubTolerance = 2;
			}
		}
    }
}
