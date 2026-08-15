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

### Podas de movimento (design do metroidvania)

A liberdade do Celeste dissolve o gate de progressão de um metroidvania. As liberdades
cortadas **não são apagadas**: ficam atrás de portões em [`Abilities.cs`](Abilities.cs),
desligados por padrão e ligados depois pela progressão (upgrade de personagem).

| Portão | Desligado (jogo hoje) | Ligado (upgrade) |
|---|---|---|
| `DashDiagonal` | dash diagonal cai p/ a horizontal; **sem hyper dash** | dash em 8 direções |
| `DashVertical` | dash p/ cima/baixo vira dash p/ frente; **sem super wall jump** | dash vertical |
| `WallClimb` | grab não agarra parede; **sem climb jump nem ledge hop** | escalada completa |

Wall jump, wall slide e pegar `Holdable` **continuam** — não passam por `ClimbCheck`.
`--parity` liga tudo (`Abilities.EnableAll()`): ele audita o port, não o design.
As podas têm harness próprio (`--poda-test`), que mede portão ligado **e** desligado.

### Câmera (sistema próprio)

A câmera do Celeste é presa à sala e reage direto ao Player. A do metroidvania é uma
follow camera em [`GameCamera.cs`](GameCamera.cs): zona morta, **centralização em
movimento contínuo** (com o atraso da suavização, ~14px a 90px/s), **posição de descanso**
deslocada para o lado que o player encara quando parado, vertical que ignora pulos curtos
e acompanha quedas longas, olhar p/ cima/baixo segurando o direcional parado no chão,
clamp na sala e suavização em tudo. A antecipação (look ahead) existe no código mas está
**desligada** (`LookAheadFracX = 0`): em movimento o pedido é centralizar.

**Enquadramento vertical:** o player não fica no meio da tela — os pés ficam a
`GroundLineFrac` (78%) da altura, então o chão cai no rodapé e sobra uma faixa do que
existe abaixo dele, abrindo o cenário acima (enquadramento de metroidvania).

**Zoom:** a tela é sempre 320×180; `Zoom` decide quanto mundo cabe nela (padrão `1.35` →
237×133, enquadramento estilo Hollow Knight). Mudar `Zoom` em runtime é uma transição, não
um corte. Por isso os deslocamentos de enquadramento (antecipação, descanso, olhar, folga
máxima) são **frações da meia-tela**, não pixels fixos: valem em qualquer zoom. Já a zona
morta e as coleiras verticais são px de mundo — elas acompanham o movimento, não a tela.

Enquanto existir uma `GameCamera` na cena ela é a **dona** de `Level.Camera`, e o follow
fiel do Celeste (`Player.Update`) fica desligado por `Level.FollowCamera`. Os harnesses
de paridade rodam **sem** `GameCamera`, então `--parity` continua medindo o port.
A transição entre salas é do port (`Level.TransitionRoutine`, pan CubeOut): a câmera nem
atualiza durante ela e se re-sincroniza depois, sem pulo.

## Estrutura

```
Monocle/          Engine portada (71 arquivos). Auditada token-a-token vs o source.
*.cs (raiz)       Classes do jogo (69). Port fiel de movimento + stubs de conteúdo.
Inspector/        Ferramenta própria de inspeção em runtime (não é port).
Content/map.txt   Mapa do demo no formato próprio (ver RoomMap.cs).
                  Salas de 384px de altura (tela = 180): sem essa folga a câmera
                  fica presa no clamp e não centra o player.
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
| `--parity` | **54 asserts** de paridade de movimento vs constantes do Celeste (roda com `Abilities.EnableAll()`) |
| `--poda-test` | **15 asserts** das podas de movimento (dash horizontal, sem wallclimb, golpe p/ cima) |
| `--combat-test` | **44 asserts** do sistema de combate |
| `--camera-test` | **41 asserts** da câmera (enquadramento, zoom, descanso, olhar, limites) — inclui o mapa real |
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
- Cena com parede em `x[0,8]`: spawnar em `x=9` deixa a hitbox (8 larg, offset −4)
  **dentro** do sólido — o `ClimbCheck` passa mas o Actor não sobe. Use `x=12` (adjacente)
  em qualquer teste que meça movimento na parede

**Level design (vocabulário do movimento portado)**
- O pulo sobe **~19px**: degrau de **16px (2 tiles)** é o limite, e ainda é apertado —
  a janela em que os pés passam acima da plataforma é de ~3px. Degrau confortável = 1 tile
- Sem wallclimb, nada de paredes como caminho vertical: subida é por plataformas
- Sala precisa de **~84px acima e abaixo** do player para a câmera centrar sem bater no clamp

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
