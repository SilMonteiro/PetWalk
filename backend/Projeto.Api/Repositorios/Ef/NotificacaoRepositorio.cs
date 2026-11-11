using Microsoft.EntityFrameworkCore;

// Repositório EF das notificações. Não chama SaveChanges: a persistência é
// responsabilidade da IUnidadeDeTrabalho. Sem AsNoTracking: MarcarComoLida altera a
// entidade carregada e essa mudança precisa ser persistida pelo mesmo contexto.
public class NotificacaoRepositorio : INotificacaoRepositorio
{
    private readonly PetWalkDbContext _contexto;

    public NotificacaoRepositorio(PetWalkDbContext contexto)
    {
        _contexto = contexto ?? throw new ArgumentNullException(nameof(contexto));
    }

    // O Id nasce no banco: é gerado pelo SQLite quando a Unidade de Trabalho salvar.
    // Destinatário e passeio devem estar rastreados no mesmo contexto, como ficam ao ser
    // carregados pelos repositórios antes do envio.
    public Notificacao Adicionar(Notificacao notificacao)
    {
        _contexto.Notificacoes.Add(notificacao);

        return notificacao;
    }

    // MarcarComoLida confere notificacao.Destinatario.Id antes de alterar Lida. As duas
    // navegações são obrigatórias no domínio, então a entidade volta completa.
    public Notificacao? ObterPorId(int id) =>
        _contexto.Notificacoes
            .Include(n => n.Destinatario)
            .Include(n => n.Passeio)
            .FirstOrDefault(n => n.Id == id);

    // Filtro pelo destinatário no banco, usando o índice da FK. Lida NÃO é filtrada aqui:
    // o NotificacaoServico é quem aplica apenasNaoLidas, como na implementação em memória.
    // NotificacaoResponse lê Passeio.Id de cada item, daí o Include do passeio.
    // Ordenado por Id: o histórico sai na ordem de criação, sem depender do plano do SQLite.
    public IReadOnlyList<Notificacao> ListarPorDestinatario(int usuarioId) =>
        _contexto.Notificacoes
            .Include(n => n.Destinatario)
            .Include(n => n.Passeio)
            .Where(n => n.Destinatario.Id == usuarioId)
            .OrderBy(n => n.Id)
            .ToList();
}
