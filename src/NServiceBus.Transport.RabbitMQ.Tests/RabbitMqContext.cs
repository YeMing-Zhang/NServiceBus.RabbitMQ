using System.Data.Common;
using System.Threading;
using NServiceBus.Unicast.Messages;

namespace NServiceBus.Transport.RabbitMQ.Tests
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using NUnit.Framework;
    using Support;

    class RabbitMqContext
    {
        public virtual int MaximumConcurrency => 1;

        [SetUp]
        public async Task SetUp()
        {
            receivedMessages = new BlockingCollection<IncomingMessage>();

            var connectionString = Environment.GetEnvironmentVariable("RabbitMQTransport_ConnectionString");

            if (string.IsNullOrEmpty(connectionString))
            {
                connectionString = "host=localhost";
                //throw new Exception("The 'RabbitMQTransport_ConnectionString' environment variable is not set.");
            }

            var connectionStringBuilder = new DbConnectionStringBuilder { ConnectionString = connectionString };

            //TODO: Parse any settings in the connection string 
            var transport = new RabbitMQTransport { Host = (string)connectionStringBuilder["Host"] };

            connectionFactory = new ConnectionFactory(ReceiverQueue, transport.Host, transport.Port ?? 5672,
                transport.VHost, transport.UserName, transport.Password, null, false,
                false, transport.HeartbeatInterval, transport.NetworkRecoveryInterval);

            infra = await transport.Initialize(new HostSettings(ReceiverQueue, ReceiverQueue, new StartupDiagnosticEntries(),
                (msg, ex) => { }, true), new[]
            {
                new ReceiveSettings(ReceiverQueue, ReceiverQueue, true, true, "error") 
            }, AdditionalReceiverQueues.ToArray(), CancellationToken.None);

            messageDispatcher = infra.Dispatcher;
            messagePump = infra.GetReceiver(ReceiverQueue);
            subscriptionManager = messagePump.Subscriptions;

            await messagePump.Initialize(new PushRuntimeSettings(MaximumConcurrency),
                messageContext =>
                {
                    receivedMessages.Add(new IncomingMessage(messageContext.MessageId, messageContext.Headers,
                        messageContext.Body));
                    return Task.CompletedTask;
                }, ErrorContext => Task.FromResult(ErrorHandleResult.Handled), new MessageMetadata[0],
                CancellationToken.None
            );

            await messagePump.StartReceive(CancellationToken.None);
        }

        [TearDown]
        public async Task TearDown()
        {
            if (messagePump != null)
            {
                await messagePump.StopReceive(CancellationToken.None);
            }

            if (infra != null)
            {
                await infra.DisposeAsync();
            }
        }

        protected bool TryWaitForMessageReceipt() => TryReceiveMessage(out var _, incomingMessageTimeout);

        protected IncomingMessage ReceiveMessage()
        {
            if (!TryReceiveMessage(out var message, incomingMessageTimeout))
            {
                throw new TimeoutException($"The message did not arrive within {incomingMessageTimeout.TotalSeconds} seconds.");
            }

            return message;
        }

        bool TryReceiveMessage(out IncomingMessage message, TimeSpan timeout) =>
            receivedMessages.TryTake(out message, timeout);

        protected virtual IEnumerable<string> AdditionalReceiverQueues => Enumerable.Empty<string>();

        protected const string ReceiverQueue = "testreceiver";
        protected const string ErrorQueue = "error";
        protected ConnectionFactory connectionFactory;
        protected IMessageDispatcher messageDispatcher;
        protected IMessageReceiver messagePump;
        protected ISubscriptionManager subscriptionManager;

        BlockingCollection<IncomingMessage> receivedMessages;

        static readonly TimeSpan incomingMessageTimeout = TimeSpan.FromSeconds(1);
        TransportInfrastructure infra;
    }
}
