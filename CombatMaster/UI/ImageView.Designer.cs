namespace CombatMaster.UI
{
    partial class ImageView
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

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.pbox_Image = new System.Windows.Forms.PictureBox();
            ((System.ComponentModel.ISupportInitialize)(this.pbox_Image)).BeginInit();
            this.SuspendLayout();
            // 
            // pbox_Image
            // 
            this.pbox_Image.BackColor = System.Drawing.Color.White;
            this.pbox_Image.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pbox_Image.Location = new System.Drawing.Point(0, 0);
            this.pbox_Image.Name = "pbox_Image";
            this.pbox_Image.Size = new System.Drawing.Size(719, 577);
            this.pbox_Image.TabIndex = 0;
            this.pbox_Image.TabStop = false;
            this.pbox_Image.Click += new System.EventHandler(this.pbox_Image_Click);
            // 
            // ImageView
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(719, 577);
            this.Controls.Add(this.pbox_Image);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "ImageView";
            this.ShowIcon = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "ImageView";
            ((System.ComponentModel.ISupportInitialize)(this.pbox_Image)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.PictureBox pbox_Image;
    }
}