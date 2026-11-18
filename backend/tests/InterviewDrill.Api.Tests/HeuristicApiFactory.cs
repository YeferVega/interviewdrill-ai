using InterviewDrill.Api;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace InterviewDrill.Api.Tests;

/// <summary>
/// Factory determinista para los endpoint tests: fuerza entorno "Testing" (sin user-secrets
/// ni appsettings.Development.json que cargan llaves de LLM reales) y fija
/// LLM:Provider=Heuristic para que los agentes usen el modo heurístico en las pruebas.
/// </summary>
public sealed class HeuristicApiFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // 1) "Testing" evita appsettings.Development.json y user-secrets (que tendrían la API key real).
        builder.UseEnvironment("Testing");

        // 2) Forzamos provider determinista. Se aplica DESPUÉS de las fuentes de configuración
        //    (archivos/env/user-secrets) así que la override gana aunque NVIDIA_API_KEY esté definida.
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["LLM:Provider"] = "Heuristic"
            });
        });
    }
}
