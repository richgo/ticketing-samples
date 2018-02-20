using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging;
using Newtonsoft.Json;

namespace Ticketing.Processor
{

    public class Program
    {
        const string AzureServiceBusConnectionString = "Endpoint=sb://admscenario.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=A9q3PGLBh9yQ6NuykQSRTeQkla9CIlAJGqVLSRIaYwA=";
        const string requestQueueName = "request";
        const string responseQueueName = "response";

        public async Task Run()
        {
            Console.WriteLine("Press any key to exit the scenario");

            CancellationTokenSource cts = new CancellationTokenSource();
            this.InitializeReceiver(AzureServiceBusConnectionString, requestQueueName, cts.Token);

            await Task.WhenAll(Task.Run(() => Console.ReadKey()));

            cts.Cancel();
        }
        
        void InitializeReceiver(string connectionString, string queueName, CancellationToken ct)
        {
            try
            {
                var receiverFactory = MessagingFactory.CreateFromConnectionString(connectionString);

                ct.Register(() => receiverFactory.Close());

                var client = receiverFactory.CreateQueueClient(queueName, ReceiveMode.PeekLock);

                // This approach has not been ported to the new .NET Core implemenetation yet
                // https://github.com/Azure/azure-service-bus/issues/131
                // TODO: Follow up on this

                client.RegisterSessionHandler(
                    typeof(SessionHandler),
                    new SessionHandlerOptions
                    {
                        MessageWaitTimeout = TimeSpan.FromSeconds(1),
                        MaxConcurrentSessions = 1,
                        AutoComplete = false
                    });
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        class SessionHandler : IMessageSessionAsyncHandler
        {
            async Task SendMessagesAsync(string sessionId, string connectionString, string queueName, dynamic data)
            {
                var senderFactory = MessagingFactory.CreateFromConnectionString(connectionString);

                var sender = await senderFactory.CreateMessageSenderAsync(queueName);
                             
                var message = new BrokeredMessage(new MemoryStream(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(data))))
                {
                    SessionId = sessionId,
                    ContentType = "application/json",              
                };

                await sender.SendAsync(message);

                await sender.CloseAsync();

                lock (Console.Out)
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("Message sent: Session {0}, MessageId = {1}", message.SessionId, message.MessageId);
                    Console.ResetColor();
                }                
            }

            public async Task OnMessageAsync(MessageSession session, BrokeredMessage message)
            {               
                var body = message.GetBody<Stream>();

                dynamic data = JsonConvert.DeserializeObject(new StreamReader(body, true).ReadToEnd());

                lock (Console.Out)
                {
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.WriteLine(
                        "\t\t\t\tMessage received:  \n\t\t\t\t\t\tSessionId = {0}, \n\t\t\t\t\t\tMessageId = {1}, \n\t\t\t\t\t\tSequenceNumber = {2}," +
                        "\n\t\t\t\t\t\tContent: {3}",
                        message.SessionId,
                        message.MessageId,
                        message.SequenceNumber,
                        data);
                    Console.ResetColor();
                }
                await message.CompleteAsync();

                var response = "Hi from Azure Service Bus Queue. ";

                await this.SendMessagesAsync(message.SessionId, AzureServiceBusConnectionString, responseQueueName, response);
    
            }

            public async Task OnCloseSessionAsync(MessageSession session)
            {
                // nothing to do
            }

            public async Task OnSessionLostAsync(Exception exception)
            {
                // nothing to do
            }
        }


        public static int Main(string[] args)
        {
            try
            {
                var app = new Program();
                Task.WaitAll(app.Run());
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                return 1;
            }
            return 0;
        }
    }
}
