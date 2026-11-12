using E2e.Bdd.Support;
using Reqnroll;

namespace E2e.Bdd.StepDefinitions;

[Binding]
public sealed class CaminhoFelizSteps(JsonHttp http, SagaContext ctx)
{
    private static readonly TimeSpan PollTimeout = TimeSpan.FromSeconds(90);

    [Given("que o compose e2e está saudável")]
    public async Task DadoComposeSaudavel()
    {
        await http.GetAsync($"{ServiceUrls.Administrativo}/administrativo/health");
        await http.GetAsync($"{ServiceUrls.Autenticacao}/autenticacao/health");
        await http.GetAsync($"{ServiceUrls.Estoque}/estoque/health");
        await http.GetAsync($"{ServiceUrls.OrdemServico}/ordem-servico/health");
        await http.GetAsync($"{ServiceUrls.Orcamento}/orcamento/health");
        await http.ResetWireMockAsync();
    }

    [Given(@"que faço login staff com ""(.*)"" e ""(.*)""")]
    public async Task DadoLoginStaff(string login, string senha)
    {
        http.SetBearer(null);
        var body = await http.PostAsync(
            $"{ServiceUrls.Autenticacao}/autenticacao/login",
            new { login, senha });
        ctx.StaffToken = body.GetProperty("token").GetString()
            ?? throw new InvalidOperationException("Login sem token.");
        http.SetBearer(ctx.StaffToken);
    }

    [Given("que preparo catálogo, estoque e veículo para a saga")]
    public async Task DadoPreparoCatalogo()
    {
        ctx.Documento = DocumentoFactory.NovoCpf();
        var cliente = await http.PostAsync(
            $"{ServiceUrls.Administrativo}/administrativo/clientes",
            new
            {
                nome = $"Cliente E2E {ctx.Suffix}",
                email = $"cliente.{ctx.Suffix}@oficina.dev",
                tipoDocumento = "Cpf",
                documento = ctx.Documento
            });
        ctx.ClienteId = cliente.GetProperty("id").GetInt32();

        var veiculo = await http.PostAsync(
            $"{ServiceUrls.Administrativo}/administrativo/veiculos",
            new
            {
                placa = DocumentoFactory.NovaPlaca(ctx.Suffix),
                idCliente = ctx.ClienteId,
                modelo = "Onix",
                marca = "Chevrolet"
            });
        ctx.VeiculoId = veiculo.GetProperty("id").GetInt32();

        var codigoServico = CodigoAaa123(ctx.Suffix);
        var servico = await http.PostAsync(
            $"{ServiceUrls.Administrativo}/administrativo/servicos",
            new
            {
                codigo = codigoServico,
                nome = "Troca de oleo E2E",
                precoPadrao = 120.0m,
                ativo = true
            });
        ctx.ServicoCatalogoId = servico.GetProperty("id").GetInt32();

        var item = await http.PostAsync(
            $"{ServiceUrls.Estoque}/estoque/itens",
            new
            {
                codigo = CodigoAaa123(ctx.Suffix + "1"),
                tipo = "Peca",
                nome = "Filtro E2E",
                unidadeMedida = "Unidade",
                precoVenda = 45.9m,
                saldo = 10m,
                saldoReservado = 0m
            });
        ctx.ItemEstoqueId = item.GetProperty("id").GetInt32();
    }

    private static string CodigoAaa123(string seed)
    {
        var letters = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        var a = letters[seed[0] % 26];
        var b = letters[seed[1 % seed.Length] % 26];
        var c = letters[seed[2 % seed.Length] % 26];
        var n = Math.Abs(seed.GetHashCode()) % 1000;
        return $"{a}{b}{c}-{n:D3}";
    }

    [When("crio a ordem de serviço com o veículo e o serviço")]
    public async Task QuandoCrioOs()
    {
        var created = await http.PostAsync(
            $"{ServiceUrls.OrdemServico}/ordem-servico",
            new
            {
                idVeiculo = ctx.VeiculoId,
                servicos = new[]
                {
                    new
                    {
                        idServico = ctx.ServicoCatalogoId,
                        valorCobrado = 150m,
                        itensNecessarios = new[]
                        {
                            new { idItemEstoque = ctx.ItemEstoqueId, quantidade = 1m }
                        }
                    }
                }
            });
        ctx.OrdemServicoId = created.GetProperty("id").GetGuid();
        ctx.ServicoLinhaId = created.GetProperty("servicos")[0].GetProperty("id").GetInt32();
    }

    [When("finalizo o diagnóstico")]
    public async Task QuandoFinalizoDiagnostico()
    {
        await http.PostAsync(
            $"{ServiceUrls.OrdemServico}/ordem-servico/{ctx.OrdemServicoId}/finalizar-diagnostico");
    }

    [When("aprovo a ordem de serviço como staff")]
    public async Task QuandoAprovoStaff()
    {
        await http.PostAsync(
            $"{ServiceUrls.OrdemServico}/ordem-servico/{ctx.OrdemServicoId}/aprovar");
    }

    [When("confirmo a execução dos serviços")]
    public async Task QuandoConfirmoExecucao()
    {
        var agora = DateTime.UtcNow;
        await http.PostAsync(
            $"{ServiceUrls.OrdemServico}/ordem-servico/{ctx.OrdemServicoId}/confirmar-execucao",
            new
            {
                servicosExecutados = new[]
                {
                    new
                    {
                        idServico = ctx.ServicoLinhaId,
                        iniciadoEm = agora.AddHours(-2),
                        finalizadoEm = agora.AddMinutes(-10)
                    }
                }
            });
    }

    [When("envio o webhook de pagamento aprovado do Mercado Pago")]
    public async Task QuandoEnvioWebhookAprovado()
    {
        ctx.CobrancaId = await http.WaitForWireMockOrderIdAsync(PollTimeout);
        await http.StubGetOrderStatusAsync(ctx.CobrancaId, "processed", "accredited");
        await EnviarWebhookAsync(ctx.CobrancaId, "order.processed", "req-e2e-happy");
    }

    [When("envio o webhook de pagamento recusado do Mercado Pago")]
    public async Task QuandoEnvioWebhookRecusado()
    {
        ctx.CobrancaIdAnterior = await http.WaitForWireMockOrderIdAsync(PollTimeout);
        ctx.CobrancaId = ctx.CobrancaIdAnterior;
        await http.StubGetOrderStatusAsync(ctx.CobrancaIdAnterior, "rejected", "rejected_by_bank");
        await EnviarWebhookAsync(ctx.CobrancaIdAnterior, "order.failed", "req-e2e-refused");

        var ids = await http.WaitForWireMockOrderIdsAsync(2, PollTimeout);
        var nova = ids.FirstOrDefault(id =>
            !string.Equals(id, ctx.CobrancaIdAnterior, StringComparison.Ordinal));
        if (nova is null)
        {
            throw new TimeoutException("Não surgiu segunda cobrança Pix após recusa.");
        }

        ctx.CobrancaId = nova;
    }

    [When("descarto a ordem de serviço")]
    public async Task QuandoDescarto()
    {
        await http.PostAsync(
            $"{ServiceUrls.OrdemServico}/ordem-servico/{ctx.OrdemServicoId}/descartar");
    }

    [Then(@"a ordem de serviço fica com status ""(.*)""")]
    public async Task EntaoStatus(string statusEsperado)
    {
        var deadline = DateTime.UtcNow + PollTimeout;
        string? atual = null;
        while (DateTime.UtcNow < deadline)
        {
            var os = await http.GetAsync(
                $"{ServiceUrls.OrdemServico}/ordem-servico/{ctx.OrdemServicoId}");
            atual = os.GetProperty("status").GetString();
            if (string.Equals(atual, statusEsperado, StringComparison.Ordinal))
            {
                if (os.TryGetProperty("servicos", out var servicos)
                    && servicos.GetArrayLength() > 0)
                {
                    ctx.ServicoLinhaId = servicos[0].GetProperty("id").GetInt32();
                }

                return;
            }

            await Task.Delay(750);
        }

        throw new TimeoutException(
            $"OS {ctx.OrdemServicoId} não chegou a '{statusEsperado}' (último: '{atual}').");
    }

    [Then("uma nova cobrança Pix foi criada no WireMock")]
    public void EntaoNovaCobranca()
    {
        if (string.IsNullOrWhiteSpace(ctx.CobrancaIdAnterior)
            || string.IsNullOrWhiteSpace(ctx.CobrancaId)
            || string.Equals(ctx.CobrancaId, ctx.CobrancaIdAnterior, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"Esperava cobrancaId novo após recusa (anterior={ctx.CobrancaIdAnterior}, atual={ctx.CobrancaId}).");
        }
    }

    private async Task EnviarWebhookAsync(string orderId, string action, string requestId)
    {
        var signature = WebhookSigner.Sign(orderId, requestId, ServiceUrls.WebhookSecret);
        await http.SendAsync(
            HttpMethod.Post,
            $"{ServiceUrls.Orcamento}/orcamento/webhook/mercado-pago?data.id={Uri.EscapeDataString(orderId)}&type=order",
            new { action, data = new { id = orderId } },
            [
                new KeyValuePair<string, string>("x-request-id", requestId),
                new KeyValuePair<string, string>("x-signature", signature)
            ]);
    }
}
