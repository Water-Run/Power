# Pistão de gás linear e acumulador hidráulico

[English](GAS_PISTON.md) · [简体中文](GAS_PISTON.zh-CN.md) · [Français](GAS_PISTON.fr.md) · [Русский](GAS_PISTON.ru.md) · [日本語](GAS_PISTON.ja.md) · [한국어](GAS_PISTON.ko.md) · [Deutsch](GAS_PISTON.de.md) · [Español](GAS_PISTON.es.md) · [Italiano](GAS_PISTON.it.md) · **Português**

`gas_piston` liga uma massa em translação a uma câmara de gás finita. A massa, a energia interna, a pressão e a temperatura da câmara continuam estados reais de simulação. O volume dela vem da geometria do pistão, e não de um volume de armazenamento fixo. Orifícios de gás e ligações térmicas de parede podem usar a mesma câmara.

Combinar um pistão de gás e um pistão hidráulico no mesmo nó em translação cria um separador de acumulador apoiado em gás. As duas forças de pressão atuam sobre uma massa e um deslocamento na solução conjunta. Molas de retorno, amortecimento e extremidades de curso flexíveis continuam componentes explícitos. Este é um acumulador de pistão concentrado, com flexibilidade líquida efetiva constante e gás ideal de calor específico constante. Geometria de bexiga, atrito de vedação, dissolução de gás, cavitação, desgaste e calibração OEM ficam fora dele.

## Geometria, pressão e trabalho

A área A e o volume de referência Vr são positivos. A posição de referência xr é explícita, e o sentido de compressão s é +1 ou -1:

```text
gas volume       V(x) = Vr - s*A*(x-xr)
absolute pressure p  = (gamma-1)*U/V
gas temperature   T  = U/(m*cv)
force on slider      = -s*A*(p-p_reference)
```

`p_reference` é uma pressão absoluta explícita, inclusive zero para um vácuo declarado. Ao compor um acumulador, ela fornece a referência de tanque usada pela convenção de pressão manométrica do líquido. Não é inferida da pré-carga de gás. A pressão e a temperatura iniciais do gás, a constante dos gases R e gamma vêm do nó de gás; a massa inicial dele é `p_initial*V_initial/(R*T_initial)`.

Durante o intervalo mecânico, o trabalho do gás fechado segue `U_next=U_old*(V_old/V_next)^(gamma-1)`. A força usa a pressão média que dá exatamente essa transferência discreta de energia. O trabalho da pressão de referência é `p_reference*s*A*dx` e entra no trabalho de fonte externa. Assim, a energia interna do gás mais a energia mecânica do separador equilibram o trabalho do fluido, o trabalho de referência e as perdas explícitas. A energia de pressão manométrica disponível a partir do armazenamento de gás usa `Delta U - reference_work`; a energia absoluta do gás sozinha superestima essa transferência.

Para a variação relativa de volume z, o fator de pressão é `phi=(1-(1+z)^(1-gamma))/((gamma-1)*z)`. O valor limite dele é um e a derivada limite é `-gamma/2`. A implementação usa série escalada para curso pequeno e diferenças estáveis de logaritmo/exponencial nos demais casos. O jacobiano da força é analítico. Câmaras de gás invertidas e opostas compartilham a mesma coordenada e preservam a convenção com sinal de volume/trabalho.

Escoamento e calor do gás usam a divisão simétrica existente de escoamento/trabalho/escoamento. Uma câmara fechada preserva o invariante adiabático; o tratamento explícito da temperatura de parede conserva a precisão existente de primeira ordem acoplada à parede. O escoamento de massa de gás é contabilizado com a entalpia do reservatório, em vez de tratar a massa acrescentada como isenta de energia. Nenhum ajuste politrópico nem substituição isotérmica substitui o estado de energia.

## Contratos e limites numéricos

| Parâmetro | Significado |
|---|---|
| `node_a` | Nó em translação com massa positiva |
| `node_b` | Câmara de gás com um único dono de volume móvel; omita o `storage` do nó |
| `area` | Área positiva em m2 ou mm2 |
| `reference_volume` | Volume positivo em m3 ou litros |
| `reference_position` | Posição em m ou mm nesse volume |
| `reference_pressure` | Pressão absoluta não negativa em Pa ou bar |
| `compression_direction` | +1 (orientação padrão) ou -1 |

Uma câmara de gás tem um único dono de geometria; várias câmaras distintas podem atuar sobre uma massa. Um pistão de gás não tem entrada direta nem substituição de sumidouro térmico. Use uma fonte de força explícita, um pistão hidráulico ligado, um orifício de gás ou uma ligação térmica de gás. Observe massa/energia/pressão/temperatura da câmara e volume do componente, força do deslizador e `source_work` da pressão de referência.

O volume de gás precisa permanecer positivo, inclusive ao longo do curso nominal de um pistão hidráulico compartilhado. Um intervalo mecânico aceito muda no máximo 25% do volume atual de gás. Curso grande, volume não positivo ou força não resolvida rejeita o lote inteiro. Reduza o tamanho do tick e inspecione escalas de geometria, massa, pressão e força antes de tentar de novo. Movimento e energia não são limitados. A penetração de uma extremidade flexível ainda armazena o potencial explícito do batente e continua sujeita a volume de gás positivo.

Estado físico/de controle, inventários de gás e todos os históricos compartilham cancelamento, rollback completo, hashes e ramificações independentes. O asset v16 preserva as quatro grandezas de geometria/referência e o sentido de compressão. JSON, CLI e MCP expõem as mesmas definições. Vistas de separador/câmara do Studio e testes de importação/Play estão preparados em fonte C# 9; a evidência real de Editor e Player continua separada.

## Experimento do acumulador e evidência

`gas-accumulator-pump` acrescenta uma câmara inicial de gás de 50 ml, um separador de 50 g, amortecimento viscoso explícito e extremidades flexíveis à bomba elétrica, ao desvio mecânico do carretel e à embreagem de pressão. O gás começa a 200 kPa absolutos e 300 K; a pressão de referência é 100 kPa. O separador começa com 0.1 mm de penetração de assentamento flexível, equilibrando a pré-carga contra pressão manométrica líquida nula. Todos os valores são parâmetros de pesquisa. O pulso de tensão/demanda de 3-4 s abre de forma explícita os caminhos de enchimento e de dreno; a energia de gás armazenada e o volume líquido varrido então diminuem antes de a carga retomar.

[MathWorks Gas-Charged Accumulator (IL)](https://www.mathworks.com/help/hydro/ref/gaschargedaccumulatoril.html) descreve a separação gás/líquido e o mecanismo de carga/descarga. O Power! compõe as próprias portas de gás de energia finita e mecânicas/hidráulicas, em vez de copiar um expoente politrópico fixo, padrões de parâmetro ou código de implementação.

As verificações incluem trabalho adiabático analítico e derivadas, curso fino, um transitório RK4 separado de massa/energia com refinamento suave de segunda ordem, câmaras opostas, movimento comum de gás/fluido, refinamento RK4 de parede finita, entrada de gás em volume móvel, contas independentes de energia/volume e transações completas. Câmaras fechadas e não misturadas, sem transporte nem calor, pulam a integração redundante de taxa zero depois da validação do estado. A otimização medida preserva cada valor/hash de fronteira; o avanço em regime e as leituras de instantâneo alocam zero bytes gerenciados. Limites detalhados de erro, tempos e escopo de plataforma estão em [VALIDATION.pt-BR.md](VALIDATION.pt-BR.md).
