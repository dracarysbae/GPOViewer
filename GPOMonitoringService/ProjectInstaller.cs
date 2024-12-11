using System.ComponentModel;
using System.ServiceProcess;
using System.Configuration.Install;

[RunInstaller(true)]
public class ProjectInstaller : Installer
{
    public ProjectInstaller()
    {
        var processInstaller = new ServiceProcessInstaller
        {
            Account = ServiceAccount.LocalSystem // Yerel sistem hesabıyla çalıştır
        };

        var serviceInstaller = new ServiceInstaller
        {
            ServiceName = "GPOMonitoringService",
            StartType = ServiceStartMode.Automatic // Otomatik başlasın
        };

        Installers.Add(processInstaller);
        Installers.Add(serviceInstaller);
    }
}
