namespace E2e.Bdd.Support;

internal static class ServiceUrls
{
    public static string Administrativo { get; } = Env("E2E_ADMINISTRATIVO_URL", "http://localhost:8080");
    public static string Autenticacao { get; } = Env("E2E_AUTENTICACAO_URL", "http://localhost:8081");
    public static string Estoque { get; } = Env("E2E_ESTOQUE_URL", "http://localhost:8082");
    public static string OrdemServico { get; } = Env("E2E_ORDEM_SERVICO_URL", "http://localhost:8083");
    public static string Orcamento { get; } = Env("E2E_ORCAMENTO_URL", "http://localhost:8084");
    public static string WireMock { get; } = Env("E2E_WIREMOCK_URL", "http://localhost:8089");

    public static string WebhookSecret { get; } =
        Env("MercadoPago__WebhookSecret", "e2e-webhook-secret");

    private static string Env(string name, string fallback) =>
        Environment.GetEnvironmentVariable(name) is { Length: > 0 } value ? value : fallback;
}
