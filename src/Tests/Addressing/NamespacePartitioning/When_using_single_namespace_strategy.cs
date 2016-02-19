namespace NServiceBus.AzureServiceBus.Tests
{
    using System.Configuration;
    using System.Linq;
    using NServiceBus.AzureServiceBus.Addressing;
    using NServiceBus.Settings;
    using NUnit.Framework;

    [TestFixture]
    [Category("AzureServiceBus")]
    public class When_using_single_namespace_strategy
    {
        private static readonly string ConnectionString = "Endpoint=sb://namespace1.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=somesecretkey";
        private static readonly string Other = "Endpoint=sb://namespace2.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=somesecretkey";

        private SingleNamespacePartitioningStrategy _strategy;

        [SetUp]
        public void SetUp()
        {
            var settings = new SettingsHolder();
            var extensions = new TransportExtensions<AzureServiceBusTransport>(settings);
            extensions.UseDefaultTopology().Addressing().NamespacePartitioning().AddNamespace("namespace1", ConnectionString);

            _strategy = new SingleNamespacePartitioningStrategy(settings);
        }

        [Test]
        public void Single_partitioning_strategy_will_return_a_single_connectionstring_for_the_purpose_of_creating()
        {
            var namespaces = _strategy.GetNamespaces("endpoint1", PartitioningIntent.Creating);

            Assert.AreEqual(1, namespaces.Count());
        }

        [Test]
        public void Single_partitioning_strategy_will_return_a_single_connectionstring_for_the_purpose_of_receiving()
        {
            var namespaces = _strategy.GetNamespaces("endpoint1", PartitioningIntent.Receiving);

            Assert.AreEqual(1, namespaces.Count());
        }

        [Test]
        public void Single_partitioning_strategy_will_return_a_single_connectionstring_for_the_purpose_of_sending()
        {
            var namespaces = _strategy.GetNamespaces("endpoint1", PartitioningIntent.Sending);

            Assert.AreEqual(1, namespaces.Count());
        }

        [Test]
        public void Single_partitioning_strategy_will_return_configured_namespace_for_any_endpoint_name()
        {
            var namespaces = _strategy.GetNamespaces("endpoint1", PartitioningIntent.Creating);

            Assert.AreEqual(new NamespaceInfo(ConnectionString, NamespaceMode.Active), namespaces.First());
        }

        [Test]
        public void Single_partitioning_strategy_will_throw_if_no_namespace_defined()
        {
            var settings = new SettingsHolder();
            
            Assert.Throws<ConfigurationErrorsException>(() => new SingleNamespacePartitioningStrategy(settings));
        }

        [Test]
        public void Single_partitioning_strategy_will_throw_if_more_namespaces_defined()
        {
            var settings = new SettingsHolder();
            var extensions = new TransportExtensions<AzureServiceBusTransport>(settings);
            extensions.UseDefaultTopology().Addressing().NamespacePartitioning().AddNamespace("namespace1", ConnectionString);
            extensions.UseDefaultTopology().Addressing().NamespacePartitioning().AddNamespace("namespace2", Other);

            Assert.Throws<ConfigurationErrorsException>(() => new SingleNamespacePartitioningStrategy(settings));
        }
    }

}