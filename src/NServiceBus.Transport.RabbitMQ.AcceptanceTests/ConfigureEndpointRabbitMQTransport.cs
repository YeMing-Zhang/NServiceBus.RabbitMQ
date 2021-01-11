using System;
using System.Data.Common;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.AcceptanceTesting.Support;
using NServiceBus.Transport.RabbitMQ;
using RabbitMQ.Client;

class ConfigureEndpointRabbitMQTransport : IConfigureEndpointTestExecution
{
    DbConnectionStringBuilder connectionStringBuilder;

    public Task Configure(string endpointName, EndpointConfiguration configuration, RunSettings settings, PublisherMetadata publisherMetadata)
    {
        var connectionString = Environment.GetEnvironmentVariable("RabbitMQTransport_ConnectionString");

        if (string.IsNullOrEmpty(connectionString))
        {
            connectionString = "host=localhost";
            //throw new Exception("The 'RabbitMQTransport_ConnectionString' environment variable is not set.");
        }

        connectionStringBuilder = new DbConnectionStringBuilder { ConnectionString = connectionString };

        //TODO: Parse any settings in the connection string 
        var transport = new RabbitMQTransport {Host = (string) connectionStringBuilder["Host"] };
        transport.RoutingTopology = new ConventionalRoutingTopology(true, t => t.FullName); //distinguish nested types
        configuration.UseTransport(transport);

        return Task.CompletedTask;
    }

    public Task Cleanup()
    {
        PurgeQueues();

        return Task.CompletedTask;
    }

    void PurgeQueues()
    {
        if (connectionStringBuilder == null)
        {
            return;
        }

        var connectionFactory = new ConnectionFactory
        {
            AutomaticRecoveryEnabled = true,
            UseBackgroundThreadsForIO = true
        };

        if (connectionStringBuilder.TryGetValue("username", out var value))
        {
            connectionFactory.UserName = value.ToString();
        }

        if (connectionStringBuilder.TryGetValue("password", out value))
        {
            connectionFactory.Password = value.ToString();
        }

        if (connectionStringBuilder.TryGetValue("virtualhost", out value))
        {
            connectionFactory.VirtualHost = value.ToString();
        }

        if (connectionStringBuilder.TryGetValue("host", out value))
        {
            connectionFactory.HostName = value.ToString();
        }
        else
        {
            throw new Exception("The connection string doesn't contain a value for 'host'.");
        }

        //TODO: Cleanup
        //var queues = queueBindings.ReceivingAddresses.Concat(queueBindings.SendingAddresses);

        //using (var connection = connectionFactory.CreateConnection("Test Queue Purger"))
        //using (var channel = connection.CreateModel())
        //{
        //    foreach (var queue in queues)
        //    {
        //        try
        //        {
        //            channel.QueuePurge(queue);
        //        }
        //        catch (Exception ex)
        //        {
        //            Console.WriteLine("Unable to clear queue {0}: {1}", queue, ex);
        //        }
        //    }
        //}
    }
}