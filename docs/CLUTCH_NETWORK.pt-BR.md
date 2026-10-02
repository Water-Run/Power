# Simulação acoplada de embreagem

[English](CLUTCH_NETWORK.md) · [简体中文](CLUTCH_NETWORK.zh-CN.md) · [Français](CLUTCH_NETWORK.fr.md) · [Русский](CLUTCH_NETWORK.ru.md) · [日本語](CLUTCH_NETWORK.ja.md) · [한국어](CLUTCH_NETWORK.ko.md) · [Deutsch](CLUTCH_NETWORK.de.md) · [Español](CLUTCH_NETWORK.es.md) · [Italiano](CLUTCH_NETWORK.it.md) · **Português**

O componente gerenciado `clutch` liga dois nós rotacionais, ou um rotor ao solo. Ele participa da solução eletromecânica/cilindro existente e encaminha o calor de atrito gerado a um nó térmico ou ao livro externo de rejeição de calor. JSON, CLI, MCP, o asset v10 e o Studio usam as mesmas definições. Isto implementa um elemento de acoplamento de transmissão; topologia DCT/AT completa, dinâmica de bomba/pistão e coordenação ECU/TCU continuam trabalho separado. A [rede hidráulica](HYDRAULIC_NETWORK.pt-BR.md) agora aciona uma variante de embreagem operada por pressão. O [conversor mapeado](CONVERTER_NETWORK.pt-BR.md) agora compartilha esta solução e usa uma embreagem paralela separada para a trava.

O [contrato físico da embreagem a seco](CLUTCH_PHYSICS.pt-BR.md) define a lei de Coulomb e uma referência exata e independente de duas inércias sob cargas constantes. O solver de grafo abaixo estende essa lei a redes acopladas. Ele não congela o torque do motor a combustão nem a corrente do motor elétrico numa entrada de sentido único para a embreagem.

## Definição e canais

```json
{
  "id": 16,
  "kind": "clutch",
  "node_a": 1,
  "node_b": 4,
  "heat_node": 5,
  "input_channel": 104,
  "initial_input": { "value": 0, "unit": "fraction" },
  "parameters": {
    "static_capacity": { "value": 100, "unit": "nm" },
    "sliding_capacity": { "value": 30, "unit": "nm" },
    "ratio": 1
  }
}
```

`node_a` é um nó rotacional. `node_b` é um nó rotacional distinto, ou omitido/zero para um freio ao solo. `ratio` é finito e diferente de zero; o freio ao solo exige relação igual a um. A capacidade estática é pelo menos a capacidade de deslizamento, e ambas são torques finitos e não negativos na porta A. `initial_input` é uma fração de engate explícita em `[0,1]`, que escala as duas capacidades. Um canal de entrada opcional muda o engate em limites exatos de tick externo. Um `heat_node` omitido/zero envia o calor ao livro externo de rejeição; um sumidouro fornecido precisa ser um nó térmico. A temperatura não altera essas capacidades.

A fábrica do Core é `ComponentDefinition.Clutch(id, a, b, staticCapacity, slidingCapacity, channel, engagement, ratio, heat)`. O descritor `Friction` contém as duas quantidades explícitas de torque. Os modelos compilados copiam os parâmetros, ordenam por ID estável e acrescentam a etiqueta de impressão digital 8 somente quando há embreagens. Cada fase de embreagem conta contra o limite existente de 64 estados. Modelos anteriores conservam as impressões digitais e os hashes de replay. O nome de fidelidade combinado é `hybrid_clutch_powertrain`, com calibração ainda `unverified`.

| Campo | Unidade | Significado |
|---|---|---|
| `slip_speed` | rad/s | `omega_A - ratio*omega_B` atual |
| `clutch_mode` | StateCode | Fase do último intervalo aceito: 0 desengatada, 1 travada, 2 deslizamento positivo, 3 deslizamento negativo |
| `torque` | Nm | Reação média em A ao longo do último tick externo completo |
| `heat_flow` | W | Potência média de atrito gerada ao longo desse tick |
| `friction_heat` | J | Calor gerado acumulado, qualquer que seja o destino |

Torque, potência e calor iniciais são zero; a fase inicial é inferida do engate e da velocidade relativa, antes de resolver uma reação de carga. Uma fase descreve o intervalo resolvido, então uma chegada exatamente no extremo ainda pode mostrar a fase de aproximação até a solução seguinte. Uma entrada de fronteira muda o estado de entrada na hora e não reescreve o histórico de saída do intervalo anterior. Isso também vale para eventos de entrada no fim de uma chamada `Step`. As médias de torque e de potência incluem cada intervalo interno aceito.

## Integração acoplada e eventos

Para `g = omega_A - r*omega_B`, os torques de porta são `tau_A = tau`, `tau_B = -r*tau`. A potência mecânica removida é `-tau*g`; essa convenção de sinal funciona com qualquer sinal de `r`. O desengate impõe torque zero. O deslizamento usa a capacidade cinética que se opõe ao deslizamento. Uma embreagem travada impõe velocidade relativa nula no ponto médio, com uma reação limitada pela capacidade estática. Isso dá trabalho travado ideal nulo, sem inserir um amortecedor artificial nem uma mola de penalidade rígida.

Cada intervalo interno usa as equações eletromecânicas implícitas de ponto médio existentes e a solução conservativa de trabalho de pressão do cilindro. As respostas de força da embreagem vêm dos mesmos fatores lineares acoplados. Uma solução de Gauss–Seidel projetada determina as reações estáticas limitadas enquanto o torque do cilindro é recalculado para as forças atuais. Restrições estáticas saturadas se liberam quando o movimento exigido excede a tolerância de velocidade. O conjunto ativo é reconsiderado se outra restrição muda uma direção de partida. Laços redundantes de embreagem são permitidos; as reações individuais podem não ser únicas. A ordem estável dos componentes escolhe uma alocação determinística, enquanto os testes conferem o movimento resultante, os limites de capacidade, o momento total e a energia.

Se um intervalo de deslizamento inverte a velocidade relativa, uma bisseção limitada localiza a fronteira observada de deslizamento nulo e reproduz o intervalo a partir de uma cópia completa do estado. O intervalo seguinte ou adere ou parte com a reação cinética oposta. Dinâmica, fatores térmicos e respostas de força do cilindro são recalculados para cada duração candidata; todos os fatores mutáveis pertencem à simulação individual. O modelo compilado permanece imutável.

A tolerância da restrição é `2e-13 + 32*epsilon*(abs(omega_A)+abs(r*omega_B))` rad/s, com `epsilon = 2.2204460492503131e-16`. A captura aceita raízes dentro de dezesseis vezes essa tolerância. Ela não projeta para longe um deslizamento finito nem descarta energia cinética finita. Trabalho de atrito negativo minúsculo, dentro do dobro da tolerância torque-vezes-velocidade do intervalo, é limitado a zero; trabalho negativo maior falha. As verificações de conservação incluem esse efeito de arredondamento. Um resíduo dentro da tolerância de raiz não pode criar um segundo evento espúrio.

A solução permite no máximo 32 intervalos internos por tick externo, 56 iterações de raiz, 256 iterações de restrição por conjunto ativo e `2*clutch_count+2` tentativas de conjunto ativo. Fatores não finitos, restrições não convergidas, eventos não resolvidos, orçamentos esgotados ou limites existentes de cilindro/gás devolvem `NumericalFailure`. O cancelamento é verificado durante o trabalho limitado de restrição e de raiz. Reduza o tick externo e inspecione escalas de inércia/relação, restrições redundantes e cronogramas de capacidade; não interprete uma chamada falha como um engate parcialmente concluído.

O calor gerado é integrado como `-duration*tau*g_mid` e depois somado à solução térmica ou ao livro externo de calor. Todo o trabalho de fonte, o transporte de gás, o histórico químico, a troca de parede e a rejeição térmica dos intervalos aceitos entram na contabilidade de energia existente. Fase da embreagem, saídas médias, calor acumulado e soma de calor compensada são copiados e hasheados com o estado físico. Uma chamada de vários ticks que falha ou é cancelada restaura o estado inicial completo, inclusive entradas programadas, fase e calor. As ramificações compartilham apenas dados do modelo compilado. O tempo externo continua sendo uma contagem inteira limitada de nanossegundos; as durações de eventos internos não introduzem ticks fracionários visíveis externamente.

O teste não linear de ruptura usa a demanda de torque média do intervalo. Ele não localiza o instante exato de tempo contínuo em que uma carga estática variável supera pela primeira vez a capacidade. Da mesma forma, o enquadramento de eventos diz respeito à trajetória discreta de ponto médio; um tick grande pode perder oscilações físicas rápidas cujos extremos escondem uma reversão. Refine o tempo em torno das transições e compare as saídas. A dinâmica eletromecânica suave conserva a precisão de ponto médio, o acoplamento térmico/parede continua de primeira ordem, e não se reivindica segunda ordem universal para todas as trajetórias de comutação.

## Laboratório e evidência

O [laboratório de embreagem em combustão](../assets/labs/fired-clutch.power.json) liga o cilindro premisturado a uma carga inercial separada e a um nó térmico da embreagem. Seis eventos em ticks exatos aplicam engate parcial/total, torque de carga, desengate e reengate. Os parâmetros são sintéticos. Ao longo de 0.6 segundos, o relatório Linux atual registra:

| Grandeza | Resultado |
|---|---|
| Velocidade final motor/carga | 68.58488546 rad/s |
| Trabalho líquido de fonte externa, inclusive carga e contrapressão do cilindro | -96.74607609 J |
| Calor de embreagem gerado | 191.55570747 J |
| Temperatura final do nó térmico da embreagem | 300.95777854 K |
| Calor de combustível liberado | 1,630.91064291 J |
| Deslizamento final | 2.84e-14 rad/s, fase travada |
| Resíduo final de energia | 1.79e-10 J |
| Impressão digital do modelo / hash de estado final | `197be44884deee90` / `28bf5335d8e35cde` |

Todas as 67 fronteiras do relatório coincidem entre tamanhos alternativos de lote, reprodução portátil e replay do servidor filho MCP real. O relatório é `artifacts/reports/fired-clutch.json`. Peça `get_example_model` com `name: "fired-clutch"`, ou execute:

```sh
dotnet src/Power.Cli/bin/Release/net10.0/Power.Cli.dll assets/labs/fired-clutch.power.json --output artifacts/reports/fired-clutch.json
```

As verificações do Core comparam engate, frenagem e reversão com `ClutchPair`, inclusive relações com sinal e os dois destinos de calor. Um motor RL travado coincide com um modelo de inércia combinada analiticamente; um cilindro de gás reagente travado também coincide com o modelo independente de inércia equivalente, inclusive pressão e consumo de combustível. Um oscilador mola/freio coincide com o movimento senoidal analítico por trechos ao longo de três reversões e de um quarto ponto de retorno que captura, com o refinamento reduzindo o erro em mais de 3.7x por metade. Laços de três embreagens exercitam restrições redundantes e engate simultâneo. Os testes também cobrem rollback completo depois de um prefixo bem-sucedido de aquecimento/captura, cancelamento, replay programado exato, posse imutável, independência de ramos e operação sem alocação, inclusive eventos internos de reversão repetidos.

O asset v10 preserva na ida e volta as capacidades explícitas e todos os canais. Contagens malformadas, registros ausentes, duplicados e de tipo errado, unidades erradas, limites inválidos e rebaixamentos forjados são rejeitados. O fixture autêntico v6 de cilindro em combustão conserva o digest, a impressão digital e o replay atualizado. Testes estritos de JSON e de agente distinguem execução bem-sucedida de KPIs aprovados. Veja [VALIDATION.pt-BR.md](VALIDATION.pt-BR.md).

A compilação exporta `FiredClutch.powerasset`. O Studio prepara dois discos esquemáticos de embreagem, cores de fase e saída de fase nomeada, junto com controles de engate e canais de calor. Os testes de importação e do ciclo de vida de Play estão preparados. A evidência real de Unity Editor, renderização, Play Mode e IL2CPP continua pendente; verificações de assembly Standard hospedadas em .NET não a substituem.

## Acoplamento permanente de engrenagem

As [restrições ideais de engrenagem e planetária](GEAR_NETWORK.pt-BR.md) agora projetam o ponto médio livre e as respostas de força de embreagem/cilindro no mesmo espaço de restrição permanente. O laboratório planetário em combustão combina um freio da coroa e uma embreagem sol/coroa com uma planetária ideal e uma redução final, reproduzindo uma subida e uma descida de marcha. Uma embreagem cuja velocidade relativa já está permanentemente restringida é rejeitada como reação independente indefinida. Os demais contratos de estado, capacidade, térmico e de evento da embreagem permanecem inalterados.
