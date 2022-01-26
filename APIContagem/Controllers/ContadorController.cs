using Microsoft.AspNetCore.Mvc;
using APIContagem.Models;
using APIContagem.Logging;
using System.Diagnostics;
using StackExchange.Redis;
using APIContagem.Tracing;

namespace APIContagem.Controllers;

[ApiController]
[Route("[controller]")]
public class ContadorController : ControllerBase
{
    private readonly ILogger<ContadorController> _logger;
    private readonly IConfiguration _configuration;
    private readonly ConnectionMultiplexer _connectionRedis;
    private readonly ActivitySource _activitySource;

    public ContadorController(ILogger<ContadorController> logger,
        IConfiguration configuration,
        ConnectionMultiplexer connectionRedis)
    {
        _logger = logger;
        _configuration = configuration;
        _connectionRedis = connectionRedis;

        _activitySource = OpenTelemetryExtensions.CreateActivitySource();
        using var activity =
            _activitySource.StartActivity($"{nameof(ContadorController)} (Construtor)");
        activity?.SetTag("horario", $"{DateTime.Now:HH:mm:ss dd/MM/yyyy}");
    }

    [HttpGet]
    public ResultadoContador Get()
    {
        using var activity =
            _activitySource.StartActivity($"ObterValorContagem ({nameof(Get)})");

        var valorAtualContador =
            (int)_connectionRedis.GetDatabase().StringIncrement("APIContagem");;

        _logger.LogValorAtual(valorAtualContador);

        activity?.SetTag("valorContador", valorAtualContador);
        activity?.SetTag("producer", OpenTelemetryExtensions.Local);
        activity?.SetTag("kernel", OpenTelemetryExtensions.Kernel);
        activity?.SetTag("framework", OpenTelemetryExtensions.Framework);
        activity?.SetTag("mensagem", _configuration["MensagemVariavel"]);

        return new ()
        {
            ValorAtual = valorAtualContador,
            Producer = OpenTelemetryExtensions.Local,
            Kernel = OpenTelemetryExtensions.Kernel,
            Framework = OpenTelemetryExtensions.Framework,
            Mensagem = _configuration["MensagemVariavel"]
        };
    }
}