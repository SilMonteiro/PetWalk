// Relógio controlável pelos testes de integração: a API hospedada pela PetWalkApiFactory
// deixa de depender do relógio real, então as datas de solicitação não expiram com o tempo.
public sealed class RelogioDeTeste : TimeProvider
{
    public RelogioDeTeste(DateTimeOffset agora) => Agora = agora;

    // Alterável para cenários que precisem avançar o tempo durante o teste.
    public DateTimeOffset Agora { get; set; }

    public override DateTimeOffset GetUtcNow() => Agora;
}
