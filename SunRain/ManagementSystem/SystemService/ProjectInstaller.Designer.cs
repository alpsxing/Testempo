namespace SystemService
{
    partial class ProjectInstaller
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
            this.infoTransServiceProcessInstaller = new System.ServiceProcess.ServiceProcessInstaller();
            this.infoTransServiceInstaller = new System.ServiceProcess.ServiceInstaller();
            // 
            // infoTransServiceProcessInstaller
            // 
            this.infoTransServiceProcessInstaller.Account = System.ServiceProcess.ServiceAccount.LocalSystem;
            this.infoTransServiceProcessInstaller.Password = null;
            this.infoTransServiceProcessInstaller.Username = null;
            // 
            // infoTransServiceInstaller
            // 
            this.infoTransServiceInstaller.Description = "DTU Management Service";
            this.infoTransServiceInstaller.DisplayName = "DTU Management";
            this.infoTransServiceInstaller.ServiceName = "DTU Management";
            this.infoTransServiceInstaller.StartType = System.ServiceProcess.ServiceStartMode.Automatic;
            // 
            // ProjectInstaller
            // 
            this.Installers.AddRange(new System.Configuration.Install.Installer[] {
            this.infoTransServiceProcessInstaller,
            this.infoTransServiceInstaller});

        }

        #endregion

        private System.ServiceProcess.ServiceProcessInstaller infoTransServiceProcessInstaller;
        private System.ServiceProcess.ServiceInstaller infoTransServiceInstaller;
    }
}