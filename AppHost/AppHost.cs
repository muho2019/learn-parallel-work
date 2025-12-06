var builder = DistributedApplication.CreateBuilder(args);

var sqlServer = builder.AddSqlServer("db")
    .WithHostPort(55906)
    .AddDatabase("appdata");

builder.AddProject<Projects.Api>("origin")
    .WithReference(sqlServer)
    .WaitFor(sqlServer);

builder.AddProject<Projects.ServerApi>("destination");

builder.Build().Run();
