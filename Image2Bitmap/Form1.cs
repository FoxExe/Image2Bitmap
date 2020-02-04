using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace Image2Bitmap
{
    public partial class Form1 : Form
    {
        enum TransformColorFormats
        {
            BW_1bpp_H,  // 8 pixels in width
            BW_1bpp_V,  // 8 pixels in height
            BW_1bpp_C,  // Horisontal read line-by-line to byte array
            RGB_332,
            RGB_444,
            RGB_565,
        }
        
        private BackgroundWorker bgWork;

        public Form1()
        {
            InitializeComponent();

            btn_Save.Enabled = false;

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
            openFile.Filter = "Images|*.jpg;*.jpeg;*.png;*.bmp";
            openFile.RestoreDirectory = true;

            if (openFile.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    imageBox.Image = Image.FromStream(openFile.OpenFile());
                    txt_imgSize.Text = string.Format(
                        "{0}\n{1}",
                        openFile.SafeFileName,
                        imageBox.Image.PixelFormat.ToString()
                    );
                    num_Width.Value = imageBox.Image.Width;
                    num_Height.Value = imageBox.Image.Height;

                    tabBox.SelectedIndex = 0;   // First tab (Image)
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
        private void ImageToCode(object sender, DoWorkEventArgs e)
        {
            StringBuilder result = new StringBuilder();
            string codeFormat = "";
            int Width = 0, Height = 0;
            int bitInBlock = 7; // For BW/1bpp conversion
            bool is_1bpp = false;
            Image image = null;

            this.Invoke(new MethodInvoker(delegate
            {
                image = imageBox.Image;
                Width = (int)num_Width.Value;
                Height = (int)num_Height.Value;
            }));

            // Some additional checks
            if ((TransformColorFormats)e.Argument == TransformColorFormats.BW_1bpp_H && Width % 8 != 0)
            {
                MessageBox.Show("Error:\nWidth has to be multiple 8!");
                return;
            }

            if ((TransformColorFormats)e.Argument == TransformColorFormats.BW_1bpp_V && Height % 8 != 0)
            {
                MessageBox.Show("Error:\nHeight has to be multiple 8!");
                return;
            }

            switch (e.Argument)
            {
                case TransformColorFormats.BW_1bpp_H:
                case TransformColorFormats.BW_1bpp_V:
                case TransformColorFormats.BW_1bpp_C:
                    result.Append("uint8_t");
                    codeFormat = "0x{0:x2}, ";
                    is_1bpp = true;
                    break;
                case TransformColorFormats.RGB_332:
                    result.Append("uint8_t");
                    codeFormat = "0x{0:x2}, ";
                    break;
                case TransformColorFormats.RGB_444:
                case TransformColorFormats.RGB_565:
                    result.Append("uint16_t");
                    codeFormat = "0x{0:x4}, ";
                    break;
                default:
                    MessageBox.Show("Sorry, unsupported format");
                    return;
            }

            result.Append(" image = {" + Environment.NewLine + "\t");

            using (Bitmap bmp = new Bitmap(image, Width, Height))
            {
                int rowPos = 0;

                int pixelsTotal = bmp.Width * bmp.Height;
                int pixelsCurrent = 0;
                int ColorByte = 0;

                // ===========================================================
                // Optimisation: Upto 5x faster than GetPixel();
                Rectangle rect = new Rectangle(0, 0, bmp.Width, bmp.Height);
                System.Drawing.Imaging.BitmapData bmpData =
                    bmp.LockBits(rect, System.Drawing.Imaging.ImageLockMode.ReadWrite,
                    bmp.PixelFormat);

                IntPtr ptr = bmpData.Scan0;
                int bytes = bmpData.Stride * bmp.Height;    // ARGB: Width * Height * 4 (Stride = Width * 4)
                byte[] rgbValues = new byte[bytes];

                // Format BGRA (GRB+Alpha, inverted). Example: BBBBBBBB GGGGGGGG RRRRRRRR AAAAAAAA
                System.Runtime.InteropServices.Marshal.Copy(ptr, rgbValues, 0, bytes);

                int pixelByte = 0;
                // ===========================================================

                // Read image pixel's from left to right, top to bottom (Like a book)
                for (int y = 0; y < bmp.Height; y++)
                {
                    for (int x = 0; x < bmp.Width; x++)
                    {
                        pixelByte = (y * bmp.Width + x) * 4;

                        switch (e.Argument)
                        {
                            case TransformColorFormats.BW_1bpp_V:
                                for (int px = 0; px < 7; px++)
                                {
                                    pixelByte = ((y + px) * bmp.Width + x) * 4;

                                    if (rgbValues[pixelByte + 2] > 128 ||   // Red
                                        rgbValues[pixelByte + 1] > 128 ||   // Green
                                        rgbValues[pixelByte + 0] > 128)     // Blue
                                        ColorByte &= ~(1 << px);    // White/remove
                                    else
                                        ColorByte |= 1 << px;       // Black/set
                                }
                                bitInBlock = 0;
                                break;
                            case TransformColorFormats.BW_1bpp_H:
                            case TransformColorFormats.BW_1bpp_C:
                                if (rgbValues[pixelByte + 2] > 128 ||   // Red
                                    rgbValues[pixelByte + 1] > 128 ||   // Green
                                    rgbValues[pixelByte + 0] > 128)     // Blue
                                    ColorByte &= ~(1 << bitInBlock);    // White/remove
                                else
                                    ColorByte |= 1 << bitInBlock;       // Black/set

                                break;
                            case TransformColorFormats.RGB_332:
                                ColorByte =
                                    (rgbValues[pixelByte + 2] & 0xE0) |         // 0xE0 = 1110 0000
                                    (rgbValues[pixelByte + 1] & 0xE0) >> 3 |
                                    (rgbValues[pixelByte + 0] & 0xC0) >> 6;     // 0xC0 = 1100 0000
                                break;
                            case TransformColorFormats.RGB_444:
                                // byte = 16 bit per color
                                // RRRR RGGG GGGB BBBB
                                ColorByte =
                                    (rgbValues[pixelByte + 2] & 0xF0) << 4 |    // 0xF0 = 1111 0000
                                    (rgbValues[pixelByte + 1] & 0xF0) |
                                    (rgbValues[pixelByte + 0] & 0xF0) >> 4;
                                break;
                            case TransformColorFormats.RGB_565:
                                // byte = 16 bit per color
                                // RRRR RGGG GGGB BBBB
                                ColorByte =
                                    (rgbValues[pixelByte + 2] & 0xF8) << 8 |    // 0xF8 = 1111 1000
                                    (rgbValues[pixelByte + 1] & 0xFC) << 3 |    // 0xFC = 1111 1100
                                    (rgbValues[pixelByte + 0] & 0xF8) >> 3;
                                break;
                        }

                        if (!is_1bpp || bitInBlock == 0)
                        {
                            result.AppendFormat(codeFormat, ColorByte);
                            pixelsCurrent++;
                            rowPos++;

                            if (rowPos == 16)
                            {
                                rowPos = 0;
                                result.Append(Environment.NewLine + "\t");
                            }

                            bitInBlock = 7; // BW/1bpp bit position reset
                            ColorByte = 0;  // Reset data
                        }
                        else
                        {
                            bitInBlock--;
                        }
                    }

                    if ((TransformColorFormats) e.Argument == TransformColorFormats.BW_1bpp_V)
                    {
                        y += 7;
                    }
                    bgWork.ReportProgress((int)((double)pixelsCurrent / (double)pixelsTotal * 100));
                }
            }

            result.Append(Environment.NewLine + "};");
            e.Result = result.ToString();
        }
        #endregion

        #region Code2Image
        private void CodeToImage(object sender, DoWorkEventArgs e)
        {
            TransformColorFormats format = (TransformColorFormats)e.Argument;

            string Code = "";
            int Width = 0, Height = 0;
            int x = 0, y = 0;

            this.Invoke(new MethodInvoker(delegate
            {
                Width = (int)num_Width.Value;
                Height = (int)num_Height.Value;
                Code = GeneratedCode.Text;
            }));

            if (Width <= 0 || Height <= 0)
            {
                MessageBox.Show("Error:\nThe size of image can't be zero!");
                return;
            }

            // Some additional checks
            if (format == TransformColorFormats.BW_1bpp_H && Width % 8 != 0)
            {
                MessageBox.Show("Error:\nWidth has to be multiple 8!");
                return;
            }

            if (format == TransformColorFormats.BW_1bpp_V && Height % 8 != 0)
            {
                MessageBox.Show("Error:\nHeight has to be multiple 8!");
                return;
            }

            Bitmap result = new Bitmap(Width, Height);

            int pixelsCurrent = 0;
            int byteOffset = 0;
            int pixelsTotal = Width * Height;
            int pixelColor;

            // Two ways read symbol-by-symbol, or use regex. What faster?
            // Read from symbos "{", ignore tabs and spaces, read to "}". Use "," as separator.
            // Use regex and search pattern "0x([0-9a-fA-F]{2}),"
            string regex = "";
            switch (format)
            {
                case TransformColorFormats.BW_1bpp_C:
                case TransformColorFormats.BW_1bpp_H:
                case TransformColorFormats.BW_1bpp_V:
                case TransformColorFormats.RGB_332:
                    regex = @"0x([0-9a-fA-F]{2}),"; // 0xAB (one byte)
                    break;
                case TransformColorFormats.RGB_444:
                case TransformColorFormats.RGB_565:
                    regex = @"0x([0-9a-fA-F]{4}),"; // 0xABCD (two bytes)
                    break;
            }

            foreach (Match m in Regex.Matches(Code, regex))
            {
                pixelColor = int.Parse(m.Groups[1].Value, System.Globalization.NumberStyles.HexNumber);

                switch (format)
                {
                    case TransformColorFormats.BW_1bpp_C:
                    case TransformColorFormats.BW_1bpp_H:
                        for (byteOffset = 7; byteOffset >= 0; byteOffset--)
                        {
                            result.SetPixel(x, y, ((pixelColor & (1 << byteOffset)) > 0) ? Color.Black : Color.White);
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
                        }
                        x--;
                        break;
                    case TransformColorFormats.BW_1bpp_V:
                        for (int offset = 7; offset >= 0; offset--)
                        {
                            result.SetPixel(x, y + offset, ((pixelColor & (1 << offset)) > 0) ? Color.Black : Color.White);
                        }
                        break;
                    case TransformColorFormats.RGB_332:  // RRRG GGBB
                        result.SetPixel(x, y, color_from332(pixelColor));
                        break;
                    case TransformColorFormats.RGB_444:  // 0000 RRRR GGGG BBBB
                        result.SetPixel(x, y, color_from444(pixelColor));
                        break;
                    case TransformColorFormats.RGB_565:  // RRRR RGGG GGGB BBBB
                        result.SetPixel(x, y, color_from556(pixelColor));
                        break;
                }

                x++;
                pixelsCurrent++;

                if (x == Width)
                {
                    x = 0;
                    if (format == TransformColorFormats.BW_1bpp_V)
                        y += 8;
                    else
                        y += 1;

                    bgWork.ReportProgress((int)((double)pixelsCurrent / (double)pixelsTotal * 100));

                    if (y == Height)
                    {
                        break;  // All done. Stop conversion.
                    }
                }
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
            btn_Save.Enabled = true;
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
                    if (imageBox.Image.Width == 0 || imageBox.Image.Height == 0)
                    {
                        MessageBox.Show("Error!\nNo image loaded or unsupported image format.");
                        btn_Convert.Enabled = true;
                        return;
                    }
                    bgWork = new BackgroundWorker();
                    bgWork.WorkerReportsProgress = true;
                    bgWork.DoWork += new DoWorkEventHandler(ImageToCode);
                    bgWork.ProgressChanged += new ProgressChangedEventHandler(BgConvertProgress);
                    bgWork.RunWorkerCompleted += new RunWorkerCompletedEventHandler(ConvertDone);
                    bgWork.RunWorkerAsync(selBox_Format.SelectedItem);
                    break;
                case 1: // Code -> Image
                    if (string.IsNullOrWhiteSpace(GeneratedCode.Text))
                    {
                        MessageBox.Show("Error!\nEnter some code in Hex format (0x1234) devided by commas.");
                        btn_Convert.Enabled = true;
                        return;
                    }
                    bgWork = new BackgroundWorker();
                    bgWork.WorkerReportsProgress = true;
                    bgWork.DoWork += new DoWorkEventHandler(CodeToImage);
                    bgWork.ProgressChanged += new ProgressChangedEventHandler(BgConvertProgress);
                    bgWork.RunWorkerCompleted += new RunWorkerCompletedEventHandler(ConvertDone);
                    bgWork.RunWorkerAsync(selBox_Format.SelectedItem);
                    break;
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
        
        private void btn_Save_Click(object sender, EventArgs e)
        {
            SaveFileDialog sfd = new SaveFileDialog();
            sfd.Filter = "JPEG|*.jpg;*.jpeg;|PNG|*.png|BMP|*.bmp";

            if (sfd.ShowDialog() == DialogResult.OK)
            {
                string ext = Path.GetExtension(sfd.FileName);

                switch (ext)
                {
                    case ".jpg":
                    case ".jpeg":
                        EncoderParameters encParams = new EncoderParameters(1);
                        encParams.Param[0] = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, 90L); // 90%

                        ImageCodecInfo ici = null;
                        foreach (ImageCodecInfo codec in ImageCodecInfo.GetImageEncoders())
                            if (codec.MimeType == "image/jpeg")
                                ici = codec;
                        
                        imageBox.Image.Save(sfd.FileName, ici, encParams);

                        //imageBox.Image.Save(sfd.FileName, ImageFormat.Jpeg);
                        break;
                    case ".png":
                        imageBox.Image.Save(sfd.FileName, ImageFormat.Png);
                        break;
                    case ".bmp":
                        imageBox.Image.Save(sfd.FileName, ImageFormat.Bmp);
                        break;
                    default:
                        return;
                }
            }
        }
    }
}
