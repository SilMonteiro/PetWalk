# Plano de implementação — correções da avaliação

Base: avaliação do PetWalk (nota 3,53). Cada etapa indica **quais dimensões da régua ela melhora**, **quais arquivos mudam** e **como saber que terminou**.

As etapas estão em ordem de dependência: o backend vem antes do frontend porque as telas precisam de endpoints, e os endpoints precisam de um domínio que já se proteja.

## Convenções que valem para todas as etapas

- **Mensagens de erro:** usar exatamente o texto da coluna "Falha" das regras REG-001 a REG-011 da `proposta.md`. Os testes conferem a mensagem, não só o tipo da exceção.
- **Vocabulário:** classes, estados e perfis seguem a proposta (`Usuario`, `Pet`, `Passeio`, `Avaliacao`, `Notificacao`; estados e perfis em MAIÚSCULAS). Se o código mudar o modelo, a `proposta.md` é atualizada no mesmo PR.
- **Relógio:** operações que dependem da data atual recebem `agora` como parâmetro, em vez de ler `DateTime.Now` dentro do domínio. Isso torna a REG-008 testável.

## Peso real de cada dimensão na nota final

| Dimensão | Peso final | Nota atual | Etapas que atacam |
|---|---|---|---|
| I — CRUD funcional | **15%** | 2,0 | 8, 9 |
| C — Encapsulamento/invariantes | **14%** | 3,0 | 1, 2, 3 |
| D — Responsabilidades/coesão | **14%** | 4,0 | 2, 3, 5 |
| B — Estado/comportamento/construtor | 10,5% | 5,0 | 1, 3 |
| E — Colaboração entre objetos | 10,5% | 5,0 | 2, 3, 4 |
| J — Integração front↔back | 7,5% | 3,0 | 8, 9 |
| K — Organização do frontend | 7,5% | 6,0 | 9 |
| A — Modelagem do domínio | 7% | 5,0 | 2, 6 |
| F — Abstração | 7% | 2,0 | 5, 6 |
| G — Polimorfismo | 3,5% | 1,0 | 6 |
| H — Testes | 3,5% | 0,0 | 7 (e um pouco em cada etapa) |

Os maiores ganhos estão no CRUD do frontend (I) e no encapsulamento e nas responsabilidades do backend (C e D).

---

## Etapa 0 — Limpeza e estrutura base

**Status:** concluída (`dotnet build` sem erros e 3 testes passando). O `dotnet new sln` do .NET 10 gera `PetWalk.slnx`.

**Dimensões:** K, D (de leve)

1. Em `backend/Projeto.Api/Program.cs`, remover `summaries`, o endpoint `/weatherforecast` e o `record WeatherForecast`.
2. Criar uma solution e um projeto de testes:
   ```bash
   cd backend
   dotnet new sln -n PetWalk
   dotnet sln add Projeto.Api/Projeto.Api.csproj
   dotnet new xunit -n Projeto.Tests
   dotnet sln add Projeto.Tests/Projeto.Tests.csproj
   dotnet add Projeto.Tests reference Projeto.Api
   ```
3. Organizar `Projeto.Api` em pastas: `Dominio/`, `Repositorios/`, `Endpoints/`, `Contratos/` (DTOs de request/response).

**Pronto quando:** `dotnet build` e `dotnet test` rodam na pasta `backend/` sem erros.

---

## Etapa 1 — Proteger o estado de todas as entidades

**Dimensões:** C, B · **Itens da correção:** "Setters públicos generalizados" e "Coleções expostas como mutáveis"

Regra geral, aplicada a **todas** as classes de `Dominio/`:

- Toda propriedade passa a ser `{ get; private set; }`, ou só `{ get; }`.
- Os dados essenciais entram pelo **construtor**, que recusa valores inválidos. Não deve ser possível criar um objeto "vazio" com `new X { ... }`.
- As coleções ficam em um campo privado e são expostas como `IReadOnlyList<T>`.
- As mudanças de estado acontecem só por **métodos com nome de domínio**.

Exemplo para `Pet` (o `Porte` vira o enum `Porte { PEQUENO, MEDIO, GRANDE }` em `Enums.cs`, serializado como número, igual ao `TipoPerfil`):

```csharp
public class Pet
{
    private readonly List<string> _vacinas = new();

    public int Id { get; private set; }
    public string Nome { get; private set; }
    public string Especie { get; private set; }
    public string Raca { get; private set; }
    public Porte Porte { get; private set; }
    public int Idade { get; private set; }
    public string Observacoes { get; private set; }
    public Usuario Tutor { get; }
    public IReadOnlyList<string> Vacinas => _vacinas;

    public Pet(Usuario tutor, string nome, string especie, string raca, Porte porte, int idade, string observacoes)
    {
        Tutor = tutor ?? throw new ArgumentNullException(nameof(tutor));
        Atualizar(nome, especie, raca, porte, idade, observacoes);
    }

    public void Atualizar(string nome, string especie, string raca, Porte porte, int idade, string observacoes)
    {
        if (string.IsNullOrWhiteSpace(nome)) throw new ArgumentException("Nome do pet é obrigatório.");
        if (idade < 0 || idade > 30) throw new ArgumentException("Idade inválida.");
        // ...demais validações
        Nome = nome; Especie = especie; Raca = raca; Porte = porte; Idade = idade; Observacoes = observacoes;
    }

    public void RegistrarVacina(string vacina) { /* valida e adiciona em _vacinas */ }
    internal void DefinirId(int id) => Id = id; // usado só pelo repositório
}
```

Ajustes de modelagem do `Pet`, alinhando-o ao formulário do frontend e à proposta:
- Remover `Animal`, que duplica `Especie`.
- Remover `Avaliacao`, porque quem é avaliado é o passeador, não o pet.
- Remover `Castrado` e `Localizacao`, que a proposta não lista. O local do passeio é informado na solicitação (REG-007).
- Incluir `Observacoes`, que a tela já envia.
- Trocar `Porte` de texto livre para enum.
- Trocar `TutorId` pela referência `Usuario Tutor` (ver etapa 2).

**Pronto quando:** `grep -rn "{ get; set; }" backend/Projeto.Api/Dominio` não retorna nada.

---

## Etapa 2 — Unificar a identidade do usuário

**Dimensões:** A, D, E, C · **Itens da correção:** "Três modelos para o mesmo usuário" e "`Tutor.Pets` nunca é preenchida"

Decisão: **um único `Usuario` com `TipoPerfil`**, como já descrito na `proposta.md`. As classes `Tutor` e `Passeador` são **excluídas**, e o comportamento delas passa para `Usuario`, que verifica o perfil antes de agir. Aceitar, recusar, iniciar e finalizar ficam no `Passeio` (etapa 3), que é o dono do estado.

```csharp
public class Usuario
{
    private readonly List<Pet> _pets = new();
    private string _endereco;                       // REG-007: sem propriedade pública

    public int Id { get; private set; }
    public string Nome { get; private set; }
    public string Email { get; private set; }
    public string Telefone { get; private set; }
    public TipoPerfil TipoPerfil { get; private set; }
    public StatusUsuario Status { get; private set; }
    public string SenhaHash { get; private set; }   // ver etapa 5
    public decimal ValorHora { get; private set; }  // só faz sentido para passeador
    public TipoCobranca TipoCobranca { get; private set; } // ver etapa 6
    public double MediaAvaliacao { get; private set; }
    public IReadOnlyList<Pet> Pets => _pets;

    public bool EhTutor => TipoPerfil is TipoPerfil.TUTOR or TipoPerfil.AMBOS;
    public bool EhPasseador => TipoPerfil is TipoPerfil.PASSEADOR or TipoPerfil.AMBOS;

    public Pet AdicionarPet(string nome, ...)      // exige EhTutor; cria o Pet e adiciona em _pets
    public void RemoverPet(Pet pet)                 // exige que o pet seja deste tutor
    public Passeio SolicitarPasseio(Pet pet, Usuario passeador, DateTime dataHora, TimeSpan duracao, string local, DateTime agora)
        // REG-002 (EhTutor), REG-005 (_pets.Contains(pet)), REG-008 (dataHora > agora),
        // REG-009 (passeador.EhPasseador e Status ATIVO)
    public string ObterEnderecoPara(Usuario solicitante)  // REG-007: só devolve se solicitante == this
    public void Bloquear() / Desbloquear()
    internal void RecalcularMedia(IEnumerable<Avaliacao> avaliacoes)
}
```

Mensagens a usar (iguais às da proposta): "Usuário não possui perfil de tutor", "O pet informado não pertence ao tutor solicitante", "A data e hora do passeio devem ser futuras", "Usuário não possui perfil de passeador", "Usuário está bloqueado" e "Acesso não autorizado ao endereço do perfil".

Pontos importantes:
- `Cadastrar`, `ObterTodos`, `Login` e a lista `static` **saem** de `Usuario` (vão para o repositório na etapa 5).
- `AdicionarPet` é a única forma de um pet entrar em `Pets`. Isso corrige o problema de `SolicitarPasseio` recusar qualquer pet.
- Apagar `Dominio/Tutor.cs` e `Dominio/Passeador.cs`.

**Pronto quando:** existe uma só classe com `Nome/Email/Telefone/Endereco` e o fluxo "cadastrar usuário → adicionar pet → solicitar passeio" funciona sem setter público nenhum.

---

## Etapa 3 — Ciclo de vida do `Passeio` (objeto central)

**Dimensões:** B, C, D, E · **Itens da correção:** "O objeto central não tem ciclo de vida", "A regra vive no objeto errado" e "`Passeio` desmembra a colaboração em Ids"

1. `Passeio` guarda **referências** a objetos, não Ids: `Usuario Tutor`, `Usuario Passeador`, `Pet Pet`.
2. `Status` passa a ser `{ get; private set; }`.
3. Toda transição vira um método que confere quem está agindo e o estado atual:

```csharp
public class Passeio
{
    public int Id { get; private set; }
    public Usuario Tutor { get; }
    public Usuario Passeador { get; }
    public Pet Pet { get; }
    public DateTime DataHora { get; }
    public TimeSpan Duracao { get; }
    public string Local { get; }
    public decimal Valor { get; }                   // calculado pela política de preço (etapa 6)
    public StatusPasseio Status { get; private set; } = StatusPasseio.SOLICITADO;

    internal Passeio(Usuario tutor, Usuario passeador, Pet pet, DateTime dataHora, TimeSpan duracao, string local, IPoliticaDePreco politica)
    { /* valida e atribui; Valor = politica.Calcular(this); criado só via Usuario.SolicitarPasseio */ }

    public void Aceitar(Usuario quem)
    {
        ExigirPasseadorAtivo(quem);                 // REG-003
        ExigirStatus(StatusPasseio.SOLICITADO);
        Status = StatusPasseio.ACEITO;
    }

    public void Recusar(Usuario quem)   { ExigirPasseadorAtivo(quem); ExigirStatus(StatusPasseio.SOLICITADO);  Status = StatusPasseio.RECUSADO; }
    public void Iniciar(Usuario quem)   { ExigirPasseadorAtivo(quem); ExigirStatus(StatusPasseio.ACEITO);      Status = StatusPasseio.EM_ANDAMENTO; }
    public void Finalizar(Usuario quem) { ExigirPasseadorAtivo(quem); ExigirStatus(StatusPasseio.EM_ANDAMENTO); Status = StatusPasseio.FINALIZADO; }

    public void Cancelar(Usuario quem)
    {
        if (!Participou(quem)) throw new InvalidOperationException("Usuário não participou deste passeio.");
        if (Status is not (StatusPasseio.SOLICITADO or StatusPasseio.ACEITO))
            throw new InvalidOperationException("Passeio não pode ser cancelado no estado atual."); // REG-004
        Status = StatusPasseio.CANCELADO;
    }

    public bool Participou(Usuario u) => u == Tutor || u == Passeador;

    private void ExigirPasseadorAtivo(Usuario quem)
    {
        // "Passeio não pertence a este passeador" se quem != Passeador;
        // "Usuário não possui perfil de passeador" se !quem.EhPasseador;
        // "Usuário está bloqueado" se quem.Status == BLOQUEADO
    }

    private void ExigirStatus(StatusPasseio esperado)
    {
        // lança "Transição não permitida a partir do estado atual" se Status != esperado
    }
}
```

Por construção, o `ExigirStatus` bloqueia todas as transições proibidas (FINALIZADO, RECUSADO e CANCELADO são terminais; SOLICITADO não salta para FINALIZADO; EM_ANDAMENTO não vai para CANCELADO).

**Pronto quando:** nenhum código fora de `Passeio.cs` escreve `Status`, e a lógica de `Passeador.AceitarPasseio` foi absorvida por `Passeio.Aceitar`.

---

## Etapa 4 — Avaliação e Notificação como colaboradores reais

**Dimensões:** E, C, B

**`AvaliacaoPasseador` passa a se chamar `Avaliacao`** (nome da proposta):
- Construtor `Avaliacao(Passeio passeio, Usuario avaliador, int nota, string comentario)` que aplica:
  - **REG-001:** `passeio.Status == FINALIZADO` ("Passeio não finalizado") e nota entre 1 e 5 ("Nota deve estar entre 1 e 5");
  - **REG-006:** `passeio.Participou(avaliador)` ("Usuário não participou deste passeio").
- O avaliado é derivado do passeio (se o tutor avalia, o avaliado é o passeador, e vice-versa).
- "Já avaliado por este usuário" (segunda parte da REG-001): fica no `Passeio`, que guarda suas avaliações em uma `List<Avaliacao>` privada e expõe `Passeio.Avaliar(Usuario avaliador, int nota, string comentario)`. Assim a duplicidade é verificada por quem tem a informação.
- Depois de criada a avaliação, o avaliado recalcula a média (`RecalcularMedia`).

**`Notificacao`**:
- Construtor `Notificacao(Usuario destinatario, Passeio passeio, TipoNotificacao tipo)`, que gera a mensagem a partir do tipo.
- Setters privados; `MarcarComoLida()` continua sendo o único jeito de mudar `Lida`.
- Criar a pasta `Servicos/` com `NotificacaoServico`: `Enviar(Usuario destinatario, Passeio passeio, TipoNotificacao tipo)` (o `enviarNotificacao()` da proposta) e `ListarNaoLidas(int usuarioId)` (o `listarNaoLidas()`). Os endpoints chamam o serviço depois de cada transição do passeio, conforme os fluxos FLX-002, FLX-003, FLX-004, FLX-008 e FLX-009.

**Pronto quando:** não é possível criar uma avaliação de passeio não finalizado, de não participante, com nota fora de 1 a 5 ou duplicada.

---

## Etapa 5 — Repositórios e senha segura

**Dimensões:** D, F · **Item da correção:** "Persistência dentro da entidade"

1. Criar interfaces em `Repositorios/`:
   ```csharp
   public interface IUsuarioRepositorio
   {
       Usuario Adicionar(Usuario u);
       Usuario? ObterPorId(int id);
       Usuario? ObterPorEmail(string email);
       IReadOnlyList<Usuario> ListarPasseadoresAtivos();
   }
   public interface IPasseioRepositorio { /* Adicionar, ObterPorId, ListarPorUsuario */ }
   public interface INotificacaoRepositorio { /* Adicionar, ObterPorId, ListarPorDestinatario */ }
   ```
   Os pets ficam dentro do agregado `Usuario`, então basta um `ObterPet(int petId)` no repositório de usuários.
2. Implementações `UsuarioRepositorioEmMemoria` etc., registradas como **singleton** no `Program.cs` (`builder.Services.AddSingleton<IUsuarioRepositorio, UsuarioRepositorioEmMemoria>()`).
3. Senha: usar `Microsoft.AspNetCore.Identity.PasswordHasher<Usuario>`, que já vem no ASP.NET Core sem pacote extra.
   - O cadastro gera o `SenhaHash` antes de criar o usuário.
   - O login usa `VerifyHashedPassword`.
   - O domínio guarda **apenas** o hash.
4. O login sai da entidade e vai para um `AutenticacaoServico` (em `Servicos/`): busca por e-mail, verifica o hash e **recusa usuário `BLOQUEADO`** com "Usuário está bloqueado" (REG-011).
5. O cadastro recusa e-mail duplicado com "E-mail já cadastrado" (REG-010).

**Pronto quando:** `Usuario.cs` não tem nada `static`, nenhuma senha em texto puro é armazenada, e e-mail duplicado e usuário bloqueado são recusados.

---

## Etapa 6 — Variação polimórfica

**Dimensões:** F, G, A · **Item da correção:** "A abstração não foi definida nem exercida"

Variação escolhida: **política de preço do passeio**. O valor cobrado pode variar por regra de negócio, e o `Passeio` não precisa saber qual regra está em uso.

```csharp
public interface IPoliticaDePreco
{
    decimal Calcular(Passeio passeio);
}

public class PrecoPorHora : IPoliticaDePreco
{
    public decimal Calcular(Passeio p) => (decimal)p.Duracao.TotalHours * p.Passeador.ValorHora;
}

public class PrecoComAdicionalPorPorte : IPoliticaDePreco
{
    private readonly IPoliticaDePreco _base;
    public PrecoComAdicionalPorPorte(IPoliticaDePreco @base) => _base = @base;
    public decimal Calcular(Passeio p) => _base.Calcular(p) * (p.Pet.Porte == Porte.GRANDE ? 1.3m : 1.0m);
}
```

- Novo enum `TipoCobranca { POR_HORA, POR_HORA_COM_ADICIONAL_DE_PORTE }` em `Enums.cs`, guardado no `Usuario` (passeador).
- `PoliticaDePrecoFabrica.Para(TipoCobranca)` é o **único** lugar que escolhe a implementação. `Usuario.SolicitarPasseio` pede a política da fábrica e a entrega ao construtor do `Passeio`, que guarda o `Valor`. Um `switch` espalhado pelo código **não** conta como polimorfismo.
- Escrever um teste que use as duas implementações pelo mesmo contrato.

A seção **Variação polimórfica** da `proposta.md` já descreve essa variação (assim como Escopo da AV1/AV2, Fora do escopo, Riscos e Participação). Se o código mudar, atualizem a proposta junto.

As interfaces de repositório da etapa 5 também contam para abstração (F).

**Pronto quando:** existe uma interface com duas implementações em uso e a proposta descreve essa variação.

---

## Etapa 7 — Testes das regras

**Dimensão:** H · **Item da correção:** "Nenhum teste"

Em `backend/Projeto.Tests/`, um arquivo por objeto (`PasseioTests.cs`, `UsuarioTests.cs`, `AvaliacaoTests.cs`, `PoliticaDePrecoTests.cs`). No mínimo, conferindo também a **mensagem** de erro:

| Regra | Teste |
|---|---|
| REG-001 | Avaliar passeio `ACEITO` lança erro · avaliar duas vezes o mesmo passeio lança erro · nota 0 e nota 6 lançam erro · avaliação válida recalcula a média |
| REG-002 | Usuário `PASSEADOR` chamando `SolicitarPasseio` lança erro · `AMBOS` consegue |
| REG-003 | Outro passeador aceitando lança erro · passeador `BLOQUEADO` aceitando lança erro · usuário `TUTOR` aceitando lança erro |
| REG-004 | Cancelar `EM_ANDAMENTO` lança erro · cancelar `FINALIZADO` lança erro · cancelar `SOLICITADO` e `ACEITO` funciona |
| REG-005 | Solicitar passeio com pet de outro tutor lança erro |
| REG-006 | Usuário que não participou avaliando lança erro |
| REG-007 | `ObterEnderecoPara(outroUsuario)` lança erro · o próprio usuário consegue |
| REG-008 | Solicitar com data no passado lança erro (usando `agora` fixo no teste) |
| REG-009 | Solicitar para usuário `TUTOR` ou `BLOQUEADO` lança erro |
| REG-010 | Cadastrar o mesmo e-mail duas vezes lança erro |
| REG-011 | Login de usuário `BLOQUEADO` lança erro |
| Ciclo de vida | `SOLICITADO → Finalizar` lança erro · caminho feliz completo `Aceitar → Iniciar → Finalizar` · `RECUSADO` não aceita nenhuma transição |
| Polimorfismo | As duas políticas de preço calculam valores esperados |

Dica: criem um `Fabrica` (test helper) com métodos como `CriarTutorComPet()` e `CriarPasseioAceito()` para não repetir setup.

**Pronto quando:** `dotnet test` passa com pelo menos um teste por regra REG-001 a REG-011.

---

## Etapa 8 — Ligar o domínio à API

**Dimensões:** I, J, D · **Item da correção:** "O domínio está desconectado da API"

Separar os endpoints em arquivos em `Endpoints/` (`UsuarioEndpoints.cs` já existe; criar `PetEndpoints.cs`, `PasseioEndpoints.cs` e `NotificacaoEndpoints.cs`) usando métodos de extensão `MapXxxEndpoints(this IEndpointRouteBuilder app)`. O `Program.cs` fica só com configuração.

Os endpoints devem ser **finos**: buscar no repositório, chamar o método de domínio e salvar. Nenhuma regra fica no endpoint.

| Método | Rota | Domínio chamado |
|---|---|---|
| POST | `/cadastro` (rota que já existe e que o frontend usa) | cadastro com hash de senha e e-mail único |
| POST | `/login` | `AutenticacaoServico` |
| GET | `/passeadores` | `ListarPasseadoresAtivos`, retornando **perfil público sem endereço** (REG-007) |
| GET | `/usuarios/{id}/pets` | `usuario.Pets` |
| GET | `/usuarios/{id}/pets/{petId}` | |
| POST | `/usuarios/{id}/pets` | `usuario.AdicionarPet(...)` |
| PUT | `/usuarios/{id}/pets/{petId}` | `pet.Atualizar(...)` |
| DELETE | `/usuarios/{id}/pets/{petId}` | `usuario.RemoverPet(pet)` |
| POST | `/passeios` | `tutor.SolicitarPasseio(...)` e notificação `PASSEIO_SOLICITADO` |
| GET | `/usuarios/{id}/passeios` | passeios como tutor ou passeador |
| POST | `/passeios/{id}/aceitar` · `/recusar` · `/iniciar` · `/finalizar` · `/cancelar` | `passeio.Aceitar(quem)` etc. e a notificação correspondente |
| POST | `/passeios/{id}/avaliacoes` | `passeio.Avaliar(...)` |
| GET | `/usuarios/{id}/notificacoes` (com `?naoLidas=true`) | `NotificacaoServico.ListarNaoLidas` |
| POST | `/notificacoes/{id}/lida` | `notificacao.MarcarComoLida()` |

Padrões:
- **DTOs** em `Contratos/` para entrada e saída. Nunca devolver a entidade direto, pois isso vazaria `SenhaHash` e endereço.
- **REG-007 nos DTOs:** o perfil público do passeador mostra nome, média e valor por hora; o DTO do passeio mostra o `Local` informado na solicitação, nunca o endereço do perfil do tutor.
- Tratamento de erro único: `ArgumentException` vira **400**, `InvalidOperationException` (regra violada) vira **409** ou **422**, e recurso inexistente vira **404**. Sempre devolver JSON `{ "erro": "mensagem" }`, com o texto da regra.
- Ações de passeio recebem `{ "usuarioId": n }` no corpo para identificar quem age. Autenticação real (JWT) está registrada como fora do escopo, na seção "Fora do escopo" da proposta.
- Atualizar `Projeto.Api.http` com exemplos de cada rota.

**Pronto quando:** todo o fluxo FLX-001 a FLX-009 pode ser executado pelo arquivo `.http`.

---

## Etapa 9 — Frontend: rotas, integração e CRUD de pet

**Dimensões:** I, J, K · **Itens da correção:** "O frontend não entrega o CRUD exigido", "A integração é parcial e repetida" e "A organização tem uma tela morta"

### 9.1 Estrutura

```
frontend/src/
  pages/          LoginPage, CadastroPage, HomePage, PetsPage, PetFormPage, PasseiosPage
  components/     componentes reutilizáveis (PetCard, Mensagem, Header, RotaProtegida)
  services/       api.js, usuarioService.js, petService.js, passeioService.js
  context/        AuthContext.jsx (usuário logado)
  styles/
```

- Mover as telas de `components/` para `pages/`.
- Cada tela importa **apenas o seu** CSS. Remover os imports de CSS de `main.jsx`.
- Trocar `class=` por `className=` em `LoginScreen.jsx`.

### 9.2 Camada de serviço

`frontend/.env`:
```
VITE_API_URL=http://localhost:5229
```

`services/api.js`:
```js
const BASE_URL = import.meta.env.VITE_API_URL;

export async function api(caminho, { method = 'GET', body } = {}) {
  const resposta = await fetch(`${BASE_URL}${caminho}`, {
    method,
    headers: body ? { 'Content-Type': 'application/json' } : undefined,
    body: body ? JSON.stringify(body) : undefined,
  });
  if (!resposta.ok) {
    const dados = await resposta.json().catch(() => ({}));
    throw new Error(dados.erro || `Erro ${resposta.status}`);
  }
  return resposta.status === 204 ? null : resposta.json();
}
```

`services/petService.js`: `listarPets(usuarioId)`, `obterPet`, `criarPet`, `atualizarPet`, `excluirPet`. Nenhuma tela chama `fetch` diretamente.

### 9.3 Telas

| Tela | O que muda |
|---|---|
| Login | Remover `MOCK_EMAIL`/`MOCK_SENHA` e a dica de credenciais; chamar `POST /login`; guardar o usuário no `AuthContext` (e em `localStorage` para sobreviver ao refresh); mostrar erro 401 e a mensagem de usuário bloqueado |
| Cadastro | Adicionar a rota `/cadastro` em `App.jsx`; remover a cópia em `localStorage`; após sucesso, ir para o login |
| Home | Remover o `fetch('/teste')`; cards levam a "Meus pets" e "Passeios" |
| **Meus pets** (nova) | **Read:** lista via `GET`, com estados de carregando, vazio e erro; botões Editar/Excluir em cada pet |
| Formulário de pet | **Create** em `/pets/novo` e **Update** em `/pets/:id/editar`, o mesmo componente; no modo edição, carrega o pet via `GET`; o select de porte envia 0, 1 ou 2 (PEQUENO, MEDIO, GRANDE); remover o `localStorage` |
| Excluir | **Delete** com confirmação construída na própria tela (não `window.confirm`); a lista é atualizada após excluir |
| Rotas protegidas | `RotaProtegida` redireciona para `/` se não houver usuário logado; o botão "Sair" limpa o contexto |

### 9.4 Passeios (fluxos FLX-001 a FLX-009)

Depois do CRUD de pet:
- Lista de passeadores ativos com o botão "Solicitar".
- Formulário de solicitação: pet (select com os pets do tutor), data, duração e local pré-preenchido.
- Tela "Meus passeios": botões Aceitar/Recusar/Iniciar/Finalizar/Cancelar conforme o perfil e o status.
- Avaliação (nota de 1 a 5) para passeio finalizado.
- Contador de notificações não lidas no header.

### 9.5 README

Atualizar o `README.md`: remover as credenciais de teste mockadas e listar as rotas reais do frontend (incluindo `/cadastro` e as telas de pets e passeios).

**Pronto quando:** dá para fazer login com um usuário real, cadastrar, listar, editar e excluir um pet sem recarregar a página, e os dados vêm da API (conferir na aba Network).

---

## Ordem sugerida e divisão entre o grupo

| Ordem | Etapas | Pode ser feito em paralelo com |
|---|---|---|
| 1 | 0 (concluída) | — |
| 2 | 1 + 2 (entidades e `Usuario`) | 9.1 e 9.2 (estrutura e serviços do front) |
| 3 | 3 + 4 (Passeio, Avaliação, Notificação) | 7 (testes, escritos junto com cada regra) |
| 4 | 5 + 6 (repositórios, senha, polimorfismo) | 9.3 Login/Cadastro (depende só de `/cadastro` e `/login`) |
| 5 | 8 (endpoints) | 9.3 CRUD de pet |
| 6 | 9.4 (passeios no front) | revisão final da `proposta.md` (alinhar com o código) |

Uma divisão possível para 5 pessoas: (1) entidades e Usuario, (2) Passeio, Avaliação e Notificação, (3) repositórios, API e polimorfismo, (4) testes, (5) frontend. Façam revisão cruzada nos PRs (exemplo: quem faz os testes revisa o PR do `Passeio`). A seção **Participação** da proposta já registra esse acordo; confirmem com o grupo.

---

## Checklist final (cada item do "O que faria a nota subir")

- [ ] 1. `Passeio` com `Aceitar/Recusar/Iniciar/Finalizar/Cancelar` e `Status` com `private set`
- [ ] 2. Nenhum `{ get; set; }` público no domínio; coleções como `IReadOnlyList`; construtores exigindo o essencial
- [ ] 3. `Tutor.cs` e `Passeador.cs` removidos; `Usuario` único com `TipoPerfil`
- [ ] 4. `Passeio` guarda `Usuario Tutor`, `Usuario Passeador` e `Pet Pet`
- [ ] 5. Endpoints de pet (CRUD) e de passeio (ciclo de vida) ligados ao domínio; template de clima removido
- [ ] 6. Persistência em repositórios; senha com hash; e-mail único e usuário bloqueado recusado no login
- [ ] 7. `/cadastro` roteado; login chamando a API; pet com CRUD pela API; `services/api.js` com `VITE_API_URL`
- [ ] 8. Testes cobrindo REG-001 a REG-011 e as transições proibidas
- [ ] Extra: `proposta.md` preenchida e igual ao código (variação polimórfica, escopo, fora do escopo, riscos, participação, conceitos e regras)
