# myProject

Metroidvania em MonoGame que replica a **precisão e a física de movimento do Celeste**.
O framework **Monocle** e a física do Player foram portados do código decompilado do Celeste.

> **O objetivo NÃO é recriar o Celeste.** Só o movimento é fiel; o conteúdo do jogo é
> podado ou vira andaime. O jogo em si (combate, salas, inimigos) é original.

- **Fonte de referência:** `C:\Users\kelvi\OneDrive\Documents\celeste_source` (decompilado, somente leitura)
- **Stack:** .NET 9, MonoGame 3.8 DesktopGL. Sem outras dependências — manter assim.
- **Repo:** https://github.com/Kynsed/myProject

## Regra central

Ao portar qualquer coisa do `celeste_source`:

| Afeta mecânica/física de movimento? | O que fazer |
|---|---|
| Sim | Portar **fiel**, bit a bit: mesmas constantes, mesma ordem de operações |
| Não | Podar ou fazer stub, e **marcar com `// NOTE`** explicando o que saiu e por quê |

Correções de bug em relação ao original vão marcadas com `// FIX`. Hoje há `// NOTE`
em 41 arquivos — é o rastro que torna o port auditável.

## Estrutura

```
Monocle/          Engine portada (71 arquivos). Auditada token-a-token vs o source.
*.cs (raiz)       Classes do jogo (57). Port fiel de movimento + stubs de conteúdo.
Inspector/        Ferramenta própria de inspeção em runtime (não é port).
Content/map.txt   Mapa do demo no formato próprio (ver RoomMap.cs).
```

Namespaces: `Monocle` (engine), `myProject` (jogo), `myProject.Inspector.*` (ferramenta),
`MonocleSmoke` (harnesses e testes), `FMOD.Studio` (shim de 1 classe).

## Comandos

```bash
dotnet build
```

Modos de execução (`dotnet run -- <modo>`):

| Modo | O que faz |
|---|---|
| `--play` | Demo jogável. F1 abre o inspector; clique seleciona entidade. |
| `--parity` | **54 asserts** de paridade de movimento vs constantes do Celeste |
| `--combat-test` | **41 asserts** do sistema de combate |
| `--inspector-test` | **48 asserts** de reflexão/atributos/undo do inspector |
| `--phys-test` | Física Actor/Solid headless (8 asserts) |
| `--player-smoke` | Player real por 180 frames, checa crash |
| `--player-fuzz` | Player com input injetado (dash/climb/wall-jump) |
| `--inspector-shot <png>` | Renderiza o inspector e salva PNG do backbuffer |

**Rode a bateria inteira antes de commitar.** Qualquer regressão em `--parity` significa
que a fidelidade do movimento quebrou.

## Branches

| Branch | Conteúdo |
|---|---|
| `main` | Port completo + combate |
| `port` | Só o port, **antes** do combate (referência limpa) |
| `inspector` | `main` + o inspector de runtime |
| `feature/combat` | Histórico do combate (já mergeado na main) |

## Armadilhas conhecidas

**Portar do decompilado**
- Aritmética de `Facings` (`Facing * (Facings)N`, `-Facing`) não compila → usar `(int)Facing`
- MonoGame 3.8 tem `Vector2.Floor()/Round()` de instância (void) que ofuscam as extensões
  do Calc → usar `.Floored()` / `Calc.Round` / `Calc.Rotate`
- Remover os comentários `// Token: 0x... RID:` do decompilador
- Diffs contra o source mostram ruído que **não** é divergência: `!!0`/`!!1` são
  placeholders genéricos; `MathHelper.Pi` tem os mesmos bits das literais; casts `(float)`
  implícitos; nomes de variáveis locais

**Harness headless**
- `Engine.DeltaTime`/`RawDeltaTime` são `{ get; private set; }` → setar via reflection
- `Engine.Pooler` só nasce no ctor do Engine → testes que **removem entidades** precisam
  de `typeof(Engine).GetProperty("Pooler").SetValue(null, new Pooler())`
- BitTags (`Tags.*`) devem existir **antes** de qualquer `Scene` (RunClassConstructor)
- Todo tipo consultado via Tracker (`CollideFirst<T>`, `GetEntities<T>`) precisa de
  `[Tracked]` **direto na classe** — herança não basta neste port
- `Engine.Scene = x` só troca a cena no fim do Update; guarde a referência local
- `MInput.Keyboard.Pressed` só dispara na transição → em testes que "mashan" uma tecla,
  intercale um frame neutro

**Comportamentos fiéis que parecem bug em teste**
- Player parado sobre um FlyFeather **recoleta a cada respawn** (`StartStarFly` renova o
  voo quando já está no estado 19)
- Dashar de dentro de um Booster **não** notifica DashListeners: o Player chama
  `PlayerBoosted` e o booster segura o player até o dash acabar
- SwapBlock já está voltando aos 60 frames (`returnTimer` 0.8s ≈ 48) — medir o pico de X

**Inspector**
- A fonte é bitmap 5×7 e cobre **só ASCII imprimível** — nada de acento ou travessão nas
  strings de UI
- `GuiStyle.Scale` (padrão 2) rege todas as métricas; não hardcode tamanhos
- Objetos aninhados começam recolhidos por necessidade: o grafo tem ciclos
  (`Entity → Scene → Entities → Entity`) e abrir por padrão dá stack overflow

## Estilo de resposta (definido pelo usuário)

- Frases curtas, imperativo. Sem preâmbulo, sem conclusão, sem narrar intenção.
- Resultado **primeiro**; a análise depois.
- Ao analisar código, nesta ordem: 1) dependências de movimento; 2) dependências de
  conteúdo; 3) o que portar; 4) o que podar; 5) o que virar andaime/stub.
