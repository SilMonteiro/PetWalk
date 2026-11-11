using Microsoft.EntityFrameworkCore;

// Repositório EF do agregado Usuario (os pets vivem dentro do usuário).
// Não chama SaveChanges: a persistência é responsabilidade da IUnidadeDeTrabalho.
// Sem AsNoTracking: o domínio altera as entidades carregadas e essas mudanças
// precisam ser detectadas e gravadas pelo mesmo contexto.
public class UsuarioRepositorio : IUsuarioRepositorio
{
    private readonly PetWalkDbContext _contexto;

    public UsuarioRepositorio(PetWalkDbContext contexto)
    {
        _contexto = contexto ?? throw new ArgumentNullException(nameof(contexto));
    }

    // O Id nasce no banco: é gerado pelo SQLite quando a Unidade de Trabalho salvar.
    public Usuario Adicionar(Usuario usuario)
    {
        _contexto.Usuarios.Add(usuario);

        return usuario;
    }

    // Os endpoints acessam usuario.Pets, então a coleção precisa vir carregada.
    public Usuario? ObterPorId(int id) =>
        _contexto.Usuarios.Include(u => u.Pets).FirstOrDefault(u => u.Id == id);

    // REG-010: a coluna Email usa collation NOCASE no banco, então a igualdade já ignora
    // maiúsculas/minúsculas. Um ToLower() aqui descartaria o índice único da coluna.
    public Usuario? ObterPorEmail(string email) =>
        _contexto.Usuarios.FirstOrDefault(u => u.Email == email);

    // EhPasseador é propriedade calculada e não é mapeada: o filtro é escrito com os
    // valores do enum, que chegam ao banco como texto pela conversão já configurada.
    public IReadOnlyList<Usuario> ListarPasseadoresAtivos() =>
        _contexto.Usuarios
            .Where(u => (u.TipoPerfil == TipoPerfil.PASSEADOR || u.TipoPerfil == TipoPerfil.AMBOS)
                        && u.Status == StatusUsuario.ATIVO)
            .ToList();

    // Inclui o tutor porque Pet.Tutor é obrigatório no domínio: devolver um pet com
    // Tutor nulo violaria o invariante da entidade.
    public Pet? ObterPet(int petId) =>
        _contexto.Pets.Include(p => p.Tutor).FirstOrDefault(p => p.Id == petId);

    public Pet RegistrarPet(Usuario tutor, Pet pet)
    {
        // REG-002 e REG-005 continuam no agregado.
        tutor.AdicionarPet(pet);

        // Marca somente o pet como novo. DbContext.Add percorreria o grafo e marcaria
        // o tutor como Added caso ele não estivesse rastreado neste contexto.
        _contexto.Entry(pet).State = EntityState.Added;

        return pet;
    }
}
