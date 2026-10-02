# Comando de válvulas por ângulo de virabrequim

[English](VALVE_TIMING.md) · [简体中文](VALVE_TIMING.zh-CN.md) · [Français](VALVE_TIMING.fr.md) · [Русский](VALVE_TIMING.ru.md) · [日本語](VALVE_TIMING.ja.md) · [한국어](VALVE_TIMING.ko.md) · [Deutsch](VALVE_TIMING.de.md) · [Español](VALVE_TIMING.es.md) · [Italiano](VALVE_TIMING.it.md) · **Português**

Um `valve_timing` opcional num `gas_orifice` multiplica a abertura por uma envoltória periódica
de ângulo de virabrequim. Aceita vasos de gás fixos e cilindros móveis pelas
mesmas definições de Core, JSON, CLI, MCP e asset portátil. A envoltória segue a posição
real do virabrequim durante aceleração, parada e reversão. Representa a área
efetiva de escoamento; não modela contato do came, levantamento físico da válvula, forças de mola nem atrito.

## Contrato e fase

```json
"valve_timing": {
  "crank_node": 1,
  "cycle_angle": { "value": 720, "unit": "deg" },
  "open_angle": { "value": 520, "unit": "deg" },
  "duration_angle": { "value": 200, "unit": "deg" }
}
```

`crank_node` deve identificar um nó rotacional. Os três ângulos exigem unidades explícitas `deg`
ou `rad`. O ângulo de ciclo é exatamente 360 ou 720 graus; a duração fica entre 1e-6
radianos e o ângulo de ciclo. O ângulo de abertura é finito e normalizado módulo o ciclo.
Ângulos negativos e um lóbulo que cruza o limite do ciclo são aceitos. Várias válvulas
podem referenciar um virabrequim, inclusive lóbulos sobrepostos.

A fase é relativa ao ângulo do virabrequim referenciado, inclusive a posição inicial.
A fase geométrica de um cilindro **não** é somada automaticamente: o autor deve escolher o
ângulo de abertura de válvula adequado para cada cilindro. Um ciclo de 720 graus distingue
revoluções sucessivas do virabrequim. Não há uma fase implícita de quatro tempos inferida da
posição do pistão, da velocidade ou do tempo decorrido.

Para o ciclo `C`, o ângulo de abertura `a`, a duração `D`, a abertura de pico `u` e o ângulo de virabrequim `theta`:

```text
s = modulo(theta - a, C)       // em [0, C)
opening = u sin²(pi s / D)     // 0 < s < D
opening = 0                   // caso contrário, inclusive os dois limites
A_effective = A_orifice * opening
```

Este perfil e a primeira derivada são contínuos nos limites do lóbulo. A rotação
inversa o percorre de novo; um virabrequim parado mantém a abertura atual e pode continuar em escoamento.
A abertura não escolhe o sentido do escoamento: a lei de orifício bidirecional existente,
comandada pela pressão, continua valendo. O coeficiente de descarga permanece um multiplicador separado.

Numa restrição temporizada, `initial_input` e o canal de entrada opcional especificam a **abertura
de pico**, uma fração em [0, 1]. A quantidade do canal é `peak_opening`; zero desativa
o lóbulo. A quantidade de saída `effective_opening` usa `Field.Opening` (campo de KPI JSON
`opening`) e informa a fração real. `mass_flow` é avaliado com essa fração.
Restrições sem temporização conservam a quantidade de entrada e a semântica existentes. Mudanças de pico
programadas e interativas conservam a validação atômica de entrada e as verificações de revisão.

## Integração e recuperação

Modelos temporizados usam integração simétrica de meio passo de gás / passo inteiro de trabalho do virabrequim / meio passo de gás,
inclusive um vaso fixo acionado por um virabrequim independente. A primeira metade
usa o ângulo inicial do virabrequim, e a segunda o ângulo resultante. O solver de gás
resolve a própria dinâmica de massa e de energia dentro de cada meio passo. Não localiza
continuamente as bordas da válvula nem adapta o tick mecânico externo.

Para cada lóbulo ativo, o tick externo deve satisfazer:

```text
limit = min(0.25 rad, D / 8)
max(abs(theta_next - theta_old), dt * max(abs(omega_old), abs(omega_next))) <= limit
8 * binary64_epsilon * max(abs(theta_old), abs(theta_next)) <= limit
```

O limite de velocidade nas extremidades também cobre uma reversão cuja variação líquida de ângulo é pequena.
O limite de precisão impede que um ângulo desenrolado perca a resolução exigida pelo
lóbulo. Um tick sub-resolvido falha mesmo quando as duas extremidades estão fechadas; não pode
pular em silêncio uma abertura estreita inteira. Um pico desativado não exige resolução do lóbulo.
São proteções numéricas, não uma tolerância de erro nem uma garantia para dinâmica arbitrária.

Uma falha devolve `NumericalFailure` / `numerical_failure` e não confirma nenhuma parte do
lote do chamador, inclusive ticks anteriores e entradas programadas. Reduza `step_ns` e recrie
o modelo ou a sessão; confira se os eventos ainda se alinham ao tick novo. Para ângulos iniciais
muito grandes, escolha um ângulo equivalente coerente com a fase de cada componente conectado.
Os limites existentes de cilindro e de gás também valem. Nenhum estado mutável oculto de came é acrescentado;
a posição do virabrequim e as entradas de pico já participam de instantâneos, hashes e ramificações.

Os casos de referência suaves e sem parede mostram convergência de segunda ordem. As temperaturas de parede
permanecem fixas ao longo do tick externo, então modelos acoplados à parede permanecem de primeira ordem. O
limitador de escoamento perto do equilíbrio já existente pode reduzir a ordem local. Conservação e replay não
demonstram, por si, precisão temporal.

## Evidência e compatibilidade

As verificações incluem valores analíticos da envoltória, ciclos explícitos, retorno de fase, aceleração,
reversão, um virabrequim parado, picos desativados, cruzamentos de lóbulo inteiro não resolvidos, cancelamento,
rollback do lote inteiro, ramificações independentes e avanço e instantâneos sem alocação.

Um teste de descarga de vaso fixo integra de forma independente a exposição em seno ao quadrado e usa
a solução de forma fechada da descarga adiabática bloqueada. Os casos direto e inverso
convergem sob refinamento de tick. Um teste separado de cilindro móvel integra massa, energia
interna, movimento do virabrequim e uma restrição dependente do ângulo com EDOs RK4
escritas de forma independente, cruzando os dois limites do lóbulo. O refinamento da referência estabelece a própria precisão
antes de comparar os resultados do Core. Veja a [validação](VALIDATION.pt-BR.md) para os limiares.

Só modelos temporizados acrescentam a etiqueta 6 de impressão digital, os IDs de componente e de virabrequim de destino e os
parâmetros normalizados do perfil. Modelos sem temporização conservam as impressões digitais e o avanço anteriores. O asset v5
acrescenta registros de temporização limitados e conserva os leitores v1–v4; fixtures anteriores autênticos conferem
impressões digitais e replay atualizado. Veja o [layout do asset](ASSET_FORMAT.pt-BR.md).

O [laboratório de cilindro temporizado pelo virabrequim](../assets/labs/crank-timed-cylinder.power.json)
motoreia uma câmara sintética por ciclos repetidos de 720 graus, com perfis de admissão e de escape.
Duas mudanças de torque programadas variam a velocidade do virabrequim; o comando de válvulas em si não tem
programação no tempo. JSON/CLI, MCP e a reprodução do asset coincidem nos 63 limites de relatório.
A compilação exporta `CrankTimedCylinder.powerasset`; o Studio anima marcadores esquemáticos
a partir dos canais de abertura efetiva. A execução de Editor/Play/IL2CPP continua pendente.

O [exemplo de reator de combustão interna do Cantera](https://cantera.org/stable/examples/python/reactors/ic_engine.html)
é uma referência conceitual para o controle de portas por ângulo de virabrequim. As hipóteses de velocidade fixa,
a lei de válvula e os parâmetros de exemplo não são adotados como calibração nem como verificação do
solver do Power!. Esta implementação usa o acoplamento conservativo de virabrequim do projeto
e a lei de bocal bidirecional. A [combustão premisturada](PREMIXED_COMBUSTION.pt-BR.md) separada
agora acrescenta a contabilidade de combustível e de energia química. Todos os parâmetros de amostra permanecem `unverified`;
o comportamento completo do motor e a calibração medida de veículo continuam em aberto.
