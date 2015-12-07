namespace NServiceBus.AzureServiceBus
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.ServiceBus.Messaging;

    public class TopologyOperator : IOperateTopology
    {
        readonly ITransportPartsContainer container;

        TopologySection topology;

        Func<IncomingMessageDetails, ReceiveContext, Task> onMessage;
        Func<Exception, Task> onError;

        ConcurrentDictionary<EntityInfo, INotifyIncomingMessages> notifiers = new ConcurrentDictionary<EntityInfo, INotifyIncomingMessages>();

        CancellationTokenSource cancellationTokenSource;

        bool running = false;
        List<Action> pendingStartOperations = new List<Action>();

        int maxConcurrency;

        public TopologyOperator(ITransportPartsContainer container)
        {
            this.container = container;
        }

        public void Start(TopologySection topologySection, int maximumConcurrency)
        {
            maxConcurrency = maximumConcurrency;
            topology = topologySection;

            cancellationTokenSource = new CancellationTokenSource();

            StartNotifiersFor(topology.Entities);

            foreach (var operation in pendingStartOperations)
            {
                operation();
            }

            pendingStartOperations = new List<Action>();
            running = true;
        }

        public Task Stop()
        {
            cancellationTokenSource.Cancel();

            return StopNotifiersForAsync(topology.Entities);
        }

        public void Start(IEnumerable<EntityInfo> subscriptions)
        {
            if (!running) // cannot start subscribers before the notifier itself is started
            {
                pendingStartOperations.Add(() => StartNotifiersFor(subscriptions));
            }
            else
            {
                StartNotifiersFor(subscriptions);
            }
        }

        public Task Stop(IEnumerable<EntityInfo> subscriptions)
        {
            return StopNotifiersForAsync(subscriptions);
        }

        public void OnIncomingMessage(Func<IncomingMessageDetails, ReceiveContext, Task> func)
        {
            onMessage = func;
        }

        public void OnError(Func<Exception, Task> func)
        {
            onError = func;
        }

        void StartNotifiersFor(IEnumerable<EntityInfo> entities)
        {
            foreach (var entity in entities)
            {
                var notifier = notifiers.GetOrAdd(entity, e =>
                {
                    var n = CreateNotifier(entity.Type);
                    var path = e.Path;
                    if (entity.Type == EntityType.Subscription)
                    {
                        var topic = entity.RelationShips.First(r => r.Type == EntityRelationShipType.Subscription);
                        path = SubscriptionClient.FormatSubscriptionPath(topic.Target.Path, path);
                    }
                    n.Initialize(path, e.Namespace.ConnectionString, onMessage, onError, maxConcurrency);
                    return n;
                });

                if (!notifier.IsRunning)
                {
                    notifier.Start();
                }
                else
                {
                    notifier.RefCount++;
                }
                
            }
        }

        INotifyIncomingMessages CreateNotifier(EntityType type)
        {
            if (type == EntityType.Queue || type == EntityType.Subscription)
            {
                return (INotifyIncomingMessages)container.Resolve(typeof(MessageReceiverNotifier));
            }

            throw new NotSupportedException("Entity type " + type + " not supported");
        }

        async Task StopNotifiersForAsync(IEnumerable<EntityInfo> entities)
        {
            foreach (var entity in entities)
            {
                INotifyIncomingMessages notifier;
                notifiers.TryGetValue(entity, out notifier);

                if (notifier == null || !notifier.IsRunning) continue;

                if (notifier.RefCount > 0)
                {
                    notifier.RefCount--;
                }
                else
                {
                    await notifier.Stop().ConfigureAwait(false);
                }
            }
        }
    }
}