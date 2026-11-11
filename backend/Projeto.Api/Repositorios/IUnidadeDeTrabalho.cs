// Limite de escrita da aplicação: um SaveChanges por operação, sobre o mesmo contexto
// compartilhado com os repositórios. Repositórios apenas preparam estado.
public interface IUnidadeDeTrabalho
{
    void Salvar();
}
