namespace Photo.org
{
    partial class FilterForm
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
            this.cmdFilter = new System.Windows.Forms.Button();
            this.f_FilterText = new System.Windows.Forms.TextBox();
            this.SuspendLayout();
            // 
            // cmdFilter
            // 
            this.cmdFilter.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.cmdFilter.Location = new System.Drawing.Point(238, 12);
            this.cmdFilter.Name = "cmdFilter";
            this.cmdFilter.Size = new System.Drawing.Size(75, 23);
            this.cmdFilter.TabIndex = 1;
            this.cmdFilter.Text = "Apply";
            this.cmdFilter.UseVisualStyleBackColor = true;
            // 
            // f_FilterText
            // 
            this.f_FilterText.Location = new System.Drawing.Point(12, 12);
            this.f_FilterText.Name = "f_FilterText";
            this.f_FilterText.Size = new System.Drawing.Size(201, 20);
            this.f_FilterText.TabIndex = 0;
            // 
            // FilterForm
            // 
            this.AcceptButton = this.cmdFilter;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(333, 47);
            this.Controls.Add(this.f_FilterText);
            this.Controls.Add(this.cmdFilter);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
            this.Name = "FilterForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Filter";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button cmdFilter;
        private System.Windows.Forms.TextBox f_FilterText;
    }
}