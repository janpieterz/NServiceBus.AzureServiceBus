﻿namespace NServiceBus.Transport.AzureServiceBus
{
    public interface IBrokerSideSubscriptionFilter
    {
        /// <summary>
        /// serialized the filter into native format, so that it can be injected into the broker (subscription case)
        /// </summary>
        string Serialize();
    }
}