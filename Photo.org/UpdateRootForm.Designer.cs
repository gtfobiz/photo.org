namespace Photo.org
{
    partial class UpdateRootForm
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
            this.f_NewRoot = new System.Windows.Forms.TextBox();
            this.cmdBrowseNewPath = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.f_OldRoot = new System.Windows.Forms.ComboBox();
            this.cmdOk = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // f_NewRoot
            // 
            this.f_NewRoot.Location = new System.Drawing.Point(15, 79);
            this.f_NewRoot.Name = "f_NewRoot";
            this.f_NewRoot.Size = new System.Drawing.Size(443, 20);
            this.f_NewRoot.TabIndex = 0;
            // 
            // cmdBrowseNewPath
            // 
            this.cmdBrowseNewPath.Location = new System.Drawing.Point(464, 78);
            this.cmdBrowseNewPath.Name = "cmdBrowseNewPath";
            this.cmdBrowseNewPath.Size = new System.Drawing.Size(44, 20);
            this.cmdBrowseNewPath.TabIndex = 2;
            this.cmdBrowseNewPath.Text = "...";
            this.cmdBrowseNewPath.UseVisualStyleBackColor = true;
            this.cmdBrowseNewPath.Click += new System.EventHandler(this.cmdBrowseNewPath_Click);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(12, 12);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(107, 13);
            this.label1.TabIndex = 3;
            this.label1.Text = "Root folder to update";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(12, 62);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(98, 13);
            this.label2.TabIndex = 4;
            this.label2.Text = "New folder location";
            // 
            // f_OldRoot
            // 
            this.f_OldRoot.FormattingEnabled = true;
            this.f_OldRoot.Location = new System.Drawing.Point(15, 28);
            this.f_OldRoot.Name = "f_OldRoot";
            this.f_OldRoot.Size = new System.Drawing.Size(493, 21);
            this.f_OldRoot.TabIndex = 5;
            // 
            // cmdOk
            // 
            this.cmdOk.Location = new System.Drawing.Point(186, 141);
            this.cmdOk.Name = "cmdOk";
            this.cmdOk.Size = new System.Drawing.Size(206, 55);
            this.cmdOk.TabIndex = 6;
            this.cmdOk.Text = "Ok";
            this.cmdOk.UseVisualStyleBackColor = true;
            this.cmdOk.Click += new System.EventHandler(this.cmdOk_Click);
            // 
            // UpdateRootForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(566, 229);
            this.Controls.Add(this.cmdOk);
            this.Controls.Add(this.f_OldRoot);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.cmdBrowseNewPath);
            this.Controls.Add(this.f_NewRoot);
            this.Name = "UpdateRootForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "UpdateRootForm";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox f_NewRoot;
        private System.Windows.Forms.Button cmdBrowseNewPath;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.ComboBox f_OldRoot;
        private System.Windows.Forms.Button cmdOk;

    }
}