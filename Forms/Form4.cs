using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace SnesGFX
{
    public partial class Form4 : Form
    {
        public Form4()
        {
            InitializeComponent();

            foreach (var c in Controls)
            {
                if (c is GroupBox)
                {
                    foreach (RadioButton d in (c as GroupBox).Controls)
                    {
                        if (int.Parse((string)d.Tag) == Program.arrangeMethod)
                        {
                            d.Checked = true;
                        }
                    }
                }
            }
        }

        private void radioButton1_CheckedChanged(object sender, EventArgs e)
        {
            Program.arrangeMethod = int.Parse((string)((sender as RadioButton).Tag));
        }
    }
}
