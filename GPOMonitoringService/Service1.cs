using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.ServiceProcess;
using System.Timers;
using System.Xml.Linq; // XML işlemleri için

namespace GPOMonitoringService
{
    public partial class Service1 : ServiceBase
    {
        private Timer _timer;
        private readonly string _backupPath = @"C:\GPOBackups";
        private readonly string _xmlPath = Path.Combine(@"C:\GPOBackups", "GPOBackupHistory.xml");

        public Service1()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            // Yedekleme dizini oluşturulmadıysa oluştur
            if (!Directory.Exists(_backupPath))
            {
                Directory.CreateDirectory(_backupPath);
            }

            // XML dosyası oluşturulmadıysa oluştur
            if (!File.Exists(_xmlPath))
            {
                var xdoc = new XDocument(new XElement("GPOBackups"));
                xdoc.Save(_xmlPath);
            }

            // Timer başlat
            _timer = new Timer(60000); // 60 saniyede bir kontrol et
            _timer.Elapsed += OnTimerElapsed;
            _timer.Start();

            // Başlatma log'u
            WriteLog("GPOMonitoringService started.");
        }

        protected override void OnStop()
        {
            if (_timer != null)
            {
                _timer.Stop();
                _timer.Dispose();
            }

            // Durdurma log'u
            WriteLog("GPOMonitoringService stopped.");
        }

        private void OnTimerElapsed(object sender, ElapsedEventArgs e)
        {
            try
            {
                BackupAllGPOs(); // Tüm GPO'ları yedekle
            }
            catch (Exception ex)
            {
                WriteLog($"Error during GPO backup: {ex.Message}");
            }
        }

        private void BackupAllGPOs()
        {
            // PowerShell komutunu çalıştırarak tüm GPO'ları yedekle
            var powershellCommand = $"Backup-GPO -All -Path \"{_backupPath}\"";
            ExecutePowerShellCommand(powershellCommand);

            // Yedeklenen GPO'lar için XML dosyasını güncelle
            UpdateBackupHistory();

            WriteLog("All GPOs backed up successfully.");
        }

        private void UpdateBackupHistory()
        {
            try
            {
                var xdoc = XDocument.Load(_xmlPath);

                foreach (var folder in Directory.GetDirectories(_backupPath))
                {
                    var gpoName = Path.GetFileName(folder);
                    var gpoBackupTime = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss");

                    // GPO için yeni bir kayıt oluştur veya mevcut olanı güncelle
                    var existingGpo = xdoc.Descendants("Gpo").FirstOrDefault(gpo => gpo.Element("Name")?.Value == gpoName);

                    if (existingGpo == null)
                    {
                        xdoc.Root.Add(new XElement("Gpo",
                            new XElement("Name", gpoName),
                            new XElement("LastModified", gpoBackupTime),
                            new XElement("BackupPath", folder)));
                    }
                    else
                    {
                        existingGpo.Element("LastModified")?.SetValue(gpoBackupTime);
                        existingGpo.Element("BackupPath")?.SetValue(folder);
                    }
                }

                xdoc.Save(_xmlPath);
            }
            catch (Exception ex)
            {
                WriteLog($"Error updating XML: {ex.Message}");
            }
        }

        private void ExecutePowerShellCommand(string command)
        {
            using (var process = new Process())
            {
                process.StartInfo.FileName = "powershell.exe";
                process.StartInfo.Arguments = $"-NoProfile -ExecutionPolicy Bypass -Command \"{command}\"";
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardError = true;
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.CreateNoWindow = true;

                process.Start();
                var output = process.StandardOutput.ReadToEnd();
                var error = process.StandardError.ReadToEnd();

                if (!string.IsNullOrEmpty(error))
                {
                    WriteLog($"PowerShell Error: {error}");
                }

                WriteLog(output);
                process.WaitForExit();
            }
        }

        private void WriteLog(string message)
        {
            // Log dosyasına yaz
            var logPath = Path.Combine(_backupPath, "ServiceLog.txt");
            File.AppendAllText(logPath, $"{DateTime.Now}: {message}{Environment.NewLine}");
        }
    }
}