using Aspire.Hosting;

IDistributedApplicationBuilder builder = DistributedApplication.CreateBuilder(args);

// PostgreSQL Resource ("gaveldb")
IResourceBuilder<PostgresServerResource> postgres = builder.AddPostgres("postgres")
    .WithImage("postgres", "17")
    .WithDataVolume();

IResourceBuilder<PostgresDatabaseResource> db = postgres.AddDatabase("gaveldb");

// Keycloak Resource
IResourceBuilder<KeycloakResource> keycloak = builder.AddKeycloak("keycloak")
    .WithDataVolume();

// Gavel.Api Orchestration
builder.AddProject<Projects.Gavel_Api>("gavel-api")
    .WithReference(db)
    .WithReference(keycloak)
    .WaitFor(db)
    .WaitFor(keycloak)
    .WithHttpHealthCheck("/health");

builder.Build().Run();
