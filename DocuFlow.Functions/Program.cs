using DocuFlow.Application;
using DocuFlow.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Azure.SignalR.Management;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices((context, services) =>
    {
        services.AddApplication();
        services.AddInfrastructure(context.Configuration);

        // SignalR Service Management for Serverless Updates
        services.AddSingleton<ServiceHubContext>(sp =>
        {
            var connectionString = context.Configuration["SignalRConnection"];
            var serviceManager = new ServiceManagerBuilder()
                .WithOptions(o => o.ConnectionString = connectionString)
                .BuildServiceManager();
            
            return serviceManager.CreateHubContextAsync("documentUpdates", default).GetAwaiter().GetResult();
        });
    })
    .Build();

host.Run();
