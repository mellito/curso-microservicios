using Azure.Messaging.ServiceBus;

namespace PickAge
{
    internal class Worker : BackgroundService
    {
        private readonly string _connectionString;
        private readonly string _queueName;
        private readonly ServiceBusClient _client;
        private readonly ServiceBusProcessor _processor;

        public Worker(IConfiguration configuration)
        {
            _connectionString = configuration["ServiceBus:ConnectionString"];
            _queueName = configuration["ServiceBus:QueueName"];
            _client = new ServiceBusClient(_connectionString);
            _processor = _client.CreateProcessor(_queueName, new ServiceBusProcessorOptions());
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _processor.ProcessMessageAsync += MessageHandler;
            _processor.ProcessErrorAsync += ErrorHandler;

            await _processor.StartProcessingAsync(stoppingToken);
            await Task.Delay(Timeout.Infinite, stoppingToken);
            await _processor.StopProcessingAsync(stoppingToken);
        }

        private async Task MessageHandler(ProcessMessageEventArgs args)
        {
            string body = args.Message.Body.ToString();
            await CreateAndSendTopic(body);
            Console.WriteLine($"Received message: {body}");

            await args.CompleteMessageAsync(args.Message);
        }

        private Task ErrorHandler(ProcessErrorEventArgs args)
        {
            Console.WriteLine($"Error occurred: {args.Exception.Message}");
            return Task.CompletedTask;
        }

        public override async Task StopAsync(CancellationToken stoppingToken)
        {
            await _processor.CloseAsync();
            await _client.DisposeAsync();
            await base.StopAsync(stoppingToken);
        }

        private async Task CreateAndSendTopic(string body)
        {
            var parts = body.Split(", ");
            var birthyearPart = parts.FirstOrDefault(p => p.StartsWith("Birthyear:"));
            int birthyear = Int32.Parse(birthyearPart.Split(": ")[1].Trim('"'));
            int currentYear = DateTime.Now.Year;

            if (birthyear < currentYear - 18)
            {
                Console.WriteLine($"Adult: {body}");
                var topicName = "adultstopic";
                var topicClient = _client.CreateSender(topicName);
                var message = new ServiceBusMessage(body);
                Console.WriteLine($"Sending message to topic: {topicName}");
                await topicClient.SendMessageAsync(message);
            }
            else
            {
                Console.WriteLine($"Child: {body}");
                var topicName = "childrentopic";
                var topicClient = _client.CreateSender(topicName);
                var message = new ServiceBusMessage(body);
                Console.WriteLine($"Sending message to topic: {topicName}");
                await topicClient.SendMessageAsync(message);
            }
        }

    }
}