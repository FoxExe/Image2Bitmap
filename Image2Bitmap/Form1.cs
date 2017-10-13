using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
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
            RGB565,
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

            num_Width.Enabled = false;
            num_Height.Enabled = false;
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
                        "{0}\n{1}",
                        openFile.SafeFileName,
                        OriginalImage.PixelFormat.ToString()
                    );
                    num_Width.Value = OriginalImage.Width;
                    num_Height.Value = OriginalImage.Height;

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

#region Image2Code
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

                // 6 = size of "0x12, ", 16 - every 16 lines we add newline and tab
                StringBuilder str = new StringBuilder((bmp.Width * bmp.Height * 6) + (bmp.Width * bmp.Height / 16 * (Environment.NewLine.Length + 1)));

                int pixelsTotal = bmp.Width * bmp.Height;
                int pixelsCurrent = 0;

                for (int y = 0; y < bmp.Height; y++)
                {
                    for (int x = 0; x < bmp.Width; x++)
                    {
                        pixelColor = bmp.GetPixel(x, y);
                        // byte = 8 bit per color
                        // RRRG GGBB
                        int ColorByte =
                            (pixelColor.R & 0xE0) |
                            (pixelColor.G & 0xE0) >> 3 |
                            (pixelColor.B & 0xC0) >> 6;

                        //result += "0x" + ColorByte.ToString("X2") + ", ";
                        str.AppendFormat("0x{0:x2}, ", ColorByte);

                        pixelsCurrent++;

                        x_step++;
                        if (x_step == 16)
                        {
                            x_step = 0;
                            //result += Environment.NewLine + "\t";
                            str.Append(Environment.NewLine + "\t");
                        }

                        bgWork.ReportProgress((int)((double)pixelsCurrent / (double)pixelsTotal * 100));
                    }
                }
                result += str.ToString();
            }
            result += Environment.NewLine + "};";
            return result;
        }


        private String GenDrawCode_RGB565(Image image)
        {
            String result = "uint8_t image = {" + Environment.NewLine + "\t";

            using (Bitmap bmp = new Bitmap(image))
            {
                int x_step = 0;
                Color pixelColor;

                int pixelsTotal = bmp.Width * bmp.Height;
                int pixelsCurrent = 0;

                // 8 = size of "0x1234, ", 16 - every 16 lines we add newline and tab
                StringBuilder str = new StringBuilder((bmp.Width * bmp.Height * 8) + (bmp.Width * bmp.Height / 16 * (Environment.NewLine.Length + 1)));

                for (int y = 0; y < bmp.Height; y++)
                {
                    for (int x = 0; x < bmp.Width; x++)
                    {
                        pixelColor = bmp.GetPixel(x, y);
                        // byte = 16 bit per color
                        // RRRR RGGG GGGB BBBB
                        int ColorByte = (pixelColor.R & 0xF8) << 8 |
                            (pixelColor.G & 0xFC) << 3 |
                            (pixelColor.B & 0xF8) >> 3;

                        str.AppendFormat("0x{0:x4}, ", ColorByte);
                        //result += "0x" + ColorByte.ToString("X4") + ", ";
                        pixelsCurrent++;

                        x_step++;
                        if (x_step == 16)
                        {
                            x_step = 0;
                            //result += Environment.NewLine + "\t";
                            str.Append(Environment.NewLine + "\t");
                        }

                        bgWork.ReportProgress((int)((double)pixelsCurrent / (double)pixelsTotal * 100));
                    }
                }
                result += str.ToString();
            }
            
            result += Environment.NewLine + "};";
            return result;
        }
        #endregion

        private void ImageToCode(object sender, DoWorkEventArgs e)
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
                case TransformColorFormats.RGB565:
                    e.Result = GenDrawCode_RGB565(OriginalImage);
                    break;
                default:
                    MessageBox.Show("Sorry, unsupported.");
                    break;
            }
        }

        #region Code2Image
        private Image GenImageFromCode(TransformColorFormats format)
        {
            string Code = "";
            int Width = 0, Height = 0;
            int x = 0, y = 0;

            this.Invoke(new MethodInvoker(delegate {
                Width = (int)num_Width.Value;
                Height = (int)num_Height.Value;
                Code = GeneratedCode.Text;
            }));

            Bitmap result = new Bitmap(Width, Height, PixelFormat.Format16bppRgb565);

            int pixelsCurrent = 0;
            int pixelsTotal = Width * Height;
            int pixelColor;

            // Two ways re symbol-by-symbol, or use regex. What faster?
            // Read from symbos "{", ignore tabs and spaces, read to "}". Use "," as separator.
            // Use regex and search pattern "0x([0-9a-fA-F]{2}),"
            string regex = "";
            switch (format)
            {
                case TransformColorFormats.BW1BppH:
                case TransformColorFormats.BW1BppV:
                case TransformColorFormats.RGB332:
                    regex = @"0x([0-9a-fA-F]{2}),";
                    break;
                case TransformColorFormats.RGB565:
                    regex = @"0x([0-9a-fA-F]{4}),";
                    break;
            }

            foreach (Match m in Regex.Matches(Code, regex))
            {
                pixelColor = int.Parse(m.Groups[1].Value, System.Globalization.NumberStyles.HexNumber);
                switch (format)
                {
                    //case TransformColorFormats.BW1BppH:
                    //    break;
                    //case TransformColorFormats.BW1BppV:
                    //    break;
                    case TransformColorFormats.RGB332:  // RRRG GGBB
                        result.SetPixel(x, y, Color.FromArgb(
                            (pixelColor & 0xE0),
                            (pixelColor & 0x1C) << 3,
                            (pixelColor & 0x3) << 6)
                        );
                        break;
                    case TransformColorFormats.RGB565:  // RRRR RGGG GGGB BBBB
                        result.SetPixel(x, y, Color.FromArgb(
                            (pixelColor & 0xF800) >> 8,
                            (pixelColor & 0x7E0) >> 3,
                            (pixelColor & 0x1F) << 3)
                        );
                        break;
                    default:
                        break;
                }
                
                x++;
                if (x == Width)
                {
                    x = 0;
                    y++;
                    if (y == Height)
                    {
                        break;  // All done. Stop conversion.
                    }
                }
                pixelsCurrent++;

                bgWork.ReportProgress((int)((double)pixelsCurrent / (double)pixelsTotal * 100));
            }

            return result;
        }
#endregion
        
        private void CodeToImage(object sender, DoWorkEventArgs e)
        {
            switch (e.Argument)
            {
                case TransformColorFormats.RGB565:
                case TransformColorFormats.RGB332:
                    e.Result = GenImageFromCode((TransformColorFormats)e.Argument);
                    break;
                default:
                    MessageBox.Show("Sorry, unsupported.");
                    e.Result = OriginalImage;
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
            switch (tabBox.SelectedIndex)
            {
                case 0:
                    GeneratedCode.Text = (String)e.Result;
                    tabBox.SelectedIndex = 1;
                    break;
                case 1:
                    imageBox.Image = (Image)e.Result;
                    tabBox.SelectedIndex = 0;
                    break;
                default:
                    break;
            }
            btn_Convert.Enabled = true;
        }

        private void Btn_Convert_Click(object sender, EventArgs e)
        {
            convertProgress.Value = 0;
            btn_Convert.Enabled = false;

            switch (tabBox.SelectedIndex)
            {
                default:
                case 0: // Image -> Code
                    bgWork = new BackgroundWorker();
                    bgWork.WorkerReportsProgress = true;
                    bgWork.DoWork += new DoWorkEventHandler(ImageToCode);
                    bgWork.ProgressChanged += new ProgressChangedEventHandler(BgConvertProgress);
                    bgWork.RunWorkerCompleted += new RunWorkerCompletedEventHandler(ConvertDone);
                    bgWork.RunWorkerAsync(selBox_Format.SelectedItem);
                    break;
                case 1: // Code -> Image
                    bgWork = new BackgroundWorker();
                    bgWork.WorkerReportsProgress = true;
                    bgWork.DoWork += new DoWorkEventHandler(CodeToImage);
                    bgWork.ProgressChanged += new ProgressChangedEventHandler(BgConvertProgress);
                    bgWork.RunWorkerCompleted += new RunWorkerCompletedEventHandler(ConvertDone);
                    bgWork.RunWorkerAsync(selBox_Format.SelectedItem);

                    break;
            }

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
        
        private void Txt_ZoomMode_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            int mode = (int)imageBox.SizeMode;
            mode++;
            if (mode == sizeof(PictureBoxSizeMode) + 1)
                mode = 0;

            txt_ZoomMode.Text = "Zoom mode: " + Enum.GetName(typeof(PictureBoxSizeMode), (PictureBoxSizeMode)mode);
            imageBox.SizeMode = (PictureBoxSizeMode)mode;
        }

        private void tabBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (tabBox.SelectedIndex == 1)  // Show size boxes only on "Code" tab
            {
                num_Width.Enabled = true;
                num_Height.Enabled = true;
            } else
            {
                num_Width.Enabled = false;
                num_Height.Enabled = false;
            }
        }
    }
}
