var builder = DistributedApplication.CreateBuilder(args);

var sqlServer = builder.AddSqlServer("db").AddDatabase("appdata");

builder.AddProject<Projects.Api>("origin")
    .WithReference(sqlServer)
    .WaitFor(sqlServer);

builder.AddProject<Projects.ServerApi>("destination");

builder.Build().Run();
