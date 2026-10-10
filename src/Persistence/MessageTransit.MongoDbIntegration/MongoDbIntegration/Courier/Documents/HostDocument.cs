namespace MessageTransit.MongoDbIntegration.Courier.Documents
{
    public class HostDocument
    {
        public HostDocument(HostInfo host)
        {
            MachineName = host.MachineName;
            ProcessId = host.ProcessId;
            ProcessName = host.ProcessName;
            Assembly = host.Assembly;
            AssemblyVersion = host.AssemblyVersion;
            MessageTransitVersion = host.MessageTransitVersion;
            FrameworkVersion = host.FrameworkVersion;
            OperatingSystemVersion = host.OperatingSystemVersion;
        }

        public string MachineName { get; private set; }
        public string ProcessName { get; private set; }
        public int ProcessId { get; private set; }
        public string Assembly { get; private set; }
        public string AssemblyVersion { get; private set; }
        public string FrameworkVersion { get; private set; }
        public string MessageTransitVersion { get; private set; }
        public string OperatingSystemVersion { get; private set; }
    }
}
