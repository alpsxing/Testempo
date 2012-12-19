namespace ServiceConfigurationApp
{
    partial class ServiceConfigurationApp
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
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ServiceConfigurationApp));
            this.niServiceConfiguration = new System.Windows.Forms.NotifyIcon(this.components);
            this.SuspendLayout();
            // 
            // niServiceConfiguration
            // 
            this.niServiceConfiguration.Icon = ((System.Drawing.Icon)(resources.GetObject("niServiceConfiguration.Icon")));
            this.niServiceConfiguration.Text = "Service Configuration";
            this.niServiceConfiguration.Visible = true;
            this.niServiceConfiguration.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler(this.NIServiceConfig_MouseDoubleClick);
            // 
            // ServiceConfigurationApp
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(784, 562);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "ServiceConfigurationApp";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Service Configuration";
            this.SizeChanged += new System.EventHandler(this.Window_SizeChanged);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.NotifyIcon niServiceConfiguration;
    }
}

