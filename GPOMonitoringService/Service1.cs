using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.ServiceProcess;
using System.Timers;
using System.Xml.Linq; // XML işlemleri için
using GPMGMTLib; // GPMC Namespace

namespace GPOMonitoringService
{
    public partial class Service1 : ServiceBase
    {
        private Timer _timer;
        private readonly string _backupPath = @"C:\GPOBackups";
        private readonly string _xmlPath = @"C:\GPOBackups\GPOBackupHistory.xml";

        public Service1()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            // Create backup directory if it does not exist
            if (!Directory.Exists(_backupPath))
            {
                Directory.CreateDirectory(_backupPath);
            }

            // Create XML file if it does not exist
            if (!File.Exists(_xmlPath))
            {
                var xdoc = new XDocument(new XElement("GPOBackups"));
                xdoc.Save(_xmlPath);
            }

            // Start timer to trigger backups every 60 seconds
            _timer = new Timer(60000);
            _timer.Elapsed += OnTimerElapsed;
            _timer.Start();

            WriteLog("GPOMonitoringService started.");
        }

        protected override void OnStop()
        {
            if (_timer != null)
            {
                _timer.Stop();
                _timer.Dispose();
            }

            WriteLog("GPOMonitoringService stopped.");
        }

        private void OnTimerElapsed(object sender, ElapsedEventArgs e)
        {
            try
            {
                BackupAllGPOs();
            }
            catch (Exception ex)
            {
                WriteLog($"Error during GPO backup: {ex.Message}");
            }
        }

        private void BackupAllGPOs()
        {
            try
            {
                GPM gpm = new GPM();
                GPMDomain domain = (GPMDomain)gpm.GetDomain("WORKGROUP", null, 0); // Replace "WORKGROUP" with your domain name
                GPMSearchCriteria searchCriteria = gpm.CreateSearchCriteria();
                GPMGPOCollection gpos = domain.SearchGPOs(searchCriteria);

                var xdoc = XDocument.Load(_xmlPath);

                foreach (GPMGPO gpo in gpos)
                {
                    string gpoBackupDir = Path.Combine(_backupPath, $"{gpo.DisplayName}_{DateTime.Now:yyyyMMddHHmmss}");
                    Directory.CreateDirectory(gpoBackupDir);

                    object progress = null;
                    object cancel = null;

                    gpo.Backup(gpoBackupDir, "Backup created by GPOMonitoringService", ref progress, out cancel);
                    WriteLog($"Backed up GPO: {gpo.DisplayName}");

                    // Update or add GPO record in XML
                    var existingGpo = xdoc.Descendants("GPO")
                        .FirstOrDefault(x => x.Element("Name")?.Value == gpo.DisplayName);

                    if (existingGpo == null)
                    {
                        xdoc.Root.Add(new XElement("GPO",
                            new XElement("Name", gpo.DisplayName),
                            new XElement("LastModified", DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss")),
                            new XElement("BackupPath", gpoBackupDir)
                        ));
                    }
                    else
                    {
                        existingGpo.Element("LastModified")?.SetValue(DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss"));
                        existingGpo.Element("BackupPath")?.SetValue(gpoBackupDir);
                    }
                }

                xdoc.Save(_xmlPath);
                WriteLog("All GPOs backed up and history updated successfully.");
            }
            catch (Exception ex)
            {
                WriteLog($"Error during GPO backup: {ex.Message}");
            }
        }

        private void WriteLog(string message)
        {
            string logPath = Path.Combine(_backupPath, "ServiceLog.txt");
            File.AppendAllText(logPath, $"{DateTime.Now}: {message}{Environment.NewLine}");
        }
    }
}
