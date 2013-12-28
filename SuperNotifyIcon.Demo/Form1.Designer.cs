namespace Zhwang.SuperNotifyIcon.Demo
{
    partial class Form1
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
            this.buttonLocationRefresh = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.labelLocation = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // buttonLocationRefresh
            // 
            this.buttonLocationRefresh.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonLocationRefresh.Location = new System.Drawing.Point(297, 12);
            this.buttonLocationRefresh.Name = "buttonLocationRefresh";
            this.buttonLocationRefresh.Size = new System.Drawing.Size(75, 23);
            this.buttonLocationRefresh.TabIndex = 0;
            this.buttonLocationRefresh.Text = "Refresh";
            this.buttonLocationRefresh.Click += new System.EventHandler(this.buttonLocationRefresh_Click);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(12, 17);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(98, 13);
            this.label1.TabIndex = 1;
            this.label1.Text = "NotifyIcon location:";
            // 
            // labelLocation
            // 
            this.labelLocation.AutoSize = true;
            this.labelLocation.Location = new System.Drawing.Point(107, 17);
            this.labelLocation.Name = "labelLocation";
            this.labelLocation.Size = new System.Drawing.Size(27, 13);
            this.labelLocation.TabIndex = 2;
            this.labelLocation.Text = "blah";
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(384, 46);
            this.Controls.Add(this.labelLocation);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.buttonLocationRefresh);
            this.Name = "Form1";
            this.Text = "SuperNotifyIcon Location Demo";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button buttonLocationRefresh;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label labelLocation;
    }
}

