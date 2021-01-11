using System;
using System.Data.Common;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.Transport;
using NServiceBus.TransportTests;
using RabbitMQ.Client;

class ConfigureRabbitMQTransportInfrastructure : IConfigureTransportInfrastructure
{
    public async Task<TransportConfigurationResult> Configure(HostSettings hostSettings, string inputQueueName, string errorQueueName,
        TransportTransactionMode transactionMode)
    {
        var connectionString = Environment.GetEnvironmentVariable("RabbitMQTransport_ConnectionString");

        if (string.IsNullOrEmpty(connectionString))
        {
            connectionString = "host=localhost";
            //throw new Exception("The 'RabbitMQTransport_ConnectionString' environment variable is not set.");
        }

        queuesToCleanUp = new[] { inputQueueName, errorQueueName };

        connectionStringBuilder = new DbConnectionStringBuilder { ConnectionString = connectionString };

        //TODO: Parse any settings in the connection string 
        var transport = new RabbitMQTransport { Host = (string)connectionStringBuilder["Host"] };

        var mainReceiverSettings = new ReceiveSettings(
            "mainReceiver",
            inputQueueName,
            transport.SupportsPublishSubscribe,
            true, errorQueueName);

        return new TransportConfigurationResult
        {
            TransportDefinition = transport,
            TransportInfrastructure = await transport.Initialize(hostSettings, new[] { mainReceiverSettings }, new[] { errorQueueName }),
            PurgeInputQueueOnStartup = true
        };
    }

    public Task Cleanup()
    {
        PurgeQueues(connectionStringBuilder, queuesToCleanUp);
        return Task.FromResult(0);
    }

    static void PurgeQueues(DbConnectionStringBuilder connectionStringBuilder, string[] queues)
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

        using (var connection = connectionFactory.CreateConnection("Test Queue Purger"))
        using (var channel = connection.CreateModel())
        {
            foreach (var queue in queues)
            {
                try
                {
                    channel.QueuePurge(queue);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Unable to clear queue {0}: {1}", queue, ex);
                }
            }
        }
    }

    string[] queuesToCleanUp;
    DbConnectionStringBuilder connectionStringBuilder;
}