using System.ServiceProcess;

namespace GPOMonitoringService
{
    internal static class Program
    {
        static void Main()
        {
            ServiceBase[] ServicesToRun;
            ServicesToRun = new ServiceBase[]
            {
                new Service1() // Servisi başlat
            };
            ServiceBase.Run(ServicesToRun);
        }
    }
}
