// Limite de escrita da aplicação: um SaveChanges por operação, sobre o mesmo contexto
// compartilhado com os repositórios. Repositórios apenas preparam estado. Quando a
// operação insere avaliações, a mesma transação recalcula a média derivada dos avaliados.
public interface IUnidadeDeTrabalho
{
    void Salvar();
}
