namespace Image2Bitmap
{
    partial class Form1
    {
        /// <summary>
        /// Обязательная переменная конструктора.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Освободить все используемые ресурсы.
        /// </summary>
        /// <param name="disposing">истинно, если управляемый ресурс должен быть удален; иначе ложно.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Код, автоматически созданный конструктором форм Windows

        /// <summary>
        /// Требуемый метод для поддержки конструктора — не изменяйте 
        /// содержимое этого метода с помощью редактора кода.
        /// </summary>
        private void InitializeComponent()
        {
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.btn_open = new System.Windows.Forms.Button();
            this.txt_imgSize = new System.Windows.Forms.Label();
            this.selBox_Format = new System.Windows.Forms.ComboBox();
            this.imageBox = new System.Windows.Forms.PictureBox();
            this.tabBox = new System.Windows.Forms.TabControl();
            this.page_Image = new System.Windows.Forms.TabPage();
            this.page_Code = new System.Windows.Forms.TabPage();
            this.GeneratedCode = new System.Windows.Forms.TextBox();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.btn_Convert = new System.Windows.Forms.Button();
            this.groupBox1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.imageBox)).BeginInit();
            this.tabBox.SuspendLayout();
            this.page_Image.SuspendLayout();
            this.page_Code.SuspendLayout();
            this.groupBox2.SuspendLayout();
            this.SuspendLayout();
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.btn_open);
            this.groupBox1.Controls.Add(this.txt_imgSize);
            this.groupBox1.Location = new System.Drawing.Point(12, 12);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(200, 108);
            this.groupBox1.TabIndex = 0;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Original image:";
            // 
            // btn_open
            // 
            this.btn_open.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.btn_open.Location = new System.Drawing.Point(9, 73);
            this.btn_open.Name = "btn_open";
            this.btn_open.Size = new System.Drawing.Size(188, 29);
            this.btn_open.TabIndex = 0;
            this.btn_open.Text = "Open image";
            this.btn_open.UseVisualStyleBackColor = true;
            this.btn_open.Click += new System.EventHandler(this.Btn_open_Click);
            // 
            // txt_imgSize
            // 
            this.txt_imgSize.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txt_imgSize.Location = new System.Drawing.Point(6, 16);
            this.txt_imgSize.Name = "txt_imgSize";
            this.txt_imgSize.Size = new System.Drawing.Size(188, 52);
            this.txt_imgSize.TabIndex = 4;
            this.txt_imgSize.Text = "Info:\r\nWidth:\r\nHeight:\r\nFormat:";
            // 
            // selBox_Format
            // 
            this.selBox_Format.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.selBox_Format.FormattingEnabled = true;
            this.selBox_Format.Location = new System.Drawing.Point(6, 19);
            this.selBox_Format.Name = "selBox_Format";
            this.selBox_Format.Size = new System.Drawing.Size(188, 21);
            this.selBox_Format.TabIndex = 1;
            // 
            // imageBox
            // 
            this.imageBox.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.imageBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.imageBox.Location = new System.Drawing.Point(3, 3);
            this.imageBox.Name = "imageBox";
            this.imageBox.Size = new System.Drawing.Size(340, 245);
            this.imageBox.TabIndex = 1;
            this.imageBox.TabStop = false;
            // 
            // tabBox
            // 
            this.tabBox.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tabBox.Controls.Add(this.page_Image);
            this.tabBox.Controls.Add(this.page_Code);
            this.tabBox.Location = new System.Drawing.Point(218, 12);
            this.tabBox.Name = "tabBox";
            this.tabBox.SelectedIndex = 0;
            this.tabBox.Size = new System.Drawing.Size(354, 277);
            this.tabBox.TabIndex = 3;
            // 
            // page_Image
            // 
            this.page_Image.Controls.Add(this.imageBox);
            this.page_Image.Location = new System.Drawing.Point(4, 22);
            this.page_Image.Name = "page_Image";
            this.page_Image.Padding = new System.Windows.Forms.Padding(3);
            this.page_Image.Size = new System.Drawing.Size(346, 251);
            this.page_Image.TabIndex = 0;
            this.page_Image.Text = "Image";
            this.page_Image.UseVisualStyleBackColor = true;
            // 
            // page_Code
            // 
            this.page_Code.Controls.Add(this.GeneratedCode);
            this.page_Code.Location = new System.Drawing.Point(4, 22);
            this.page_Code.Name = "page_Code";
            this.page_Code.Padding = new System.Windows.Forms.Padding(3);
            this.page_Code.Size = new System.Drawing.Size(346, 251);
            this.page_Code.TabIndex = 1;
            this.page_Code.Text = "Code";
            this.page_Code.UseVisualStyleBackColor = true;
            // 
            // GeneratedCode
            // 
            this.GeneratedCode.Dock = System.Windows.Forms.DockStyle.Fill;
            this.GeneratedCode.Location = new System.Drawing.Point(3, 3);
            this.GeneratedCode.Multiline = true;
            this.GeneratedCode.Name = "GeneratedCode";
            this.GeneratedCode.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.GeneratedCode.Size = new System.Drawing.Size(340, 245);
            this.GeneratedCode.TabIndex = 0;
            this.GeneratedCode.WordWrap = false;
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.btn_Convert);
            this.groupBox2.Controls.Add(this.selBox_Format);
            this.groupBox2.Location = new System.Drawing.Point(12, 126);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(200, 82);
            this.groupBox2.TabIndex = 3;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "Convert to:";
            // 
            // btn_Convert
            // 
            this.btn_Convert.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.btn_Convert.Location = new System.Drawing.Point(6, 46);
            this.btn_Convert.Name = "btn_Convert";
            this.btn_Convert.Size = new System.Drawing.Size(188, 29);
            this.btn_Convert.TabIndex = 2;
            this.btn_Convert.Text = "Convert!";
            this.btn_Convert.UseVisualStyleBackColor = true;
            this.btn_Convert.Click += new System.EventHandler(this.btn_Convert_Click);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(584, 301);
            this.Controls.Add(this.groupBox2);
            this.Controls.Add(this.tabBox);
            this.Controls.Add(this.groupBox1);
            this.MinimumSize = new System.Drawing.Size(600, 340);
            this.Name = "Form1";
            this.Text = "Image2Bitmap";
            this.Load += new System.EventHandler(this.Form1_Load);
            this.groupBox1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.imageBox)).EndInit();
            this.tabBox.ResumeLayout(false);
            this.page_Image.ResumeLayout(false);
            this.page_Code.ResumeLayout(false);
            this.page_Code.PerformLayout();
            this.groupBox2.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.Label txt_imgSize;
        private System.Windows.Forms.ComboBox selBox_Format;
        private System.Windows.Forms.PictureBox imageBox;
        private System.Windows.Forms.Button btn_open;
        private System.Windows.Forms.TabControl tabBox;
        private System.Windows.Forms.TabPage page_Image;
        private System.Windows.Forms.TabPage page_Code;
        private System.Windows.Forms.TextBox GeneratedCode;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.Button btn_Convert;
    }
}

