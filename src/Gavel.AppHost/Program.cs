using Aspire.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

// PostgreSQL Resource ("gaveldb")
var postgres = builder.AddPostgres("postgres")
    .WithImage("postgres", "17")
    .WithDataVolume();

var gavelDb = postgres.AddDatabase("gaveldb");

// Keycloak Resource for OIDC
var keycloak = builder.AddKeycloak("keycloak")
    .WithDataVolume();

// Gavel.Api Orchestration
builder.AddProject<Projects.Gavel_Api>("gavel-api")
    .WithReference(gavelDb)
    .WithReference(keycloak)
    .WaitFor(gavelDb)
    .WaitFor(keycloak)
    .WithHttpHealthCheck("/health");

builder.Build().Run();
