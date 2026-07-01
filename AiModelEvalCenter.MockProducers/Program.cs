using AiModelEvalCenter.MockProducers.Producers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHostedService<MockTelemetryProducer>();

var host = builder.Build();
host.Run();
