# Documentación de AddChild

Agrega los siguientes paquetes de Nuget en tu proyecto **AddChild** y después ejecútalo.

```bash
dotnet add package Azure.Messaging.ServiceBus
dotnet add package Microsoft.Extensions.Configuration
dotnet add package Microsoft.Extensions.Configuration.FileExtensions
dotnet add package Microsoft.Extensions.Configuration.Json
dotnet add package Microsoft.Extensions.Hosting
dotnet add package Microsoft.EntityFrameworkCore
dotnet add package Microsoft.EntityFrameworkCore.SqlServer

dotnet run
```

Verifica que puedes acceder a tu interfaz de swagger y además al método que **weatherforecast**.

## Código del proyecto

Agrega lo siguiente en tu **appsettings.json**, agrega tu cadena de conexión.

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "ServiceBus": {
    "ConnectionString": "Endpoint=sb://<your-service-bus-namespace>.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;Shared",
    "TopicName": "childrentopic"
  },
  "ConnectionStrings": {
    "DefaultConnection": "Server=tcp"
  },
  "AllowedHosts": "*"
}
```

Reemplaza el contenido de **Program.cs**

```csharp
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;
using AddChild;
using AddChild.Data;

IServiceCollection serviceDescriptors = new ServiceCollection();

Host.CreateDefaultBuilder(args)
   .ConfigureHostConfiguration(configHost =>
   {
       configHost.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true).Build();
   })
   .ConfigureServices((hostContext, services) =>
   {
       IConfiguration configuration = hostContext.Configuration;
       services.AddOptions();
       services.AddHostedService<Worker>();
       services.AddDbContext<DataContext>(options =>
           options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));
   }).Build().Run();
```

Crea un nuevo archivo llamado **Worker.cs** y agrega:

```csharp
using Azure.Messaging.ServiceBus;
using AddChild.Data;
using AddChild.Models;
using System.Text.Json;

namespace AddChild
{
    internal class Worker : BackgroundService
    {
        private readonly string _connectionString;
        private readonly string _topicName;
        private readonly string _subscriptionName;
        private readonly ServiceBusClient _client;
        private readonly ServiceBusProcessor _processor;

        private readonly IServiceProvider _serviceProvider;

        public Worker(IConfiguration configuration, IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            _connectionString = configuration["ServiceBus:ConnectionString"];
            _topicName = configuration["ServiceBus:TopicName"];
            _subscriptionName = "S1";
            _client = new ServiceBusClient(_connectionString);

            _processor = _client.CreateProcessor(_topicName, _subscriptionName, new ServiceBusProcessorOptions());
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
            Console.WriteLine($"Received message: {body}");
            await args.CompleteMessageAsync(args.Message);
            await SaveToDatabaseAsync(body);
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

        private async Task SaveToDatabaseAsync(string body)
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<DataContext>();

                var firstPart = body.Split('"');
                var parts = firstPart[1].Split(", ");
                var child = new Child
                {
                    Name = parts.FirstOrDefault(p => p.StartsWith("Name:"))?.Split(": ")[1].Trim('"'),
                    Lastname = parts.FirstOrDefault(p => p.StartsWith("Lastname:"))?.Split(": ")[1].Trim('"'),
                    BirthYear = int.Parse(parts.FirstOrDefault(p => p.StartsWith("Birthyear:"))?.Split(": ")[1].Trim('"')),
                    ImageUrl = string.Format("{0}{1}.jpg", (parts.FirstOrDefault(p => p.StartsWith("Name:"))?.Split(": ")[1].Trim('"')).ToLower(), (parts.FirstOrDefault(p => p.StartsWith("Lastname:"))?.Split(": ")[1].Trim('"')).ToLower())
                };

                Console.WriteLine($"Sending Adult to database: {child.Name} {child.Lastname} {child.BirthYear} {child.ImageUrl}");
                dbContext.Children.Add(child);
                await dbContext.SaveChangesAsync();
            }
        }
    }
}
```

Crea una carpeta llamada **Data** y ahí un archivo llamado **DataContext.cs**.

```csharp
using Microsoft.EntityFrameworkCore;
using AddChild.Models;

namespace AddChild.Data
{
    public class DataContext : DbContext
    {
        public DataContext(DbContextOptions<DataContext> options) : base(options) { }

        public DbSet<Child> Children { get; set; }
    }
}
```

Crea una carpeta llamada **Models** y ahí un archivo llamado **Child.cs**.

```csharp
namespace AddChild.Models
{
    public class Child
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Lastname { get; set; }
        public int BirthYear { get; set; }
        public string ImageUrl { get; set; }
    }
}
```
