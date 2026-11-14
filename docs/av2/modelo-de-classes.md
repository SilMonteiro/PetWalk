# Modelo de Classes — PetWalk

```mermaid
classDiagram
    class Usuario {
        +int Id
        +string Nome
        +string Email
        +string Telefone
        +TipoPerfil TipoPerfil
        +StatusUsuario Status
        +decimal ValorHora
        +TipoCobranca TipoCobranca
        +double MediaAvaliacao
        +IReadOnlyList~Pet~ Pets
        +bool EhTutor
        +bool EhPasseador
        +ObterEnderecoPara(Usuario solicitante) string
        +AdicionarPet(Pet pet) void
        +RemoverPet(Pet pet) void
        +SolicitarPasseio(Pet pet, Usuario passeador, DateTime dataHora, TimeSpan duracao, string local, DateTime agora) Passeio
        +DefinirCobranca(decimal valorHora, TipoCobranca tipoCobranca) void
        +Bloquear() void
        +Desbloquear() void
    }
    class Pet {
        +int Id
        +string Nome
        +string Especie
        +string Raca
        +Porte Porte
        +int Idade
        +string Observacoes
        +Usuario Tutor
        +IReadOnlyList~string~ Vacinas
        +Atualizar(string nome, string especie, string raca, Porte porte, int idade, string observacoes) void
        +RegistrarVacina(string vacina) void
    }
    class Passeio {
        +int Id
        +DateTime DataHora
        +TimeSpan Duracao
        +string Local
        +decimal Valor
        +StatusPasseio Status
        +Usuario Tutor
        +Usuario Passeador
        +Pet Pet
        +IReadOnlyList~Avaliacao~ Avaliacoes
        +Aceitar(Usuario quem) void
        +Recusar(Usuario quem) void
        +Iniciar(Usuario quem) void
        +Finalizar(Usuario quem) void
        +Cancelar(Usuario quem) void
        +Avaliar(Usuario avaliador, int nota, string comentario, DateTime agora) Avaliacao
        +Participou(Usuario u) bool
    }
    class Avaliacao {
        +int Id
        +int Nota
        +string Comentario
        +DateTime Data
        +Usuario Avaliador
        +Usuario Avaliado
        +Passeio Passeio
    }
    class Notificacao {
        +int Id
        +Usuario Destinatario
        +TipoNotificacao Tipo
        +string Mensagem
        +bool Lida
        +DateTime DataEnvio
        +Passeio Passeio
        +MarcarComoLida() void
    }
    class IPoliticaDePreco {
        <<interface>>
        +Calcular(Passeio passeio) decimal
    }
    class PrecoPorHora {
        +Calcular(Passeio passeio) decimal
    }
    class PrecoComAdicionalPorPorte {
        -IPoliticaDePreco _base
        +Calcular(Passeio passeio) decimal
    }
    class PoliticaDePrecoFabrica {
        <<static>>
        +Para(TipoCobranca tipo) IPoliticaDePreco$
    }

    Usuario "1" *-- "0..*" Pet : pets
    Pet --> Usuario : tutor
    Passeio --> Usuario : tutor
    Passeio --> Usuario : passeador
    Passeio --> Pet
    Passeio "1" *-- "0..2" Avaliacao : avaliações
    Avaliacao --> Usuario : avaliador
    Avaliacao --> Usuario : avaliado
    Notificacao --> Usuario : destinatário
    Notificacao --> Passeio
    IPoliticaDePreco <|.. PrecoPorHora
    IPoliticaDePreco <|.. PrecoComAdicionalPorPorte
    PrecoComAdicionalPorPorte o-- IPoliticaDePreco : base
    PoliticaDePrecoFabrica ..> IPoliticaDePreco
    Usuario ..> PoliticaDePrecoFabrica
```

> Propriedades privadas omitidas do diagrama: `_endereco`, `_pets`, `_avaliacoesRecebidas` (Usuario); `_avaliacoes` (Passeio); `_vacinas` (Pet). Construtores privados para materialização pelo EF Core também omitidos.
