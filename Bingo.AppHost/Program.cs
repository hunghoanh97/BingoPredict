var builder = DistributedApplication.CreateBuilder(args);

// PostgreSQL (chạy trong Docker) + database "BingoDb" + pgAdmin.
var postgres = builder.AddPostgres("postgres")
    .WithDataVolume()
    .WithPgAdmin();

var bingoDb = postgres.AddDatabase("BingoDb");

builder.AddProject<Projects.Bingo_ApiService>("apiservice")
    .WithReference(bingoDb)
    .WaitFor(bingoDb);

builder.Build().Run();
