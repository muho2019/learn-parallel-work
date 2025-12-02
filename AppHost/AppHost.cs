var builder = DistributedApplication.CreateBuilder(args);

var sqlServer = builder.AddSqlServer("db").AddDatabase("appdata");

builder.AddProject<Projects.Api>("api")
    .WithReference(sqlServer)
    .WaitFor(sqlServer);

builder.Build().Run();
