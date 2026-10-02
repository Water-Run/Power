# Acionamento eletromagnético da agulha e realimentação de dose amostrada

[English](NEEDLE_ACTUATION.md) · [简体中文](NEEDLE_ACTUATION.zh-CN.md) · [Français](NEEDLE_ACTUATION.fr.md) · [Русский](NEEDLE_ACTUATION.ru.md) · [日本語](NEEDLE_ACTUATION.ja.md) · [한국어](NEEDLE_ACTUATION.ko.md) · [Deutsch](NEEDLE_ACTUATION.de.md) · [Español](NEEDLE_ACTUATION.es.md) · [Italiano](NEEDLE_ACTUATION.it.md) · **Português**

Um injetor líquido atuado lê o levantamento de uma agulha de translação, em vez de
fechar uma comporta de massa ideal na dose solicitada. Um solenoide dependente da posição,
massa explícita da agulha, mola e amortecimento de retorno e batentes elásticos de curso fornecem o
movimento. Um driver amostrado é dono da tensão da bobina e interrompe o comando quando a janela
do ciclo fecha ou a entrega medida atinge o pedido travado.

O decaimento da corrente, o atraso mecânico de fechamento e o ricochete no assento podem continuar a entrega
depois desse comando. O combustível real permanece no livro de fonte, filme e gás; a dose
solicitada é um alvo de controle, não um corte físico imposto. É um atuador de pesquisa
e uma realimentação simples liga/desliga. Mapas magnéticos não lineares, saturação,
perdas por histerese e por correntes de Foucault, resistência dependente da temperatura, comutação e flyback,
alimentação por bateria, força axial de fluido e injeção calibrada continuam em aberto.

## Energia magnética e mecânica recíprocas

`solenoid` usa uma resistência de enrolamento constante fornecida e indutância linear:

```text
L(x) = L_reference + gradient (x - x_reference) > 0
lambda = L(x) i
W_magnetic = lambda^2 / (2 L(x))
d(lambda)/dt = V - R i
F_magnetic = gradient i^2 / 2
```

Levantamento positivo aumenta a indutância, e a força magnética age nesse sentido.
As duas polaridades de corrente atraem a armadura. A força segue a coenergia
magnética, como descrito pelo [guia de força de relutância do Modelica](https://doc.modelica.org/Modelica%204.0.0/Resources/helpWSM/Modelica/Modelica.Magnetic.FluxTubes.UsersGuide.ReluctanceForceCalculation.html)
e pelas [equações de solenoide do Simscape](https://www.mathworks.com/help/simscape-electrical/ref/solenoid.html).
O Power! usa a própria lei constitutiva reduzida e a própria integração; nenhuma dependência de Modelica ou
Simscape é introduzida. A indutância deve permanecer positiva em todas as
posições aceitas e especulativas. O modelo não trunca indutância negativa
nem substitui medições magnéticas ausentes por um mapa calibrado.

O estado magnético é o fluxo concatenado. Para um intervalo `h`, indutâncias nas extremidades
`L0,L1` e tensão mantida, um gradiente discreto simétrico dá:

```text
A = (1/L0 + 1/L1) / 4
lambda1 = ((1 - h R A) lambda0 + h V) / (1 + h R A)
i_bar = A (lambda0 + lambda1)
F_bar = gradient (lambda0^2 + lambda1^2) / (4 L0 L1)
W_supply = h V i_bar
Q_copper = h R i_bar^2
delta W_magnetic = W_supply - Q_copper - F_bar (x1 - x0)
```

O fluxo é eliminado de forma analítica para uma posição de extremidade proposta. A força restante
e a derivada analítica em relação à posição entram na mesma solução mecânica não linear
de cilindros, conversores, pistões hidráulicos e embreagens especulativas.
O movimento aceito confirma fluxo, trabalho elétrico, calor no cobre e força média uma vez.
O calor no cobre entra no nó térmico declarado ou no calor rejeitado externo; a energia armazenada
magnética e a mecânica permanecem separadas.

A integração simultânea independente de EDOs verifica refinamento suave de segunda ordem.
O limite RL estacionário também tem uma referência analítica de corrente. O avanço
conjugado em energia não estabelece, por si, movimento preciso num passo de tempo grosso;
constantes de tempo elétricas, percurso do curso e eventos de contato ainda precisam de resolução.

## Massa da agulha, mola e batentes elásticos

A agulha é um nó de translação comum, com kg, m e m/s. Uma `linear_spring` comum
fornece pré-carga e amortecimento, com encaminhamento explícito de calor.
`travel_stop` acrescenta energia unilateral reversível nos limites nominais de curso:

```text
E_stop = K/2 [max(0,x_min-x)^2 + max(0,x-x_max)^2]
```

A reação discreta é o negativo do gradiente de energia entre as extremidades aceitas.
A penetração armazena energia, em vez de prender a posição ao batente. A derivada analítica
compartilha a solução mecânica; o percurso do curso nominal por intervalo limita-se a
um quarto do vão. Um deslizante tem um único dono de batente, inclusive os batentes já
possuídos por um pistão hidráulico. Coordenadas compartilhadas de hidráulica e de solenoide continuam
possíveis, com cada força contribuindo para a mesma coordenada.

O ricochete no assento é físico dentro desta redução elástica. Abertura zero num
instantâneo não prova fluxo zero ao longo de um intervalo posterior. Amortecimento de contato,
atrito de vedação, restituição de impacto e comportamento medido de assento e de agulha continuam em aberto.

## Abertura física e entrega

O `parameters.needle` opcional em `liquid_fuel_injector` contém um `needle_node` de translação
mais `closed_position` e `full_open_position` em m/mm. A abertura real
é a razão linear limitada de levantamento:

```text
opening = clamp((x - x_closed)/(x_full_open - x_closed), 0, 1)
```

A lei passiva de [trilho líquido e bocal](LIQUID_FUEL_INJECTION.pt-BR.md) integra a carga de pressão
com essa abertura efetiva. Conserva o inventário finito e os limites de energia de pressão,
mas não limita a entrega física à dose solicitada nem apaga o escoamento
quando a janela do virabrequim fecha ou inverte. Uma agulha aberta pode admitir combustível mesmo com
virabrequim parado ou dose solicitada zero. A janela do virabrequim ainda trava o histórico de alvo
para a realimentação; a entrega fora de uma janela observada nova continua fazendo parte
dos históricos do último ciclo observado e do total.

Sem `needle`, o caminho anterior do injetor ideal limitado por cota é conservado, com
impressões digitais de modelo e replay inalterados. Modelos com agulha declaram a
fidelidade diferente. A agulha está equilibrada em pressão neste incremento; nenhuma força axial
de pressão ou de jato é inferida. O receptor existente de volume líquido desprezível
exporta de forma explícita o trabalho de pressão do deslocamento.

## Driver de relógio inteiro e posse das entradas

`needle_driver` nomeia um injetor atuado, o solenoide dele e o mesmo virabrequim de
temporização. Exige um `sample_period_ns` positivo explícito, alinhado aos ticks físicos
e não maior que um segundo, e uma `drive_voltage` positiva em V. A
bobina possuída começa em tensão zero. Em cada amostra devida, o driver registra a
dose travada e a massa realmente entregue, e então mantém a tensão de acionamento enquanto a janela
para a frente ainda tem entrega de alvo restante; caso contrário, mantém tensão zero.

O driver é dono do canal de tensão do solenoide. Os agentes escrevem o pedido em `kg`
do injetor; escritas diretas de tensão devolvem `controlled_input`, identificam o canal de
comando gravável e preservam estado e revisão. Escritas iniciais e de evento não
avançam o histórico de controle. A fase de amostragem segue o tempo inteiro de simulação. Este
driver não implementa regulação de corrente de pico e de manutenção, compensação preditiva de fechamento,
PWM/flyback nem comportamento completo de ECU/TCU.

## Definições, canais e transações

| Componente | Parâmetros e portas |
|---|---|
| `solenoid` | Nó A de translação; entrada em V; resistência não negativa em ohm, indutância de referência positiva em H e gradiente em H/m (`h_m`), posição de referência em m/mm, corrente inicial em A; sumidouro térmico opcional |
| `travel_stop` | Nó A de translação; limites crescentes em m/mm e rigidez positiva em N/m |
| `needle_driver` | Nó A rotacional de temporização; IDs estáveis de injetor e de solenoide, período de amostra inteiro e nível de acionamento em V |

As definições rejeitam quantidades não relacionadas, unidades ou domínios errados, indutância inicial
inválida, posse duplicada de batente ou de tensão, agulha, bobina ou virabrequim incompatíveis e
períodos de amostra desalinhados. Os clientes do Core usam `SolenoidCoil`, `StrokeStop`,
`NeedleDrive`, `InjectorNeedleDefinition` e as leis magnéticas e de contato independentes.

As saídas do solenoide expõem `current` instantânea, `force` média discreta do último tick,
`internal_energy` magnética, `copper_heat` acumulado e `source_work` elétrico.
As saídas do batente expõem energia elástica e reação instantânea. As saídas do driver
expõem `command_voltage` mantida e a dose solicitada e entregue da última amostra. A `opening`
do injetor é a abertura real de posição, com a entrega média real do último tick.
Todos os IDs e unidades são descobríveis pela validação e pela criação da sessão.

Quatro entradas de estado reportadas por solenoide e três por driver entram no orçamento
de estado limitado. Fluxo, força média, calor e trabalho compensados, estado de controle amostrado,
entradas mantidas, agulha e batente, e todos os históricos de fonte e de fase são copiados, hasheados e sofrem rollback com
a simulação completa. O avanço ativo bem-sucedido e os instantâneos não alocam
memória gerenciada. Cancelamento, lotes que falham e ramificações independentes preservam
juntos os históricos elétrico, mecânico, térmico e de controlador.

O asset v20 acrescenta tabelas tipadas de magnético, batente, agulha e driver, conservando
os leitores v1-v19. Comprimentos e contagens limitados, digest, unidades, posse distinta e
proteção contra rebaixamento forjado são verificados. Um fixture autêntico de injeção líquida v19
conserva a impressão digital e o replay atualizado no mesmo runtime. Veja
[ASSET_FORMAT.pt-BR.md](ASSET_FORMAT.pt-BR.md).

## Experimentos e aceitação

`needle-actuated-cylinder` liga o atuador e a realimentação amostrada ao
cilindro em combustão de trilho finito e filme. O limite de 0.6 s pode reter filme líquido durante
o último transitório de fechamento e evaporação. O inventário completo de fonte, filme, gás e reação
é verificado, em vez de supor um filme seco ou a entrega exata do alvo. JSON,
assets portáteis e um servidor MCP filho real compartilham as mesmas definições e o mesmo replay.

O atuador isolado solicita 8 mg e observa entrega em excesso por decaimento de
corrente, movimento de fechamento e pequenos ricochetes posteriores no assento. Essas quantidades são resultados
de pesquisa, não temporização calibrada de injetor nem um controlador aceito de rastreamento de dose.
[VALIDATION.pt-BR.md](VALIDATION.pt-BR.md) registra a evidência numérica e os limites.
Vistas preparadas no Unity de bobina, batente, controlador e agulha em escala ainda exigem
verificação real de Editor/Play. Powertrain completo, atuação medida, forças magnéticas, eletrônicas e de fluido
refinadas, reabastecimento do trilho e ECU/TCU continuam inacabados.
