# Alimentação hidráulica acionada por eixo

[English](HYDRAULIC_PUMP.md) · [简体中文](HYDRAULIC_PUMP.zh-CN.md) · [Français](HYDRAULIC_PUMP.fr.md) · [Русский](HYDRAULIC_PUMP.ru.md) · [日本語](HYDRAULIC_PUMP.ja.md) · [한국어](HYDRAULIC_PUMP.ko.md) · [Deutsch](HYDRAULIC_PUMP.de.md) · [Español](HYDRAULIC_PUMP.es.md) · [Italiano](HYDRAULIC_PUMP.it.md) · **Português**

O grafo gerenciado suporta uma bomba de cilindrada ideal reversível e um alívio de pressão unidirecional quase estacionário. O [laboratório de bomba em combustão](../assets/labs/fired-pump.power.json) liga o virabrequim a uma linha de alimentação flexível, válvulas de troca e embreagens operadas por pressão. Os parâmetros são sintéticos e `unverified`.

## Equações e potência

A cilindrada `D > 0` está em m³/rad. Velocidade positiva do eixo entrega volume de referência da entrada para a saída:

```
Q = D * omega
shaft reaction = -D * (p_out - p_in)
shaft-to-fluid power = Q * (p_out - p_in) = -shaft reaction * omega
```

Escoamento reverso e motorização hidráulica são permitidos. Não há válvula de retenção, vazamento, atrito nem mapa de eficiência inferidos. A inércia pertence ao nó de eixo explícito. Uma entrada finita perde exatamente o volume entregue à saída. Uma entrada de reservatório contribui `p_in * Q` ao trabalho hidráulico externo; o trabalho de eixo para fluido é uma transferência interna e não é somado ao trabalho de fonte global.

O alívio usa uma característica linear explícita de excesso de pressão:

```
Q_A_to_B = G * max(p_A - p_B - p_crack, 0)
heat = Q_A_to_B * (p_A - p_B)
```

`G >= 0` tem unidades m³/(s·Pa), e `p_crack >= 0` é uma pressão diferencial. Abaixo do limiar ele veda exatamente. Escoamento finito exige sobrepressão; a pressão nunca é grampeada no ajuste. Esta é uma aproximação constitutiva, não mecânica de carretel nem uma curva ajustada de área de abertura da válvula. A queda de pressão completa gera calor, inclusive a parte da pressão de abertura.

As equações da bomba ideal seguem o limite sem perdas da [descrição da bomba de cilindrada fixa da MathWorks](https://www.mathworks.com/help/hydro/ref/fixeddisplacementpumpil.html). O comportamento de limiar é consistente com a [descrição da válvula de alívio de pressão](https://www.mathworks.com/help/hydro/ref/pressurereliefvalveil.html); a lei linear de excesso de pressão do Power! é uma escolha de modelagem explícita e mais simples. Essas referências fornecem equações e escopo, não medições de parâmetro OEM nem código-fonte.

## Contratos compartilhados

`hydraulic_pump` exige um `node_a` rotacional, uma saída hidráulica `node_b` e `parameters.inlet_node` (zero seleciona o reservatório). A entrada precisa diferir da saída. Os parâmetros incluem `displacement` positivo em `m3_rad`, mais `reservoir_pressure` explícita em `pa` ou `bar` somente quando a entrada é zero. Não tem entrada, sumidouro de calor nem `node_c` planetário.

`hydraulic_relief` usa `node_a` hidráulico, `node_b` hidráulico opcional (zero/omitido seleciona um reservatório), `heat_node` térmico opcional e os parâmetros `coefficient`, `cracking_pressure` e `reservoir_pressure` só de reservatório. Não tem entrada de abertura. Os cronogramas de válvula continuam usando restrições controladas separadas.

As saídas da bomba são a média do último tick de `volume_flow`, a reação de eixo `torque`, `hydraulic_power` com sinal e `hydraulic_work` acumulado com sinal. Os históricos iniciais são zero; mudanças de entrada deixam as médias aceitas inalteradas. O `hydraulic_work` global continua sendo trabalho externo de reservatório. As saídas do alívio reutilizam escoamento de restrição, potência média de calor e calor de fluido acumulado. Todas as entradas, históricos e termos de compensação participam de ramificações, hashes, cancelamento e rollback do lote inteiro. O asset v11 retém as definições novas e todos os leitores v1–v10. O agente 0.13.0 anuncia `shaft_driven_hydraulics` e `fired-pump`.

## Evidência numérica e limites

A velocidade da bomba, as pressões das câmaras, as velocidades de porta do conversor e o trabalho do cilindro compartilham um sistema de Newton, usando respostas mecânicas projetadas nas engrenagens. As capacidades da embreagem de pressão são atualizadas dentro da iteração limitada de restrição. Transferências de fluido aceitas atualizam as duas portas e o livro de volume de referência. O caminho hidráulico independente existente é conservado para modelos sem bombas, preservando os hashes de replay anteriores.

A solução de Newton conjunta permite 24 iterações e 12 reduções à metade da busca em linha. A tolerância do resíduo de pressão hidráulica é `2e-7 Pa + 64*epsilon*(|p_mid|+|p_old|)`; as tolerâncias mecânicas e de embreagem conservam os contratos existentes. Estados não finitos, pressões manométricas aceitas negativas ou orçamentos de solver esgotados rejeitam a chamada completa. Reduza `step_ns` e inspecione escalas de pressão, flexibilidade, cilindrada, inércia e embreagem antes de tentar de novo. Não há grampo de cavitação.

Os testes cobrem oscilação analítica eixo/flexibilidade e refinamento de segunda ordem, conservação de entrada fechada, motorização reversa, reações de bomba engrenada, decaimento analítico do alívio, uma carga de eixo em regime regulada e uma solução analítica independente de embreagem deslizante dependente da pressão. Captura, ramos, cancelamento, falha tardia, nova tentativa e avanço sem alocação são conferidos. O replay portátil/MCP compara todas as 89 fronteiras de `fired-pump`; contratos malformados e rebaixamentos de versão são rejeitados.

No experimento em combustão de 0.8 s, o eixo entrega 53.94250162 J ao fluido, o trabalho hidráulico externo é zero e o alívio dissipa 45.02640514 J. A energia hidráulica inicial é explicitamente 3 J. A pressão final de linha é 1.06972624 MPa, a velocidade virabrequim/turbina 69.75553569 rad/s e a velocidade da carga 6.64338435 rad/s. O resíduo de energia total é cerca de `1.07e-9 J`; o resíduo de volume de referência é `3.05e-20 m³`. Impressão digital `d0bd8f29a706fd89`, hash final `572150ab5d66a2f6`.

Mapas de perda medidos, controle de cilindrada, dinâmica de bateria e de controle de tensão, curso/inércia de carretel e de pistão, acumuladores de gás, cavitação, propriedades dependentes da temperatura e coordenação ECU/TCU continuam em aberto. Este ponto de controle não estabelece DCT/AT completa, desempenho de veículo calibrado nem aceitação de Unity Editor/Player.

## Vazamento explícito, atrito de eixo e alimentação elétrica

`HydraulicPumpAssembly` fornece uma redução reutilizável de bomba com coeficientes constantes. Aceita cilindrada D em m³/rad, condutância de vazamento G em m³/(s·Pa) e atrito viscoso de eixo B em N·m·s/rad. D precisa ser positivo; G e B precisam ser não negativos e finitos. Nenhuma eficiência nominal nem propriedade do óleo é inferida.

Para a pressão diferencial `dp = p_out - p_in`:

```text
net inlet-to-outlet flow = D*omega - G*dp
shaft reaction          = -D*dp - B*omega
leakage heat power      = G*dp^2
shaft friction heat     = B*omega^2
absorbed shaft power    = net fluid power + leakage heat + shaft friction heat
```

Os sinais suportam bombeamento e motorização hidráulica em qualquer sentido, assim como vazamento através de uma bomba parada. O vazamento continua um caminho passivo da saída para a entrada mesmo quando excede o escoamento de cilindrada. A redução de vazamento de condutância constante segue a descrição analítica de perdas na [referência de bomba da MathWorks](https://www.mathworks.com/help/hydro/ref/fixeddisplacementpumpil.html). O arrasto viscoso linear é uma escolha constitutiva explícita do Power!; não é o modelo de atrito dependente da pressão dessa referência nem um mapa de eficiência OEM.

`TryEvaluate` devolve o escoamento líquido instantâneo, a reação total de eixo, a potência com sinal de eixo/fluido e as duas potências de perda não negativas. Rejeita pressões manométricas negativas, entradas não finitas e overflow sem devolver uma reação parcial.

`CreateComponents` devolve uma lista imutável com IDs explícitos e distintos para uma `hydraulic_pump` ideal, uma `hydraulic_resistance` de abertura fixa da saída para a entrada e um `shaft` de rigidez zero do eixo da bomba ao solo. Especifique um sumidouro térmico ou deixe as perdas entrarem na rejeição externa de calor. O compilador do modelo confere portas, domínios, unidades e IDs globais. Os componentes ordinários conservam a solução de ponto médio acoplada, as transações, os canais, o esquema JSON e o asset v11; não há estado oculto de conjunto nem formato novo. Os canais da bomba descrevem o ramo ideal. Subtraia o escoamento de vazamento para obter a entrega do conjunto; inclua o arrasto de eixo ao interpretar a carga total do eixo. Não conte o trabalho da bomba ideal ao mesmo tempo como trabalho de fonte externo e como transferência interna.

`fired-pump-losses` liga vazamento e arrasto à transmissão em combustão existente. Um nó térmico separado da bomba recebe as duas perdas. O limite sem perdas reproduz todos os observáveis compartilhados de `fired-pump` dentro da tolerância física. G e B constantes são entradas de pesquisa e permanecem `unverified`.

`electric-pump` liga um motor CC RL de 12 V a um eixo de bomba separado, com força contraeletromotriz, indutância, torque e calor no cobre explícitos. Uma linha de alimentação flexível, alívio e válvulas programadas de enchimento/drenagem acionam uma embreagem entre um eixo acionado e uma carga. Mudanças de tensão e eventos de válvula usam ticks exatos. A bomba não tem ligação com o virabrequim e o trabalho hidráulico externo é zero. O trabalho elétrico entra no trabalho de fonte global; o eixo acionado e o torque de carga são fronteiras de potência externa separadas. Tensão e comandos de válvula prescritos não implementam uma bateria, uma ECU/TCU nem um regulador em malha fechada.

Movimento analítico amortecido de eixo/pressão e uma EDO de três estados, motor RL/eixo/pressão, integrada de forma independente conferem refinamento suave de segunda ordem. Entradas fechadas, trabalho de reservatório, operação com sinal, perdas passivas, encaminhamento de calor, alocações zero, ramos, cancelamento e rollback de falha tardia são conferidos contra os dois assemblies do Core. JSON, assets portáteis e MCP comparam todas as 89 fronteiras de perda em combustão e as 106 do relatório elétrico. Os testes preparados de importação/Play do Unity exigem execução separada do Editor.

<a id="sampled-pressure-regulation"></a>
## Regulação de pressão amostrada

`pressure_controller` lê um nó de pressão manométrica hidráulica e é dono de um canal existente de tensão de motor CC. É um controlador PI discreto com ganho proporcional explícito em `v_pa`, ganho integral em `v_pa_s`, limites de tensão e tensão integral inicial. A entrada de ponto de ajuste tem unidades de pressão. O `sample_period_ns` inteiro vai de 1 ns a 1 s e precisa ser um múltiplo exato do tick do modelo. Ganhos e pontos de ajuste de pressão são não negativos; os limites de tensão são finitos e estritamente crescentes. Nenhum ajuste é inferido.

```text
error = setpoint - sampled_pressure
I_candidate = I + Ki * sample_period_s * error
raw_voltage = Kp * error + I_candidate
if raw_voltage exceeds a limit and the integral increment pushes farther outside:
    retain the preceding integral state
command = clamp(Kp * error + accepted_integral, minimum_voltage, maximum_voltage)
```

A integração condicional é a estratégia de anti-windup por grampeamento descrita pela [referência de controle da MathWorks](https://www.mathworks.com/help/simulink/slref/anti-windup-control-using-a-pid-controller.html). A transição discreta precisa do Power! acima é o modelo declarado, não código de implementação copiado nem evidência de ajuste OEM. A saturação sozinha não estabelece rastreamento: um alvo inalcançável pode executar e reproduzir com sucesso enquanto reprova KPIs.

As amostras ocorrem no tempo zero e em múltiplos absolutos do período configurado. A primeira amostra preserva a integral inicial fornecida de forma explícita; as amostras seguintes usam o período. O comando é retido entre amostras. Eventos de tick exato são aplicados antes de uma amostra no mesmo tick. Um evento no extremo de uma chamada atualiza o ponto de ajuste antes do instantâneo; a amostragem nesse extremo só acontece quando o próximo tick físico começa. Intervalos internos de captura/reversão da embreagem não disparam atualizações extras do controlador.

O compilador confere que o alvo é uma entrada de tensão de motor CC e tem exatamente um dono, que o sensor é hidráulico e que a tensão inicial do motor está nos limites. O canal de tensão possuído permanece na definição do componente, mas está ausente da lista de entradas externas. Escritas diretas e substituições programadas de tensão são rejeitadas; mude a entrada `pressure_setpoint` do controlador. Os outros canais de motor, bomba e válvula conservam a semântica existente. Vários laços independentes podem compartilhar um sensor de pressão.

Os canais observáveis são `sampled_pressure`, `pressure_error`, `integral_voltage` e `command_voltage`. Os históricos de pressão/erro começam em zero; o comando inicial é a tensão configurada do motor, e a integral inicial é explícita. Os históricos descrevem a última amostra, e não um erro de pressão recalculado continuamente. Os quatro estados do controlador e a entrada de motor retida participam de hashes, ramificações e rollback completo do lote. A amostragem e o avanço bem-sucedido não alocam memória gerenciada depois do aquecimento. Aritmética PI não finita rejeita a chamada inteira; inspecione escalas de ganho, ponto de ajuste e integral.

O controlador não acrescenta energia armazenada física nem fronteira de potência. O comando dele muda a fronteira de tensão do motor existente, cuja corrente, trabalho e calor no cobre permanecem na solução acoplada e no livro de conservação. Modelos sem controladores conservam as impressões digitais e o avanço. Modelos controlados acrescentam a etiqueta de impressão digital 13. O asset v12 retém a definição completa do controlador; um fixture autêntico de bomba v11 conserva o digest original, a impressão digital e o replay no mesmo runtime depois da atualização.

`pressure-regulated-pump` usa um controlador de 5 ms e ticks físicos de 100 µs, com perturbações programadas de enchimento/drenagem da embreagem e alvos de 300/350/200 kPa. Ganhos, limites do atuador e todos os outros parâmetros permanecem `unverified`. Tem 757 fronteiras coincidentes de relatório JSON, asset e MCP. Os testes comparam um controlador amostrado separado com planta RK4, conferem refinamento do tick físico com período de controlador fixo, regras exatas de relógio/extremo, recuperação da saturação, diagnósticos de unidade/posse, rollback do estado do controlador, ramos, alocações zero e o livro completo de trabalho elétrico/hidráulico.

Isto fornece um laço de realimentação de pressão. Dinâmica de bateria e de PWM/laço de corrente, filtragem/atraso/quantização de sensor, dinâmica de válvula/carretel/pistão, coordenação ECU/TCU, DCT/AT completa, falhas e calibração medida continuam trabalho separado e inacabado.

<a id="finite-battery-supply-and-duty-regulation"></a>
## Alimentação por bateria finita e regulação de duty

Um nó `battery` possui dois estados: fração de carga z e tensão de polarização v_p. O armazenamento é a capacidade de carga explícita Q em C ou Ah (1 Ah = 3600 C), o estado inicial é o SOC em `fraction` e a posição é a tensão inicial de polarização em V. O registro da bateria fornece OCV de vazio/pleno, resistência série R0, resistência de polarização Rp, capacitância Cp e um sumidouro térmico ou rejeição externa de calor. A OCV é afim no SOC:

```text
E(z)       = V_empty + (V_full - V_empty)*z
z'         = -I_battery / Q
v_p'       = I_battery / Cp - v_p/(Rp*Cp)
V_bus      = E(z) - v_p - R0*I_battery
U_chemical = Q*(V_empty*z + (V_full - V_empty)*z^2/2)
U_polar    = Cp*v_p^2/2
heat power = R0*I_battery^2 + v_p^2/Rp
```

Corrente positiva descarrega; corrente negativa carrega. A topologia segue a [descrição do circuito equivalente de bateria](https://www.mathworks.com/help/simscape-battery/ref/batteryequivalentcircuit.html). A OCV afim e os parâmetros constantes são reduções explícitas do Power!, e não tabelas de temperatura/envelhecimento, química medida, perda de capacidade nem um BMS. O SOC permanece em [0,1]. Exceder o inventário de carga ou obter tensão de barramento negativa rejeita o lote inteiro; não há grampo silencioso nem reserva inventada. Encurte o lote, pare a descarga/carga ou forneça outras condições iniciais declaradas.

`battery_motor` liga um eixo rotacional a um barramento de bateria e conserva resistência, indutância, coeficiente de torque/força contraeletromotriz e corrente inicial explícitos do motor. A entrada de duty bidirecional médio fica em [-1,1]: a tensão do motor é o duty vezes a tensão do barramento, e a corrente do lado da bateria é o duty vezes a corrente do motor. Essa transferência de potência é interna e não é somada a `source_work`. Tanto a energia indutiva do motor quanto a energia de polarização/química da bateria participam da energia armazenada total. Calor no cobre, série e de polarização seguem para os sumidouros explícitos. Este é um conversor ideal em valor médio, não comutação PWM, perdas do conversor, contatores nem um laço de controle de corrente.

`resistive_load` fornece uma resistência positiva explícita, entrada de abertura opcional em [0,1] e sumidouro de calor. A abertura escala a condutância; abertura zero desliga exatamente. Para a condutância total de carga G e a corrente de barramento do lado do motor I_m:

```text
V_bus     = (E(z) - v_p - R0*I_m)/(1 + R0*G)
I_battery = I_m + G*V_bus
load heat = opening*V_bus^2/R_load
```

A resistência compartilhada da bateria acopla todos os consumidores. A matriz de ponto médio acoplada inclui carga, polarização, corrente do motor e respostas mecânicas. Fatores pertencentes à simulação são atualizados quando duties, aberturas de acessório ou durações de intervalo interno mudam. Respostas de engrenagem, cilindro, conversor e embreagem usam esses mesmos fatores. Modelos antigos sem alimentação conservam o caminho de solver e as impressões digitais anteriores. A energia química afim e a energia RC são quadráticas, então as transferências elétricas de ponto médio têm verificações de conservação independentes. A mesma física suporta regeneração do motor.

`pressure_duty_controller` usa a transição existente de PI/grampeamento em relógio inteiro, com ganhos em `fraction_pa` e `fraction_pa_s`, limites explícitos de duty dentro de [-1,1] e um duty integral inicial. É dono de um canal de duty de `battery_motor`. Os agentes mudam `pressure_setpoint`; substituições diretas de duty devolvem `controlled_input`. Leia `sampled_pressure`, `pressure_error`, `integral_duty` e `command_duty`. Duty retido, carga, polarização e memória de controle compartilham instantâneos, ramificações, cancelamento e rollback completo. A amostragem e o avanço bem-sucedido continuam sem alocação.

`battery-regulated-pump` combina alimentação finita de bateria, pulsos de carga de acessório e um regulador de duty de 5 ms com o laboratório da embreagem de pressão. A capacidade de 50 C é um inventário sintético pequeno de teste, não uma medição de bateria de veículo. Em 15 s o SOC cai de 0.8 para cerca de 0.627, enquanto a pressão termina em cerca de 200.828 kPa para um alvo de 200 kPa. Todas as 761 fronteiras JSON/asset/MCP concordam. Os testes conferem à parte o relaxamento RC analítico, o inventário da carga resistiva, a integração RK4 independente de motor/circuito, o refinamento do tick físico, duty com sinal e regeneração, equivalência de enrolamentos em paralelo, acoplamento engrenagem/embreagem/bomba, rollback de esgotamento tardio, ramos, cancelamento e alocações zero.

BMS/química/envelhecimento da bateria e realimentação de temperatura, falha/contatores, controle PWM/corrente, dinâmica de sensor, mecânica de atuador, ECU/TCU completa e calibração continuam em aberto. Os parâmetros da bateria e todas as entradas de laboratório permanecem `unverified`.
