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
#region Color code conversion
        private Color color_from332(int pixel)
        {
            // Format: RRRG GGBB (Hex) 8*8*4=256 colors
            // 1110 0000 = 0xE0
            // 0001 1100 = 0x1C
            // 0000 0011 = 0x03
            return Color.FromArgb((pixel & 0xE0), (pixel & 0x1C) << 3, (pixel & 0x3) << 6);
        }

        private Color color_from444(int pixel)
        {
            // Format: 0000 RRRR GGGG BBBB, 16*16*16=4096 colors
            // 0000 1111 0000 0000 = 0x0F00
            // 0000 0000 1111 0000 = 0x00F0
            // 0000 0000 0000 1111 = 0x000F
            return Color.FromArgb((pixel & 0x0F00) >> 4, (pixel & 0x00F0), (pixel & 0x000F) << 4);
        }

        private Color color_from556(int pixel)
        {
            // Format: RRRR RGGG GGGB BBBB (Hex) 32*32*64=65536 colors
            // 1111 1000 0000 0000 = 0xF800
            // 0000 0111 1110 0000 = 0x07E0
            // 0000 0000 0001 1111 = 0x001F
            return Color.FromArgb((pixel & 0xF800) >> 8, (pixel & 0x7E0) >> 3, (pixel & 0x1F) << 3);
        }
#endregion

#region Image2Code
        private String CodeFromImage_1bpp(Image image, bool horisontal = false)
        {
            String result = "uint8_t image = {" + Environment.NewLine + "\t";
            int addW = 0;
            int addH = 0;
            if (horisontal)
            {
                // Horisontal
                addW = image.Width % 8;
                if (addW > 0)
                    addW = -addW + 8;
            } else
            {
                // Vertical
                addH = image.Height % 8;
                if (addH > 0)
                    addH = -addH + 8;
            }

            using (Bitmap bmp = new Bitmap(image.Width + addW, image.Height + addH))
            {
                using (Graphics canvas = Graphics.FromImage(bmp))
                {
                    canvas.Clear(Color.White);
                    canvas.DrawImage(image, 0, 0, image.Width, image.Height);
                    
                    int x_step = 0;
                    int pixelsTotal = bmp.Width * bmp.Height;
                    int pixelsCurrent = 0;
                    int bitInBlock = 7;
                    int ColorByte = 0;
                    Color pixelColor;

                    for (int y = 0; y < bmp.Height; y++)
                    {
                        for (int x = 0; x < bmp.Width; x++)
                        {
                            for (int tmpY = 0; tmpY < ((horisontal)? 1 : 8); tmpY++)
                            {
                                pixelColor = bmp.GetPixel(x, ((horisontal)? y : y + tmpY));

                                if (pixelColor.R > 248 || pixelColor.G > 248 || pixelColor.B > 248)
                                    ColorByte &= ~(1 << bitInBlock);    // White
                                else
                                    ColorByte |= 1 << bitInBlock;       // Black

                                if (bitInBlock == 0)
                                {
                                    result += "0x" + ColorByte.ToString("X2") + ", ";
                                    x_step++;
                                    if (x_step == 16)
                                    {
                                        x_step = 0;
                                        result += Environment.NewLine + "\t";
                                    }
                                    ColorByte = 0;
                                    bitInBlock = 7;
                                }
                                else
                                    bitInBlock--;

                                bgWork.ReportProgress((int)((double)pixelsCurrent++ / (double)pixelsTotal * 100));
                            }
                        }
                        if (!horisontal)
                            y += 7;
                    }
                }
            }

            result += Environment.NewLine + "};";
            return result;
        }
        
        private string CodeFromImage(TransformColorFormats format)
        {
            StringBuilder result = new StringBuilder();
            string codeFormat = "";
            int Width = 0, Height = 0;
            Image image = null;

            this.Invoke(new MethodInvoker(delegate {
                image = imageBox.Image;
                Width = (int)num_Width.Value;
                Height = (int)num_Height.Value;
            }));

            switch (format)
            {
                case TransformColorFormats.RGB332:
                    result.Append("uint8_t");
                    codeFormat = "0x{0:x2}, ";
                    break;
                case TransformColorFormats.RGB565:
                    result.Append("uint16_t");
                    codeFormat = "0x{0:x4}, ";
                    break;
            }
            result.Append(" image = {" + Environment.NewLine + "\t");
            
            using (Bitmap bmp = new Bitmap(image, Width, Height))
            {
                int rowPos = 0;
                Color pixelColor;

                int pixelsTotal = bmp.Width * bmp.Height;
                int pixelsCurrent = 0;
                int ColorByte = 0;

                for (int y = 0; y < bmp.Height; y++)
                {
                    for (int x = 0; x < bmp.Width; x++)
                    {
                        pixelColor = bmp.GetPixel(x, y);

                        switch (format)
                        {
                            case TransformColorFormats.RGB332:
                                ColorByte =
                                    (pixelColor.R & 0xE0) |
                                    (pixelColor.G & 0xE0) >> 3 |
                                    (pixelColor.B & 0xC0) >> 6;
                                break;
                            case TransformColorFormats.RGB565:
                                // byte = 16 bit per color
                                // RRRR RGGG GGGB BBBB
                                ColorByte =
                                    (pixelColor.R & 0xF8) << 8 |
                                    (pixelColor.G & 0xFC) << 3 |
                                    (pixelColor.B & 0xF8) >> 3;
                                break;
                        }

                        result.AppendFormat(codeFormat, ColorByte);
                        pixelsCurrent++;

                        rowPos++;
                        if (rowPos == 16)
                        {
                            rowPos = 0;
                            result.Append(Environment.NewLine + "\t");
                        }

                        bgWork.ReportProgress((int)((double)pixelsCurrent / (double)pixelsTotal * 100));
                    }
                }
            }

            result.Append(Environment.NewLine + "};");
            return result.ToString();
        }

        #endregion

        private void ImageToCode(object sender, DoWorkEventArgs e)
        {
            switch (e.Argument)
            {
                case TransformColorFormats.BW1BppH:
                    e.Result = CodeFromImage_1bpp(OriginalImage, true);
                    break;
                case TransformColorFormats.BW1BppV:
                    e.Result = CodeFromImage_1bpp(OriginalImage, false);
                    break;
                case TransformColorFormats.RGB332:
                case TransformColorFormats.RGB565:
                    e.Result = CodeFromImage((TransformColorFormats)e.Argument);
                    //e.Result = GenDrawCode_RGB332(OriginalImage);
                    break;
                //case TransformColorFormats.RGB565:
                    //e.Result = GenDrawCode_RGB565(OriginalImage);
                    //break;
                default:
                    MessageBox.Show("Sorry, unsupported.");
                    break;
            }
        }

        #region Code2Image

        private void CodeToImage(object sender, DoWorkEventArgs e)
        {
            TransformColorFormats format = (TransformColorFormats)e.Argument;

            string Code = "";
            int Width = 0, Height = 0;
            int x = 0, y = 0;

            this.Invoke(new MethodInvoker(delegate {
                Width = (int)num_Width.Value;
                Height = (int)num_Height.Value;
                Code = GeneratedCode.Text;
            }));

            Bitmap result = new Bitmap(Width, Height);

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
                    regex = @"0x([0-9a-fA-F]{2}),"; // 0xAB (one byte)
                    break;
                case TransformColorFormats.RGB565:
                    regex = @"0x([0-9a-fA-F]{4}),"; // 0xABCD (two bytes)
                    break;
            }

            foreach (Match m in Regex.Matches(Code, regex))
            {
                pixelColor = int.Parse(m.Groups[1].Value, System.Globalization.NumberStyles.HexNumber);

                switch (format)
                {
                    case TransformColorFormats.BW1BppH:
                        for (int offset = 7; offset >= 0; offset--)
                        {
                            result.SetPixel(x, y, ((pixelColor & (1 << offset)) > 0) ? Color.Black : Color.White);
                            x++;
                        }
                        x--;
                        break;
                    case TransformColorFormats.BW1BppV:
                        for (int offset = 7; offset >= 0; offset--)
                        {
                            result.SetPixel(x, y + (-offset + 7), ((pixelColor & (1 << offset)) > 0) ? Color.Black : Color.White);
                        }
                        break;
                    case TransformColorFormats.RGB332:  // RRRG GGBB
                        result.SetPixel(x, y, color_from332(pixelColor));
                        break;
                    case TransformColorFormats.RGB565:  // RRRR RGGG GGGB BBBB
                        result.SetPixel(x, y, color_from556(pixelColor));
                        break;
                }

                x++;
                if (x == Width)
                {
                    x = 0;
                    if (format == TransformColorFormats.BW1BppV)
                        y += 8;
                    else
                        y += 1;

                    if (y == Height)
                    {
                        break;  // All done. Stop conversion.
                    }
                }
                pixelsCurrent++;

                bgWork.ReportProgress((int)((double)pixelsCurrent / (double)pixelsTotal * 100));
            }

             e.Result = result;
        }

        #endregion

        private void BgConvertProgress(object sender, ProgressChangedEventArgs e)
        {
            convertProgress.Value = e.ProgressPercentage;
        }

        private void ConvertDone(object sender, RunWorkerCompletedEventArgs e)
        {
            GC.Collect();
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
            GC.Collect();
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

            while (bgWork.IsBusy)
            {
                convertProgress.Increment(1);
                // Keep UI messages moving, so the form remains 
                // responsive during the asynchronous operation.
                Application.DoEvents();
            }
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
