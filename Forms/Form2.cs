using System;
using System.Drawing;
using System.Windows.Forms;

namespace SnesGFX
{
    public partial class Form2 : Form
    {
        private Bitmap bitmap;

        public Bitmap CurrrentImage
        {
            get
            {
                return bitmap;
            }

            set
            {
                this.bitmap = value;
                this.pictureBox1.Image = Program.ScaleRatio(value, pictureBox1.ClientSize);
            }
        }

        public Form2(Bitmap bitmap, Size size)
        {
            InitializeComponent();
            this.bitmap = bitmap;
            this.ClientSize = size;
        }

        private void ViewBase_SizeChanged(object sender, EventArgs e)
        {
            if (bitmap != null)
            {
                pictureBox1.Image = Program.ScaleRatio(bitmap, pictureBox1.ClientSize);
            }
        }
    }
}
