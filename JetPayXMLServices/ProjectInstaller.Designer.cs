namespace JetXmlService
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
            string DisplayName = "First Data Cash management Service Test";
            DisplayName = ErrorLog.ReadConfigFile("@ServiceDisplayName");

            this.serviceProcessInstaller1 = new System.ServiceProcess.ServiceProcessInstaller();
            this.FDCashManagement_Services = new System.ServiceProcess.ServiceInstaller();
            // 
            // serviceProcessInstaller1
            // 
            this.serviceProcessInstaller1.Account = System.ServiceProcess.ServiceAccount.LocalSystem;
            this.serviceProcessInstaller1.Password = null;
            this.serviceProcessInstaller1.Username = null;
            // 
            // FDCashManagement_Services
            // 
            this.FDCashManagement_Services.Description = "Cash Management MonthEnd Service";
            this.FDCashManagement_Services.DisplayName = DisplayName;
            this.FDCashManagement_Services.ServiceName = DisplayName;
            this.FDCashManagement_Services.StartType = System.ServiceProcess.ServiceStartMode.Automatic;
            // 
            // ProjectInstaller
            // 
            this.Installers.AddRange(new System.Configuration.Install.Installer[] {
            this.serviceProcessInstaller1,
            this.FDCashManagement_Services});

        }

        #endregion

        private System.ServiceProcess.ServiceProcessInstaller serviceProcessInstaller1;
        private System.ServiceProcess.ServiceInstaller FDCashManagement_Services;
    }
}