using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using WisdomPetMedicine.Rescue.Domain.Repositories;

namespace WisdomPetMedicine.Rescue.Api.IntegrationEvents
{
    public class PetFlaggedForAdoptionIntegrationEventHandler : BackgroundService
    {
        private readonly ILogger<PetFlaggedForAdoptionIntegrationEventHandler> _logger;
        private readonly ServiceBusClient _client;
        private readonly ServiceBusProcessor _proccessor;

        public PetFlaggedForAdoptionIntegrationEventHandler(IConfiguration configuration, ILogger<PetFlaggedForAdoptionIntegrationEventHandler> logger,
                                                            IRescueRepository rescueRepository)
        {
            _client = new ServiceBusClient(configuration["ServiceBus:ConnectionString"]);

            _proccessor = _client.CreateProcessor(configuration["ServiceBus:Adoption:TopicName"],
                                                    configuration["ServiceBus:Adoption:SubcriptionName"]);

            _logger = logger;
            _proccessor.ProcessMessageAsync +=Process_ProcessMessageAsync;
            _proccessor.ProcessErrorAsync += Process_ProcessErrorAsync;

        }

        private async Task Process_ProcessMessageAsync(ProcessMessageEventArgs args)
        {
            var body = args.Message.Body.ToString();
            var theEvent = JsonConvert.DeserializeObject<PetFlaggedForAdoptionIntegrationEvent>(body);
            
            await args.CompleteMessageAsync(args.Message);
        }
        private Task Process_ProcessErrorAsync(ProcessErrorEventArgs args)
        {
            _logger.LogError(args.Exception.ToString());

            return Task.CompletedTask;
        }

        protected async override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await _proccessor.StartProcessingAsync();
        }
        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            await _proccessor.StopProcessingAsync(cancellationToken);
        }
    }
}