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
            //BW8Bpp,
            RGB332,
            //RGB444,
            //RGB565,
        }

        private Image OriginalImage;
        private BackgroundWorker bgWork;

        public Form1()
        {
            InitializeComponent();
            btn_Convert.Enabled = false;

            //selBox_Format.DataSource = Enum.GetValues(typeof(PixelFormat));
            selBox_Format.DataSource = Enum.GetValues(typeof(TransformColorFormats));
            selBox_Format.SelectedIndex = 0;

            bgWork = new BackgroundWorker();
            bgWork.WorkerReportsProgress = true;
            bgWork.DoWork += new DoWorkEventHandler(ConvertImage);
            bgWork.ProgressChanged += new ProgressChangedEventHandler(BgConvertProgress);
            bgWork.RunWorkerCompleted += new RunWorkerCompletedEventHandler(ConvertDone);
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
                        "Filename: {0}\nWidth:  {1}\nHeight: {2}\n{3}",
                        openFile.SafeFileName,
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

        private String GenDrawCode_BW1BbppH(Image image)
        {
            String result = "uint8_t image = {" + Environment.NewLine + "\t";
            int correction = image.Width % 8;
            if (correction > 0)
                correction = -correction + 8;

            using (Bitmap bmp = new Bitmap(image.Width + correction, image.Height))
            {
                using (Graphics canvas = Graphics.FromImage(bmp))
                {
                    canvas.Clear(Color.White);
                    canvas.DrawImage(image, 0, 0);
                    
                    int x_step = 0;
                    int pixelsTotal = bmp.Width * bmp.Height;
                    int pixelsCurrent = 0;

                    for (int y = 0; y < bmp.Height; y++)
                    {
                        for (int block = 0; block < bmp.Width / 8; block++)
                        {
                            int ColorByte = 0;

                            for (int bitInBlock = 7; bitInBlock >= 0; bitInBlock--)
                            {
                                Color pixelColor = bmp.GetPixel((block * 8 + 7 - bitInBlock), y);
                                if (pixelColor.R > 248 || pixelColor.G > 248 || pixelColor.B > 248)
                                    ColorByte &= ~(1 << bitInBlock);
                                else
                                    ColorByte |= 1 << bitInBlock;
                                pixelsCurrent++;    // For progress bar
                            }
                            result += "0x" + ColorByte.ToString("X2") + ", ";
                            x_step++;
                            if (x_step == 16)
                            {
                                x_step = 0;
                                result += Environment.NewLine + "\t";
                            }

                            bgWork.ReportProgress((int)((double)pixelsCurrent / (double)pixelsTotal * 100));
                        }
                    }
                }
            }

            result += Environment.NewLine + "};";
            return result;
        }
        
        private String GenDrawCode_BW1BbppV(Image image)
        {
            String result = "uint8_t image = {" + Environment.NewLine + "\t";
            int correction = image.Height % 8;
            if (correction > 0)
                correction = -correction + 8;

            using (Bitmap bmp = new Bitmap(image.Width, image.Height + correction))
            {
                using (Graphics canvas = Graphics.FromImage(bmp))
                {
                    canvas.Clear(Color.White);
                    canvas.DrawImage(image, 0, 0);

                    int x_step = 0;

                    int pixelsTotal = bmp.Width * bmp.Height;
                    int pixelsCurrent = 0;

                    for (int vBlock = 0; vBlock < bmp.Height / 8; vBlock++)
                    {
                        for (int x = 0; x < bmp.Width; x++)
                        {
                            int ColorByte = 0;
                            //result += "B";    // Show in byte format: B11110000 (0xf0)
                            for (int bitInBlock = 0; bitInBlock < 8; bitInBlock++)
                            {
                                Color pixelColor = bmp.GetPixel(x, vBlock * 8 + bitInBlock);
                                if (pixelColor.R > 248 || pixelColor.G > 248 || pixelColor.B > 248)
                                    ColorByte &= ~(1 << bitInBlock);    // result += "0";
                                else
                                    ColorByte |= 1 << bitInBlock;   // result += "1";
                                pixelsCurrent++;
                            }
                            result += "0x" + ColorByte.ToString("X2") + ", ";
                            
                            x_step++;
                            if (x_step == 16)
                            {
                                x_step = 0;
                                result += Environment.NewLine + "\t";
                            }

                            bgWork.ReportProgress((int)((double)pixelsCurrent / (double)pixelsTotal * 100));
                        }
                    }
                }
            }
            result += Environment.NewLine + "};";
            return result;
        }

        private String GenDrawCode_RGB332(Image image)
        {
            String result = "uint8_t image = {" + Environment.NewLine + "\t";

            using (Bitmap bmp = new Bitmap(image))
            {
                int x_step = 0;
                Color pixelColor;

                int pixelsTotal = bmp.Width * bmp.Height;
                int pixelsCurrent = 0;

                for (int y = 0; y < bmp.Height; y++)
                {
                    for (int x = 0; x < bmp.Width; x++)
                    {
                        pixelColor = bmp.GetPixel(x, y);
                        // byte = 8 bit per color
                        // RRRRRRRR
                        int ColorByte = (pixelColor.R & 0xE0) | (pixelColor.G & 0xE0) >> 3 | (pixelColor.B & 0xC0) >> 6;

                        result += "0x" + ColorByte.ToString("X2") + ", ";
                        pixelsCurrent++;

                        x_step++;
                        if (x_step == 16)
                        {
                            x_step = 0;
                            result += Environment.NewLine + "\t";
                        }

                        bgWork.ReportProgress((int)((double)pixelsCurrent / (double)pixelsTotal * 100));
                    }
                }
            }
            result += Environment.NewLine + "};";
            return result;
        }

        private void ConvertImage(object sender, DoWorkEventArgs e)
        {
            switch (e.Argument)
            {
                case TransformColorFormats.BW1BppH:
                    e.Result = GenDrawCode_BW1BbppH(OriginalImage);
                    break;
                case TransformColorFormats.BW1BppV:
                    e.Result = GenDrawCode_BW1BbppV(OriginalImage);
                    break;
                case TransformColorFormats.RGB332:
                    e.Result = GenDrawCode_RGB332(OriginalImage);
                    break;
                default:
                    MessageBox.Show("Sorry, unsupported.");
                    break;
            }
        }

        private void BgConvertProgress(object sender, ProgressChangedEventArgs e)
        {
            convertProgress.Value = e.ProgressPercentage;
        }

        private void ConvertDone(object sender, RunWorkerCompletedEventArgs e)
        {
            convertProgress.Value = 100;
            GeneratedCode.Text = (String)e.Result;
            tabBox.SelectedIndex = 1;   // Open "code" tab

            //bgWork.DoWork -= ConvertImage;
            //bgWork.ProgressChanged -= BgConvertProgress;
            //bgWork.RunWorkerCompleted -= ConvertDone;

            btn_Convert.Enabled = true;
        }

        private void Btn_Convert_Click(object sender, EventArgs e)
        {
            convertProgress.Value = 0;
            btn_Convert.Enabled = false;
            bgWork.RunWorkerAsync(selBox_Format.SelectedItem);
            /*
            while (bgWork.IsBusy)
            {
                convertProgress.Increment(1);
                // Keep UI messages moving, so the form remains 
                // responsive during the asynchronous operation.
                Application.DoEvents();
            }
            */
        }
    }
}
