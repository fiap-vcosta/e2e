namespace E2e.Bdd.Support;

public sealed class SagaContext
{
    public string StaffToken { get; set; } = string.Empty;
    public string Documento { get; set; } = string.Empty;
    public int ClienteId { get; set; }
    public int VeiculoId { get; set; }
    public int ServicoCatalogoId { get; set; }
    public int ItemEstoqueId { get; set; }
    public Guid OrdemServicoId { get; set; }
    public int ServicoLinhaId { get; set; }
    public string CobrancaId { get; set; } = string.Empty;
    public string CobrancaIdAnterior { get; set; } = string.Empty;
    public string Suffix { get; } = Guid.NewGuid().ToString("N")[..8];
}
