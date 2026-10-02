# Predição limitada de fechamento da agulha e corte na grade de ticks

[English](CLOSURE_PREDICTION.md) · [简体中文](CLOSURE_PREDICTION.zh-CN.md) · [Français](CLOSURE_PREDICTION.fr.md) · [Русский](CLOSURE_PREDICTION.ru.md) · [日本語](CLOSURE_PREDICTION.ja.md) · [한국어](CLOSURE_PREDICTION.ko.md) · [Deutsch](CLOSURE_PREDICTION.de.md) · [Español](CLOSURE_PREDICTION.es.md) · [Italiano](CLOSURE_PREDICTION.it.md) · **Português**

O [driver físico da agulha](NEEDLE_ACTUATION.pt-BR.md) pode compensar o combustível entregue
depois que o comando de tensão termina. O `closure_prediction_ns` opcional ativa um replay
separado da planta inteira, pré-alocado. O levantamento real da agulha, o decaimento da corrente, o trabalho de pressão,
o ricochete no assento e o inventário de combustível permanecem físicos; a predição muda o instante
do comando, em vez de cortar a massa realmente entregue.

## Contrato de predição

Numa amostra devida, o preditor copia o estado atual completo. Reproduz os
ticks físicos configurados com a bobina em tensão zero, ou com um período limitado
de tensão de acionamento antes do corte. Todos os outros comandos de atuador são mantidos. Controladores
amostrados não recorrem nem mudam comandos dentro desta previsão, e eventos
externos de entrada futuros não são antecipados. Troca de gás, comportamento de virabrequim e de cilindro,
combustão, alimentação hidráulica e elétrica, contatos e intervalos de embreagem aceitos
continuam pelas equações normais da planta.

A massa adicional prevista é o aumento da entrega total desse injetor.
O estado de rascunho nunca é confirmado na simulação real. Cada candidato parte
do mesmo estado de fonte completo; o tempo real, a memória do controlador e os históricos
físicos permanecem intocados. O espaço de trabalho do solver, pertencente à simulação, é preparado de novo
para o intervalo real. `PredictNeedleClosure(driver_id, out estimate)` expõe uma
predição somente leitura de tensão zero para clientes do Core, com cancelamento e status.

O horizonte é um múltiplo inteiro de ticks físicos, cobre pelo menos dois períodos
de amostra do driver e limita-se a **4096 ticks físicos**. Zero conserva o comportamento anterior
de driver liga/desliga. A predição não pode dar a volta no relógio inteiro limitado. Previsões
que falham ou são canceladas rejeitam o lote real inteiro; uma previsão parcial não é
tratada em silêncio como uma estimativa válida.

## Decisão de fechamento programada

O driver compara a entrega do ciclo atual mais o combustível de fechamento previsto com o
pedido travado. Se o fechamento em tensão zero já atinge o alvo, corta agora.
Caso contrário, também prevê manter o acionamento até a amostra seguinte. Se esses dois
candidatos delimitam o alvo, uma bisseção inteira limitada encontra candidatos vizinhos
de corte em tick físico e escolhe a massa final projetada mais próxima.

O prazo escolhido é uma contagem regressiva de ticks físicos. Pode remover a tensão
antes da amostra seguinte do controlador. A decisão de corte fica travada para o ciclo
observado, evitando reaberturas repetidas por diferenças minúsculas de predição. Um ciclo observado
novo zera essa trava. O desligamento por janela ou por reversão pode cancelar um prazo pendente.
O combustível real continua governado pela agulha em movimento durante todo o fechamento e o ricochete.

O delimitador local de candidatos deve ser monótono dentro da tolerância numérica
declarada. Um delimitador violado devolve falha numérica com o estado do modelo e da sessão
inalterado; inspecione tensão, mecânica, amostragem e hipóteses de predição em vez
de aceitar um corte inválido. Cada candidato limita-se a 4096 ticks, e a
bisseção inteira tem no máximo doze consultas interiores mais as previsões das extremidades.

Isto é temporização liga/desliga baseada em modelo, não combustão preditiva, controle de ECU
calibrado, gestão robusta de falhas nem um mapa medido de injetor. Manter os outros
comandos e omitir eventos externos futuros são hipóteses explícitas da previsão.
Mudanças de carga, de pressão ou de ação do controlador no futuro podem mudar a entrega real.

## Horizonte e precisão física

Uma predição finita precisa incluir o combustível relevante de fechamento e de ricochete. No atuador
de pesquisa isolado, a predição de 8 ms trunca uma cauda tardia relevante; a predição de 20/30 ms
dá a mesma decisão na grade de ticks. O estudo de horizonte é conservado como
evidência, em vez de tratar uma previsão curta arbitrária como fechamento completo.

O passo de tempo físico, o período de amostra do controlador e o horizonte da previsão são controles
de precisão separados. Um horizonte mais longo não repara integração elétrica ou de contato
grossa nem um modelo constitutivo impreciso. A igualdade entre previsão e real sob
o mesmo modelo de entradas mantidas verifica a implementação, não a calibração OEM. Verificações analíticas,
de EDO independente, de conservação e de eventos na planta subjacente continuam valendo.

## Observáveis e transações

As saídas do driver com predição ativada incluem:

- `predicted_fuel_mass`: combustível adicional do candidato de fechamento escolhido, kg.
- `prediction_ticks`: a contagem configurada de replay físico.
- `driver_state`: se o corte foi travado para o ciclo observado.
- `closing_delay_ticks`: ticks físicos restantes antes da remoção programada da tensão.

A tensão mantida e o alvo e a entrega da última amostra continuam disponíveis. A quantidade
prevista inclui qualquer atraso de acionamento planejado, enquanto a consulta pública somente leitura do Core
sempre prevê o fechamento imediato em tensão zero. Essas quantidades não são transferências reais
de combustível e não entram nos livros de massa, químico ou de energia.

Cinco entradas adicionais de estado reportado por driver conservam massa e contagem da predição,
trava e ciclo do corte, e a contagem regressiva, quando a predição está ativada. O estado de replay
separado é alocado uma vez por simulação. Leituras, avanço ativo bem-sucedido e
instantâneos não alocam memória gerenciada depois do aquecimento. Cancelamento, revisões,
ramificações independentes, captura especulativa de embreagem e falha numérica tardia preservam
todos os históricos de predição, de controle e físicos. A predição desativada conserva as
impressões digitais e os hashes anteriores.

## Definições compartilhadas e evidência

O JSON aceita o opcional `needle_driver.parameters.closure_prediction_ns`. O Core usa
`NeedleDriverDefinition.ClosurePredictionNanoseconds`. As capacidades declaram limites,
hipóteses de manutenção e observáveis; `closure-compensated-cylinder` é o exemplo
compartilhado. Os pedidos de fonte em `kg` continuam graváveis, e a tensão da bobina permanece do driver.
Pedidos bem-sucedidos são distintos do rastreamento real da dose e de KPIs aprovados.

O asset v21 conserva a tabela de contagens existente e estende cada registro de driver de
32 para 40 bytes com um uint64 de horizonte. Os leitores anteriores usam predição desativada por padrão;
um fixture autêntico v20 conserva a impressão digital e o replay no mesmo runtime. A predição
ativada acrescenta a etiqueta 25 de impressão digital e o horizonte configurado. Contagens limitadas,
unidades, horizonte e alinhamento, posse do controlador e rejeição de rebaixamento são verificados.

O pedido isolado de 8 mg, o laboratório completo em combustão, o estudo de horizonte, os modelos
imutáveis, a previsão somente leitura e o fechamento manual independente, o replay completo, a alocação zero
e os lotes cancelados ou que falham são verificados em [VALIDATION.pt-BR.md](VALIDATION.pt-BR.md).
Motor, transmissão e controle completos, mapas físicos e de atuação medidos, reabastecimento do trilho,
Unity real e aceitação de veículo calibrado continuam inacabados.
