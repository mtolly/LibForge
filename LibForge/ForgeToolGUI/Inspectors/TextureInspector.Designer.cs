namespace ForgeToolGUI
{
  partial class ImageInspector
  {
    /// <summary> 
    /// Required designer variable.
    /// </summary>
    private System.ComponentModel.IContainer components = null;

    /// <summary> 
    /// Clean up any resources being used.
    /// </summary>
    /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
    protected override void Dispose(bool disposing)
    {
      if (disposing && (components != null))
      {
        components.Dispose();
      }
      base.Dispose(disposing);
    }

    #region Component Designer generated code

    /// <summary> 
    /// Required method for Designer support - do not modify 
    /// the contents of this method with the code editor.
    /// </summary>
    private void InitializeComponent()
    {
            this.pictureBox1 = new System.Windows.Forms.PictureBox();
            this.DecompressList = new System.Windows.Forms.ComboBox();
            this.Label = new System.Windows.Forms.Label();
            this.Save = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            this.SuspendLayout();
            // 
            // pictureBox1
            // 
            this.pictureBox1.Location = new System.Drawing.Point(0, 26);
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.Size = new System.Drawing.Size(91, 70);
            this.pictureBox1.TabIndex = 3;
            this.pictureBox1.TabStop = false;
            // 
            // DecompressList
            // 
            this.DecompressList.FormattingEnabled = true;
            this.DecompressList.Items.AddRange(new object[] {
            "default",
            "BC3",
            "BC4",
            "BC5",
            "R8G8"});
            this.DecompressList.Location = new System.Drawing.Point(97, 3);
            this.DecompressList.Name = "DecompressList";
            this.DecompressList.Size = new System.Drawing.Size(121, 21);
            this.DecompressList.TabIndex = 5;
            this.DecompressList.SelectedIndexChanged += new System.EventHandler(this.comboBox1_SelectedIndexChanged);
            // 
            // Label
            // 
            this.Label.AutoSize = true;
            this.Label.Location = new System.Drawing.Point(3, 6);
            this.Label.Name = "Label";
            this.Label.Size = new System.Drawing.Size(88, 13);
            this.Label.TabIndex = 6;
            this.Label.Text = "Decompress with";
            this.Label.Click += new System.EventHandler(this.label1_Click);
            // 
            // Save
            // 
            this.Save.Location = new System.Drawing.Point(237, 3);
            this.Save.Name = "Save";
            this.Save.Size = new System.Drawing.Size(75, 23);
            this.Save.TabIndex = 7;
            this.Save.Text = "Save";
            this.Save.UseVisualStyleBackColor = true;
            this.Save.Click += new System.EventHandler(this.button1_Click);
            // 
            // ImageInspector
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoScroll = true;
            this.Controls.Add(this.Save);
            this.Controls.Add(this.Label);
            this.Controls.Add(this.DecompressList);
            this.Controls.Add(this.pictureBox1);
            this.Name = "ImageInspector";
            this.Size = new System.Drawing.Size(409, 288);
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

    }

    #endregion

    private System.Windows.Forms.PictureBox pictureBox1;
    private System.Windows.Forms.ComboBox DecompressList;
    private System.Windows.Forms.Label Label;
    private System.Windows.Forms.Button Save;
  }
}
