namespace NServiceBus.AzureServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using NServiceBus.Settings;
    using NServiceBus.Transports;

    class ManageRightsCheck 
    {
        private readonly IManageNamespaceManagerLifeCycle manageNamespaceManagerLifeCycle;
        private readonly ReadOnlySettings settings;

        public ManageRightsCheck(ITransportPartsContainer container)
        {
            this.manageNamespaceManagerLifeCycle = container.Resolve<IManageNamespaceManagerLifeCycle>();
            this.settings = container.Resolve<ReadOnlySettings>();
        }

        public async Task<StartupCheckResult> Run()
        {
            if (!settings.Get<bool>(WellKnownConfigurationKeys.Core.CreateTopology))
                return StartupCheckResult.Success;

            var namespaces = new List<string>();
            foreach (var @namespace in settings.Get<List<string>>(WellKnownConfigurationKeys.Topology.Addressing.Partitioning.Namespaces))
            {
                var namespaceManager = manageNamespaceManagerLifeCycle.Get(@namespace);
                var canManageEntities = await namespaceManager.CanManageEntities();

                if (!canManageEntities)
                    namespaces.Add(@namespace);
            }

            if (!namespaces.Any())
                return StartupCheckResult.Success;

            return StartupCheckResult.Failed($"Manage rights on namespace(s) is required if {WellKnownConfigurationKeys.Core.CreateTopology} setting is true." +
                                             $"Configure namespace(s) {namespaces.Aggregate((curr, next) => String.Concat(curr, ", ", next))} with manage rights");
        }
    }
}