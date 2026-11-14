# Análise do Modelo — PetWalk

## Decisão 1 — Um único `Usuario` com `TipoPerfil` vs. classes `Tutor` e `Passeador` separadas

### Contexto histórico

O commit `c171558` ("Apagar Tutor.cs e Passeador.cs") removeu as classes separadas. A alternativa adotada usa `Usuario` com o enum `TipoPerfil { TUTOR, PASSEADOR, AMBOS }`.

### Alternativas

| Critério | Alt. A: Tutor e Passeador separados | Alt. B (adotada): Usuario + TipoPerfil |
|---|---|---|
| Usuário com dois papéis | Precisaria de dois objetos ou herança múltipla | Um único objeto com TipoPerfil.AMBOS |
| E-mail único (REG-010) | Precisaria verificar em duas tabelas | Verificado em uma tabela só |
| Login e autenticação | Dois fluxos distintos | Um único AutenticacaoServico |
| Regras REG-002/REG-009 | if (obj is Tutor) / if (obj is Passeador) | EhTutor / EhPasseador (propriedades calculadas) |
| Custo de herança | Hierarquia de classes, acoplamento de tipo | Sem herança; composição por enum |

### Justificativa no código

```csharp
public bool EhTutor => TipoPerfil is TipoPerfil.TUTOR or TipoPerfil.AMBOS;
public bool EhPasseador => TipoPerfil is TipoPerfil.PASSEADOR or TipoPerfil.AMBOS;
```

`AutenticacaoServico` trata um único tipo de usuário.

## Decisão 2 — Política de preço: interface Strategy vs. switch em Passeio vs. subclasses de Passeio

### Contexto

O sistema tem dois tipos de cobrança (`TipoCobranca`): `POR_HORA` e `POR_HORA_COM_ADICIONAL_DE_PORTE`. A decisão é: onde e como calcular o valor?

### Alternativas

| Critério | Alt. A: switch em Passeio | Alt. B: PasseioPorHora / PasseioComAdicional | Alt. C (adotada): IPoliticaDePreco + Fábrica |
|---|---|---|---|
| Adicionar novo tipo de cobrança | Alterar Passeio (viola Aberto/Fechado) | Criar subclasse de Passeio (hierarquia cresce) | Criar nova classe que implemente IPoliticaDePreco |
| Testabilidade isolada | Precisa de Passeio completo | Precisa instanciar subclasse correta | Testar PrecoPorHora com Passeio fake |
| Responsabilidade | Passeio sabe calcular preço | Passeio sabe qual é seu tipo | Passeio não conhece cálculo — recebe política pronta |
| Valor calculado quando? | Poderia recalcular a cada acesso | Idem | Uma vez no construtor; Valor é imutável |

### Justificativa no código

- `IPoliticaDePreco` em `IPoliticaDePreco.cs`: contrato mínimo.
- `PoliticaDePrecoFabrica.Para(TipoCobranca)` em `PoliticaDePrecoFabrica.cs`: único ponto de escolha.
- `Usuario.SolicitarPasseio` chama a fábrica e passa a política ao construtor de `Passeio`.
- `Passeio` só conhece `IPoliticaDePreco` — nunca `PrecoPorHora` ou `PrecoComAdicionalPorPorte` diretamente.
- `PrecoComAdicionalPorPorte` usa Decorator: encapsula `_base` (`IPoliticaDePreco`) em vez de herdar, permitindo composição futura.
