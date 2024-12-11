using System;
using System.Diagnostics;
using System.IO;
using System.ServiceProcess;
using System.Timers;

namespace GPOMonitoringService
{
    public partial class Service1 : ServiceBase
    {
        private Timer _timer;
        private readonly string _backupPath = @"C:\GPOBackups";

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
            WriteLog("All GPOs backed up successfully.");
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
