using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Image2Bitmap
{
    public partial class Form1 : Form
    {
        enum TransformColorFormats
        {
            BW1BppH,
            BW1BppV,
            BW8Bpp,
            RGB332,
            RGB444,
            RGB565,
        }

        Image OriginalImage;

        public Form1()
        {
            InitializeComponent();
            btn_Convert.Enabled = false;

            //selBox_Format.DataSource = Enum.GetValues(typeof(PixelFormat));
            selBox_Format.DataSource = Enum.GetValues(typeof(TransformColorFormats));
            selBox_Format.SelectedIndex = 0;
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void Btn_open_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFile = new OpenFileDialog();
            openFile.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            openFile.Filter = "Images (*.jpg, *.jpeg, *.png, *.bmp)|*.jpg;*.jpeg;*.png;*.bmp";
            openFile.RestoreDirectory = true;

            if (openFile.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    OriginalImage = Image.FromStream(openFile.OpenFile());
                    imageBox.Image = OriginalImage;
                    txt_imgSize.Text = string.Format(
                        "Info:\nWidth:  {0}\nHeight: {1}\n{2}",
                        OriginalImage.Width,
                        OriginalImage.Height,
                        OriginalImage.PixelFormat.ToString()
                    );
                    tabBox.SelectedIndex = 0;   // First tab (Image)
                    btn_Convert.Enabled = true;
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error:" + Environment.NewLine + ex.Message);
                    //throw;
                }
            }
        }

        private void GenDrawCode_BW1BbppH(Image image)
        {
            using (Bitmap bmp = new Bitmap(image.Width - (image.Width % 8) + 8, image.Height))
            {
                using (Graphics canvas = Graphics.FromImage(bmp))
                {
                    canvas.Clear(Color.White);
                    canvas.DrawImage(image, 0, 0);

                    GeneratedCode.Text = "uint8_t image = {" + Environment.NewLine;
                    GeneratedCode.Text += "\t";
                    int x_step = 0;

                    for (int y = 0; y < bmp.Height; y++)
                    {
                        for (int block = 0; block < bmp.Width / 8; block++)
                        {
                            int ColorByte = 0;

                            for (int bitInBlock = 7; bitInBlock >= 0; bitInBlock--)
                            {
                                Color pixelColor = bmp.GetPixel((block * 8 + 7 - bitInBlock), y);
                                if (pixelColor.R > 127 || pixelColor.G > 127 || pixelColor.B > 127)
                                    ColorByte &= ~(1 << bitInBlock);
                                else
                                    ColorByte |= 1 << bitInBlock;
                            }
                            GeneratedCode.Text += "0x" + ColorByte.ToString("X2") + ", ";
                            x_step++;
                            if (x_step == 16)
                                GeneratedCode.Text += Environment.NewLine + "\t";

                        }
                    }
                    GeneratedCode.Text += Environment.NewLine + "};";
                }
            }
        }


        private void GenDrawCode_BW1BbppV(Image image)
        {
            using (Bitmap bmp = new Bitmap(image.Width, image.Height - (image.Height % 8) + 8))
            {
                using (Graphics canvas = Graphics.FromImage(bmp))
                {
                    canvas.Clear(Color.White);
                    canvas.DrawImage(image, 0, 0);

                    GeneratedCode.Text = "uint8_t image = {" + Environment.NewLine;
                    GeneratedCode.Text += "\t";
                    int x_step = 0;

                    for (int x = 0; x < bmp.Width; x++)
                    {
                        for (int block = 0; block < bmp.Height / 8; block++)
                        {
                            int ColorByte = 0;

                            for (int bitInBlock = 7; bitInBlock >= 0; bitInBlock--)
                            {
                                Color pixelColor = bmp.GetPixel(x, block * 8 + bitInBlock);
                                if (pixelColor.R > 127 || pixelColor.G > 127 || pixelColor.B > 127)
                                    ColorByte &= ~(1 << bitInBlock);
                                else
                                    ColorByte |= 1 << bitInBlock;
                            }
                            GeneratedCode.Text += "0x" + ColorByte.ToString("X2") + ", ";

                            /*
                            GeneratedCode.Text += "B";
                            for (int bitInBlock = 7; bitInBlock >= 0; bitInBlock--)
                            {
                                Color pixelColor = bmp.GetPixel(x, block * 8 + bitInBlock);
                                if (pixelColor.R > 127 || pixelColor.G > 127 || pixelColor.B > 127)
                                    GeneratedCode.Text += "0";
                                else
                                    GeneratedCode.Text += "1";
                            }
                            GeneratedCode.Text += ", ";
                            */

                        }
                        x_step++;
                        if (x_step == 16)
                            GeneratedCode.Text += Environment.NewLine + "\t";
                    }
                    GeneratedCode.Text += Environment.NewLine + "};";
                }
            }
        }

        private void btn_Convert_Click(object sender, EventArgs e)
        {
            tabBox.SelectedIndex = 1;   // 2nd tab
            switch (selBox_Format.SelectedItem)
            {
                case TransformColorFormats.BW1BppH:
                    GenDrawCode_BW1BbppH(OriginalImage);
                    break;
                case TransformColorFormats.BW1BppV:
                    GenDrawCode_BW1BbppV(OriginalImage);
                    break;
                default:
                    MessageBox.Show("Sorry, unsupported.");
                    break;
            }
        }
    }
}
