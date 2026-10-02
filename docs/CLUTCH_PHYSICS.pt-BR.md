# Física gerenciada da embreagem a seco

[English](CLUTCH_PHYSICS.md) · [简体中文](CLUTCH_PHYSICS.zh-CN.md) · [Français](CLUTCH_PHYSICS.fr.md) · [Русский](CLUTCH_PHYSICS.ru.md) · [日本語](CLUTCH_PHYSICS.ja.md) · [한국어](CLUTCH_PHYSICS.ko.md) · [Deutsch](CLUTCH_PHYSICS.de.md) · [Español](CLUTCH_PHYSICS.es.md) · [Italiano](CLUTCH_PHYSICS.it.md) · **Português**

`Power.Core` fornece uma lei de atrito `DryClutch` imutável e um integrador de referência `ClutchPair` para duas inércias sob torques externos e engate constantes. Ambos compilam para `net10.0` e `netstandard2.1` sem dependências de terceiros.

Esses primitivos dão uma referência independente para o [componente de grafo de embreagem](CLUTCH_NETWORK.pt-BR.md) já integrado. O grafo acopla eixos, motores e cilindros, admite várias embreagens e encaminhamento térmico, e preserva o rollback do lote inteiro através de eventos internos. JSON, CLI/MCP, o asset v8 e o Studio consomem essa definição de grafo. O par isolado documentado aqui continua sendo uma referência de carga constante; ele mesmo não avança uma rede compilada.

## Contrato de atrito

Todas as capacidades e reações se expressam na porta A. A relação com sinal `r` usa a mesma convenção de potência do componente de eixo existente:

```text
g       = omega_A - r * omega_B                  [rad/s]
tau_B   = -r * tau_A                            [Nm]
P_heat  = -tau_A * g                            [W]
C_s     = engagement * static_capacity          [Nm]
C_k     = engagement * sliding_capacity         [Nm]
```

A fração de engate fica em `[0,1]`. A capacidade estática é não negativa e não menor que a capacidade de deslizamento. Ambas podem ser zero. Uma capacidade estática efetiva nula desengata a embreagem. Não há pressão de aperto inferida, coeficiente de atrito, geometria de discos, queda por temperatura, desgaste, arrasto nem atraso de atuador.

Com deslizamento diferente de zero, `tau_A = -sign(g) * C_k`. Com deslizamento exatamente nulo, o sistema integrador deve fornecer o torque necessário para manter nula a aceleração relativa. Se a magnitude for no máximo `C_s`, a embreagem trava nessa reação e não produz calor de atrito. Caso contrário, começa a deslizar na direção da carga desbalanceada, usando `C_k`. A igualdade no limite estático permanece travada. A lei não tem banda morta de velocidade e não transforma em silêncio uma velocidade relativa pequena numa restrição de aderência.

Essa distinção idealizada entre atrito cinético e uma reação estática restringida segue a mecânica descrita pelas referências principais: [Modelica Clutch](https://doc.modelica.org/om/Modelica.Mechanics.Rotational.Components.Clutch.html) e [MathWorks Fundamental Friction Clutch](https://www.mathworks.com/help/sdl/ref/fundamentalfrictionclutch.html). A implementação do Power! é escrita de forma independente e usa capacidades de torque explícitas; não reproduz nenhuma das duas implementações nem reivindica os modelos constitutivos mais amplos delas.

`ClutchMode` distingue `Disengaged`, `Locked`, `SlippingPositive` e `SlippingNegative`. Um modo em velocidade nula pode ser um estado de deslizamento de partida quando a carga externa supera a capacidade estática. `HeatFlowWatts` é instantâneo; seu valor nessa partida em velocidade nula é zero, embora o calor posterior seja positivo.

## Par exato de carga constante

Para duas inércias positivas `J_A`, `J_B` e torques externos constantes `T_A`, `T_B`:

```text
J_A * domega_A/dt = T_A + tau_A
J_B * domega_B/dt = T_B - r * tau_A
D                 = 1/J_A + r*r/J_B
b                 = T_A/J_A - r*T_B/J_B
required_tau_A    = -b/D
dg/dt             = b + D*tau_A
```

Cada fase de deslizamento tem aceleração constante. Se a velocidade relativa chega a zero dentro do intervalo pedido, o solver avança exatamente até `t_zero = -g / (dg/dt)` e avalia a reação estática. Em seguida integra o restante, ou travado ou deslizando na direção oposta. O forçamento constante permite no máximo uma chegada dessas, então a solução exige no máximo duas fases, sem laço de convergência nem subdivisão de tempo. Um evento exatamente no extremo do intervalo devolve o modo de reação do lado direito do evento.

A trajetória travada obedece a `omega_A = r*omega_B`, com

```text
domega_B/dt = (r*T_A + T_B) / (r*r*J_A + J_B).
```

Numa chegada calculada, uma projeção que preserva o momento remove o resíduo de arredondamento do evento em binary64. Ela usa pesos inerciais limitados, em vez de formar somas grandes ponderadas pela inércia. Uma vez travada, a restrição de velocidade é construída de forma explícita. Isso é uma correção de arredondamento num evento resolvido, não um engate instantâneo inelástico de deslizamento finito. Os avanços angulares integram cada fase de aceleração constante. O trabalho externo é `T_A*delta_theta_A + T_B*delta_theta_B`; o calor de atrito é a integral de `-tau_A*g`. O resultado inclui o impulso de torque com sinal em A e a variação de energia cinética, verificável de forma independente. O resíduo de energia é `external_work - heat - delta_kinetic`.

O par aceita qualquer sinal de `r` finito e diferente de zero. Seu momento generalizado `r*J_A*omega_A + J_B*omega_B` muda apenas por `r*T_A + T_B`. A conservação do momento angular ordinário vale quando `r = 1`; uma relação representa um transformador mecânico ideal cujo suporte pode reagir torque. Para um freio ao solo, construa `ClutchPair.Brake(J, friction)`. A porta B fica então com velocidade nula fixa e torque externo, e `r = 1`. Infinito não é usado como sentinela de inércia.

## API e comportamento de falha

```csharp
var pair = new ClutchPair(0.2, 0.8, new DryClutch(20, 10));
var status = pair.Advance(new ClutchPairState(100, 0),
    externalTorqueANewtonMeters: 0,
    externalTorqueBNewtonMeters: 0,
    engagement: 1,
    durationSeconds: 4,
    out var step);
```

O exemplo chega a 20 rad/s nas duas portas após 1.6 s e gera 800 J de calor. Os avanços angulares ao longo de quatro segundos são 144 rad e 64 rad. Todos os números são sintéticos, sem reivindicação de calibração de veículo.

As duas classes são imutáveis. `ClutchPairState` e `ClutchPairStep` são tipos valor. `Advance` não altera o estado de quem chama nem aloca memória. Chamadas independentes podem compartilhar o mesmo par. Não há histórico de fase retido nem relógio global de simulação; as velocidades fornecidas e as novas cargas constantes determinam o intervalo seguinte.

| Status | Significado e recuperação |
|---|---|
| `Ok` | Um resultado local finito e completo está disponível; avalie a conservação e a adequação do modelo separadamente |
| `InvalidDuration` | Forneça um intervalo finito e estritamente positivo, em segundos |
| `InvalidEngagement` | Forneça uma fração finita em `[0,1]` |
| `InvalidState` | Forneça velocidades finitas; um freio ao solo exige velocidade B igual a zero |
| `InvalidTorque` | Forneça torques externos finitos; um freio ao solo exige torque B igual a zero |
| `NumericalFailure` | O movimento derivado, o instante do evento ou a energia excede a faixa binary64 suportada; inspecione unidades/escalas e encurte ou reformule o intervalo |

Em qualquer rejeição a saída é `default`; não há estado publicado parcialmente. Parâmetros imutáveis inválidos lançam `ArgumentException` ou suas subclasses na construção. `DryClutch.Evaluate` também rejeita entradas inválidas ou calor instantâneo em overflow. Um instante de evento que sofre underflow até zero falha, em vez de descartar em silêncio energia cinética relativa finita. Os resultados físicos continuam sujeitos ao arredondamento de ponto flutuante; entradas finitas sozinhas não garantem quantidades derivadas representáveis.

`ZeroSlipTimeSeconds` é a primeira chegada engatada em velocidade relativa nula, ou zero quando o intervalo começa ali. É nulo quando não há essa chegada, inclusive no movimento desengatado. Não implica aderência: uma carga externa grande pode causar reversão imediata. `SlippingDurationSeconds` inclui as fases de deslizamento de partida; o movimento desengatado fica de fora. `EndReaction` é instantâneo no estado final, enquanto calor, trabalho, impulso e avanços angulares são integrados ao longo do intervalo completo.

O parâmetro local em segundos não substitui o relógio fixo e limitado, em nanossegundos, de `Simulation`. A integração do grafo mantém os limites exatos de tick e de evento externos, os hashes de estado, a independência das ramificações, o cancelamento e o rollback completo de vários ticks.

## Evidência e limites

[ClutchChecks.cs](../tests/Power.Tests/ClutchChecks.cs) executa os mesmos dez grupos contra os dois assemblies de destino do Core:

- Tempo de sincronização de duas inércias em forma fechada, velocidade, avanços angulares, impulso, momento e energia cinética perdida, com engate total e parcial.
- Compartilhamento exato da carga estática, limiar de ruptura inclusivo, capacidade cinética menor, deslizamento diferente de zero sem banda morta e uma retenção estática de capacidade cinética nula.
- Reversão dentro de um intervalo e no seu extremo, além de frenagem ao solo, retenção e partida sob carga excessiva.
- Relações positivas e negativas, momento generalizado e variações de energia calculadas de forma independente. Duas mil combinações determinísticas varrem inércia, relação, velocidade, carga externa, capacidade e duração.
- Invariância de partição através de eventos híbridos sob forçamento constante. A amostragem no ponto médio de cargas senoidais variáveis converge contra integrais analíticas independentes de velocidade, ângulo e calor. Isso prova o comportamento de segunda ordem desse exemplo de amostragem de carga; o grafo tem as próprias verificações de convergência acoplada e híbrida, separadas.
- Entradas inválidas, overflow, um evento irresolvível, saída default na falha, avaliações repetidas independentes e alocação zero em 10,000 intervalos bem-sucedidos.

O par devolve o calor como energia gerada; o componente de grafo o encaminha a um nó térmico ou ao livro externo. Nenhuma das duas APIs implementa topologia DCT/AT, seleção de marcha, conversor de torque, atuadores hidráulicos, coordenação ECU/TCU, identificação de material de embreagem ou calibração medida. Esses limites continuam no [roteiro](ROADMAP.pt-BR.md).
