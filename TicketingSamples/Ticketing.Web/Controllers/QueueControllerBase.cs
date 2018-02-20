using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.ServiceBus;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;
using System.Text;
using System.Threading.Tasks;

namespace Ticketing.Web.Controllers
{
    public class QueueControllerBase : Controller
    {       
        private readonly string _requestQueueName = "request";
        private readonly string _responseQueueName = "response";

        private ConnectionStrings _connectionStrings;

        public QueueControllerBase(ConnectionStrings connectionStrings)
        {
            _connectionStrings = connectionStrings;
        }

        public async void SendRequestServiceBus(string message)
        {
            var queueClient = new QueueClient(_connectionStrings.ServiceBus, _requestQueueName);

            var serviceBusMessage = new Message(Encoding.UTF8.GetBytes(message));

            serviceBusMessage.SessionId = HttpContext.Session.Id;

            await queueClient.SendAsync(serviceBusMessage);

            await queueClient.CloseAsync();
        }

        public async Task<string> GetResponseServiceBus()
        {
            var sessionClient = new SessionClient(_connectionStrings.ServiceBus, _responseQueueName);

            IMessageSession session = await sessionClient.AcceptMessageSessionAsync(HttpContext.Session.Id);

            if (session != null)
            {
                Message message = await session.ReceiveAsync();

                if (message != null)
                {
                    await session.CompleteAsync(message.SystemProperties.LockToken);                  

                    return Encoding.UTF8.GetString(message.Body);
                }
                await session.CloseAsync();
            }
            return string.Empty;
        }

        public async void SendRequestStorageQueue(string message)
        {
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(_connectionStrings.StorageQueue);

            CloudQueueClient queueClient = storageAccount.CreateCloudQueueClient();

            CloudQueue cloudQueue = queueClient.GetQueueReference(_requestQueueName);

            await cloudQueue.CreateIfNotExistsAsync();

            await cloudQueue.AddMessageAsync(new CloudQueueMessage(message));
        }

        public async Task<string> GetResponseStorageQueue()
        {
            string response = string.Empty;

            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(_connectionStrings.StorageQueue);

            CloudQueueClient queueClient = storageAccount.CreateCloudQueueClient();

            CloudQueue cloudQueue = queueClient.GetQueueReference(_responseQueueName);

            await cloudQueue.CreateIfNotExistsAsync();

            CloudQueueMessage message = await cloudQueue.GetMessageAsync();

            if (message != null)
            {
                response = message.AsString;

                await cloudQueue.DeleteMessageAsync(message);
            }

            return response;
        }
    }
}
