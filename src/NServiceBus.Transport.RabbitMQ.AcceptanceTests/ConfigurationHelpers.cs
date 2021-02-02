using NServiceBus;
using NServiceBus.Configuration.AdvancedExtensibility;
using NServiceBus.Transport;

static class ConfigurationHelpers
{
    public static RabbitMQTransport ConfigureRabbitMQTransport(this EndpointConfiguration configuration)
    {
        return (RabbitMQTransport) configuration.GetSettings().Get<TransportDefinition>();
    }
}