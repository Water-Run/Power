# Filme de combustível líquido finito e evaporação

[English](FUEL_FILM.md) · [简体中文](FUEL_FILM.zh-CN.md) · [Français](FUEL_FILM.fr.md) · [Русский](FUEL_FILM.ru.md) · [日本語](FUEL_FILM.ja.md) · [한국어](FUEL_FILM.ko.md) · [Deutsch](FUEL_FILM.de.md) · [Español](FUEL_FILM.es.md) · [Italiano](FUEL_FILM.it.md) · **Português**

`fuel_film` guarda um inventário líquido inicial explícito ao lado de um receptor de gás
rastreado. Um nó térmico finito fornece o calor sensível e o de mudança de fase. O combustível
evaporado entra na massa, na energia interna e no constituinte de combustível do receptor; a reação
premisturada existente consome só vapor. O inventário líquido é molhagem inicial,
não combustível injetado, e continua fazendo parte do livro de massa total e de energia química.

É um modelo de pesquisa de propriedades constantes, com volume de deslocamento líquido
desprezível e temperatura de saturação prescrita. Não implementa dinâmica de trilho
líquido, de agulha nem de pulverização, equilíbrio de fases dependente da pressão, condensação,
propriedades de combustível multicomponente nem comportamento de gasolina calibrado.

## Energia de fase e fonte de calor finita

Sejam `c_l` o calor específico do líquido, `c_v` o calor específico isocórico do gás receptor,
`T_s` a temperatura de saturação declarada e `L_u > 0` a diferença de energia interna
específica vapor menos líquido em `T_s`. A referência térmica compartilhada é:

```text
e_offset = (c_v - c_l) T_s - L_u
U_liquid = m_liquid (c_l T_liquid + e_offset)
U_vapor_added = delta_m c_v T_s
```

`L_u` é uma diferença de energia interna em J/kg, e não uma entalpia de
vaporização. Uma entalpia fornecida precisa de uma conversão explícita e justificada antes de
poder ser usada aqui. A energia térmica do líquido pode ser negativa sob esta referência;
temperatura e massa ainda precisam ser fisicamente admissíveis. A energia química
`m_liquid * LHV` é separada e se transfere com o vapor sem criar calor de reação
nem trabalho de fonte externo.

Abaixo da saturação, a condutância `K` acopla a capacidade líquida `m_liquid c_l` à capacidade
finita da parede `C_w`. A diferença de temperatura decai de forma analítica à taxa
`K (1 / (m_liquid c_l) + 1 / C_w)`. A temperatura média ponderada pela capacidade permanece
constante. Se o líquido atinge `T_s`, a lei resolve esse instante e usa o
intervalo restante para a evaporação.

Na saturação com `T_wall > T_s`, o superaquecimento da parede decai à taxa `K / C_w`.
O calor de fase disponível no intervalo `h` é
`C_w (T_wall - T_s) (1 - exp(-K h / C_w))`, limitado por `m_liquid L_u`.
O filme permanece em `T_s` até secar; a massa evaporada é o calor de fase dividido por
`L_u`. A secagem deixa exatamente massa e energia líquidas nulas e interrompe a retirada de calor.
Uma parede fria pode resfriar o líquido existente; não condensa o vapor do receptor.

Toda transferência satisfaz `delta U_liquid + U_vapor_added = Q_from_wall`.
A parede perde esse mesmo calor, então a mudança de fase não introduz uma fronteira
de energia externa. Inventários de combustível não negativos e o livro completo de constituintes
são verificados à parte do livro de energia total.

## Contrato de grafo e de documento

| Dado | Requisito |
|---|---|
| `node_a` | Receptor de gás finito com rastreamento explícito de combustível premisturado e LHV |
| `node_b` | Parede térmica finita, com capacidade e temperatura positivas |
| `initial_mass` | kg não negativos; o inventário líquido inicial completo |
| `initial_temperature` | K positivo, não maior que a saturação |
| `liquid_specific_heat` | J/(kg K) positivo, unidade JSON `j_kg_k` |
| `saturation_temperature` | K positivo, unidade JSON `k` |
| `latent_internal_energy` | J/kg positivo, unidade JSON `j_kg` |
| `conductance` | W/K não negativo, unidade JSON `w_k` |

O JSON exige os seis parâmetros. O filme não tem canal de entrada, temporização de virabrequim nem
sumidouro de calor separado. Parâmetros não relacionados, domínios de porta errados, unidades, valores
não finitos e capacidade de estado não suportada são rejeitados. Os chamadores do Core usam
`ComponentDefinition.LiquidFilm` e `FuelFilmDefinition`; a lei independente
`EquilibriumFuelFilm` expõe a criação de estado admissível e o avanço de banho finito.

Cada filme contribui cinco entradas de estado reportadas para o orçamento de estado limitado do compilador.
Massa, energia térmica, histórico de evaporação, fluxo médio, calor de parede e
os históricos compensados pertencem à simulação. Cancelamento, entradas rejeitadas,
falhas tardias do solver, ramificações independentes e intervalos especulativos de embreagem preservam
a transação completa. O avanço bem-sucedido e as leituras de instantâneo não alocam
memória gerenciada depois do aquecimento.

## Precisão da integração

Um intervalo aceito usa meios passos de filme / gás / mecânica e reação / gás / filme.
Filmes que compartilham uma parede rodam em ordem estável de componentes antes do avanço
do gás e em ordem inversa depois. A temperatura finita da parede é carregada
entre os subpassos de filme, e o calor de parede entra na mesma solução térmica.

A lei isolada de banho finito é analítica ao longo do aquecimento sensível, da saturação e
da secagem. A integração simultânea independente de EDOs verifica refinamento suave de segunda ordem
para dois filmes que compartilham uma parede e para vapor transportado por uma saída de gás
bloqueada, sem outras fontes de calor de parede. Elos térmicos de gás
e outras fontes térmicas ainda leem a temperatura explícita da parede no intervalo externo,
então esse acoplamento conserva precisão de primeira ordem. Janelas de reação,
eventos de válvula e secagem precisam das próprias verificações de refinamento; o replay exato do lote, sozinho,
não prova precisão do passo de tempo nem segunda ordem uniforme para um powertrain em combustão.

## Semântica observável e portátil

| Campo do filme | Significado |
|---|---|
| `mass` | Combustível líquido restante, kg |
| `temperature` | Temperatura do líquido; temperatura de saturação declarada quando seco |
| `internal_energy` | Energia térmica líquida com sinal, sob a referência de fase declarada, J |
| `chemical_energy` | Energia química do combustível líquido restante, J |
| `evaporated_fuel_mass` | Vapor entregue acumulado, kg |
| `mass_flow` | Entrega média de vapor no último tick físico completo, kg/s |
| `film_wall_heat` | Calor acumulado retirado da parede, J; o resfriamento pode torná-lo negativo |
| `heat_flow` | `K (T_wall - T_liquid)` instantâneo, W; zero quando seco |

Descubra IDs e unidades de saída pela validação ou pela criação da sessão. Os observáveis globais
de massa, de combustível e de energia química incluem o inventário do filme. A entrega interna
de vapor não incrementa a energia de combustível de reservatório nem a entalpia externa.

O asset v19 guarda todas as propriedades de fase e conserva os leitores v1-v18. Cada filme precisa
de um registro de fase tipado de 64 bytes. Comprimentos e contagens limitados, digest, cobertura completa,
registros duplicados ou ausentes, unidades, compilação física e proteção contra rebaixamento
são verificados. O fixture autêntico de medição de combustível v17 conserva a impressão digital e
o replay atualizado no mesmo runtime. Veja [ASSET_FORMAT.pt-BR.md](ASSET_FORMAT.pt-BR.md).

## Laboratório e trabalho restante

`film-fired-cylinder` aquece um filme inicialmente molhado, admite ar à parte e depois
consome o vapor disponível pela queima de Wiebe prescrita. A parede quente finita
paga o calor de fase; o líquido não queima diretamente. JSON, CLI, replay portátil e
o servidor MCP real concordam em cada limite de relatório. Os contratos de fonte, de esquema e de sessão
permanecem compartilhados; os parâmetros são `unverified`.

Veja [VALIDATION.pt-BR.md](VALIDATION.pt-BR.md) para a evidência numérica medida. Marcadores de filme
e verificações de ciclo de vida preparados no Unity ainda exigem verificação real de Editor/Play.
O [injetor líquido](LIQUID_FUEL_INJECTION.pt-BR.md) separado agora repõe filmes a partir de
uma fonte flexível finita. Bomba e reabastecimento, agulha e pulverização, propriedades medidas de combustível,
ignição e ECU, comportamento completo de admissão e escape, controles de transmissão e powertrains
calibrados continuam como requisitos separados.
