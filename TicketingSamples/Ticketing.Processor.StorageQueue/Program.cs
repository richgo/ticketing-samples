using System;

namespace Ticketing.Processor.StorageQueue
{
    using Microsoft.WindowsAzure.Storage;
    using Microsoft.WindowsAzure.Storage.Queue;
    using System;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    class Program
    {
        const string requestQueueName = "request";
        const string responseQueueName = "response";
        const string AzureStorageQueueConnectionString = "DefaultEndpointsProtocol=https;AccountName=admscenariostorage;AccountKey=I4WFr7qJpO3kCHWF7/txZ4DNkqbZtEReQGTHFvft3kBPeIIUEsBiBarBD9Y2V82LeNY7WVM7c/w+JyPlLUZJSg==;EndpointSuffix=core.windows.net";

        public async Task SendResponseStorageQueue(string message)
        {
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(AzureStorageQueueConnectionString);

            CloudQueueClient queueClient = storageAccount.CreateCloudQueueClient();

            CloudQueue cloudQueue = queueClient.GetQueueReference(responseQueueName);

            await cloudQueue.CreateIfNotExistsAsync();

            await cloudQueue.AddMessageAsync(new CloudQueueMessage(message));

            lock (Console.Out)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("Message sent");
                Console.ResetColor();
            }
        }

        public async Task<string> GetRequestStorageQueue()
        {
            string response = string.Empty;

            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(AzureStorageQueueConnectionString);
            
            CloudQueueClient queueClient = storageAccount.CreateCloudQueueClient();

            CloudQueue cloudQueue = queueClient.GetQueueReference(requestQueueName);

            await cloudQueue.CreateIfNotExistsAsync();

            CloudQueueMessage message = await cloudQueue.GetMessageAsync();

            if (message != null)
            {
                response = message.AsString;


                lock (Console.Out)
                {
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.WriteLine(
                        "\t\t\t\tMessage received:  \n\t\t\t\t\t\tId = {0}" +
                        "\n\t\t\t\t\t\tContent: {1}",
                        message.Id,
                        response);
                    Console.ResetColor();
                }

                await cloudQueue.DeleteMessageAsync(message);
            }

            return response;
        }

        public async Task Run()
        {
            Console.WriteLine("This will loop forever");

            // This would be far better run as a Web Job
            // https://github.com/Azure/azure-webjobs-sdk/wiki/Queues

            var runContinuously = true;
            while (runContinuously)
            {
                string message = await GetRequestStorageQueue();
                if(!string.IsNullOrEmpty(message))
                {
                    var response = "Hi from Azure Storage Queue. ";
                    await SendResponseStorageQueue(response);
                }
                Thread.Sleep(500);
            };            
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
