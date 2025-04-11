var builder = DistributedApplication.CreateBuilder(args);

var apiService = builder.AddProject<Projects.Bingo_ApiService>("apiservice");

builder.AddProject<Projects.Bingo_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithReference(apiService)
    .WaitFor(apiService);

builder.Build().Run();
