# Troca de gás do cilindro móvel

[English](MOVING_CYLINDER.md) · [简体中文](MOVING_CYLINDER.zh-CN.md) · [Français](MOVING_CYLINDER.fr.md) · [Русский](MOVING_CYLINDER.ru.md) · [日本語](MOVING_CYLINDER.ja.md) · [한국어](MOVING_CYLINDER.ko.md) · [Deutsch](MOVING_CYLINDER.de.md) · [Español](MOVING_CYLINDER.es.md) · [Italiano](MOVING_CYLINDER.it.md) · **Português**

Um `gas_cylinder` liga um virabrequim rotacional a uma câmara de gás. Ao contrário da referência
adiabática fechada, esta câmara carrega massa e energia interna independentes, então
restrições e elos de parede podem mudar o estado enquanto a pressão aciona o virabrequim.
O componente está disponível pelo Core, JSON, CLI, MCP e assets portáteis. Não
modela inércia do pistão nem química detalhada. Componentes separados de
[combustão premisturada](PREMIXED_COMBUSTION.pt-BR.md) e de [comando por ângulo de virabrequim](VALVE_TIMING.pt-BR.md)
agora fornecem a conversão de energia do combustível e controlam as restrições conectadas.

## Contrato do modelo

```csharp
var definition = new ModelDefinition
{
    StepNanoseconds = 50_000,
    Nodes = [NodeDefinition.Rotor(1, 0.2, 60),
        NodeDefinition.CylinderGas(2, 100_000, 300),
        NodeDefinition.Thermal(3, 500, 350)],
    Components = [ComponentDefinition.GasCylinder(10, 1, 2, new()
    {
        Bore = new(86, Unit.Millimeter), Stroke = new(86, Unit.Millimeter),
        RodLength = new(143, Unit.Millimeter), Phase = new(0, Unit.Radian),
        CompressionRatio = 10, BackPressure = new(1, Unit.Bar)
    }), ComponentDefinition.GasReservoir(11, 2, 50e-6, 110_000, 300, 0.8, 100, 1),
        ComponentDefinition.GasHeatLink(12, 2, 3, 2.5)]
};
```

O nó de gás fornece pressão absoluta inicial, temperatura, R e gamma. A quantidade `Storage`
é zero/None: exatamente um componente de cilindro é dono do volume. O compilador
deriva a massa e a energia iniciais da geometria no ângulo inicial do virabrequim, inclusive
a fase. Rejeita um volume especificado de forma independente ou dois donos de cilindro para uma câmara.
Nós de gás fixos não conectados ainda exigem volume explícito positivo.

Em JSON, use `domain: "gas"` e omita `storage` para uma câmara móvel. O componente `gas_cylinder`
exige `node_a` (rotacional), `node_b` (gás) e os parâmetros `bore`, `stroke`,
`rod_length`, `phase`, `compression_ratio` e `back_pressure`. Nenhuma composição nem estado
inicial do gás é duplicado neste componente. O nó de gás expõe pressão, temperatura,
massa e energia interna; o cilindro expõe volume, deslocamento do pistão e torque
do virabrequim. Portas, restrições, limites de abertura e elos de parede usam o
[contrato da rede de gás](GAS_NETWORK.pt-BR.md) existente.

Modelos com câmaras móveis sem restrições temporizadas ou rastreamento premisturado informam a fidelidade `moving_cylinder_gas_exchange` e
acrescentam a etiqueta 5 de impressão digital do solver. Modelos lineares, de cilindro fechado e só de volume fixo
já existentes conservam as impressões digitais e o avanço. A composição permanece fixa, os nós de gás
conectados devem concordar, e todos os parâmetros permanecem `unverified`.

## Equações e acoplamento conservativo

Para um gás caloricamente perfeito de composição fixa:

```text
p = (gamma - 1) U / V(theta)
T = U / (m cv)
dm/dt = sum(inflow) - sum(outflow)
dU/dt = -p dV/dt + Q_wall + sum(mdot_in h_in) - sum(mdot_out h_out)
tau_gas = (p - p_back) dV/dtheta
```

O balanço de massa e de energia segue a primeira lei padrão de sistema aberto; veja
[as equações de volume de controle do Cantera](https://www.cantera.org/stable/reference/reactors/controlreactor.html).
Essa referência sustenta as equações, não o esquema de integração do Power! nem a validação.
O gás usa a lei de bocal compressível bidirecional existente, e não a implementação
linear de válvula de um só sentido do Cantera. O trabalho de contrapressão é trabalho de fonte externo;
a entalpia de reservatório e a troca com a parede conservam os sinais de livro existentes.

A implementação usa uma divisão simétrica de operadores para modelos com câmaras móveis:

1. Avança a troca de gás e o calor de parede por meio tick na geometria inicial do virabrequim.
2. Resolve a eletromecânica acoplada e o trabalho de pressão adiabático no tick inteiro com
   o solver de virabrequim por gradiente discreto limitado.
3. Avança a troca de gás e o calor de parede por meio tick na geometria resultante do virabrequim.
4. Aplica o calor acumulado gás-parede e as perdas eletromecânicas à solução térmica.

Durante o passo 2, a massa é fixa e `U_new = U_old (V_old/V_new)^(gamma-1)`. A pressão média do gás
e o torque vêm da diferença dividida dessa mesma variação de energia.
O virabrequim ganha o trabalho do gás menos o trabalho de contrapressão; a câmara perde exatamente o
trabalho de gás correspondente, até a precisão de ponto flutuante. `log1p`/`expm1` e a diferença
dividida analítica do volume evitam subtrair estados quase iguais em passos pequenos e
pontos mortos. Vários cilindros podem compartilhar um virabrequim ou agir por eixos acoplados.

A divisão tem convergência de segunda ordem no caso ensaiado de escoamento bloqueado suave, sem
transferência de parede. As temperaturas de parede permanecem fixas durante os dois meios passos de gás, seguidas
pela solução térmica existente: a precisão com acoplamento de parede permanece de primeira ordem. O limitador
de escoamento perto do equilíbrio também pode mudar a ordem local. A conservação não estabelece a precisão.

## Limites, falha e compatibilidade

O percurso do virabrequim limita-se a 0.25 rad por tick; a solução não linear usa no máximo 16
iterações e 10 tentativas de busca linear. Cada meio passo de gás conserva o limite de 4096 subpassos,
a variação relativa alvo de 2% e a rejeição de variação corrigida de 25%. Estados, saídas ou esgotamento
do solver inválidos ou não finitos rejeitam o lote inteiro do chamador, inclusive todos os ticks
anteriores e as entradas programadas. Reduza `step_ns` e inspecione a área de escoamento, o estado do gás, a condutância,
a velocidade e a inércia do virabrequim antes de tentar de novo. Cancelamento e ramificações conservam todo o estado de gás e de livro;
o avanço bem-sucedido e os instantâneos no buffer do chamador não alocam memória gerenciada.

O asset v4 acrescenta um registro de geometria indexado e limitado para cada cilindro de gás, preservando todos
os leitores v1/v2/v3. Nunca serializa espaços de trabalho do solver. Um fixture autêntico de volume fixo v3
verifica que introduzir geometria móvel não muda impressões digitais de gás anteriores
nem o replay. Veja o [formato de asset](ASSET_FORMAT.pt-BR.md) e a [procedência dos fixtures](../tests/Power.Tests/Fixtures/README.md).

## Experimento e evidência

O [laboratório de cilindro móvel](../assets/labs/moving-cylinder.power.json) motoreia um
cilindro com duas restrições de reservatório e uma parede térmica finita. Os oito eventos
de abertura baseados em tempo exercitam o escoamento para dentro e para fora da câmara e incluem ticks entre limites
de relatório e de apresentação. É um experimento de motoreagem não calibrado; a programação
não é uma ECU, um perfil de came, um controlador de motor de quatro tempos nem um modelo de combustão.

Os testes comparam uma câmara fechada com a implementação existente de cilindro fechado em
rotação direta e inversa e nos pontos mortos, e comparam uma câmara aberta com uma
integração RK4, escrita de forma independente, das EDOs governantes. Esta última escreve geometria,
fluxo de massa bloqueado e trabalho de pressão diretamente das equações. O refinamento de passo verifica
a precisão de escoamento suave e a de acoplamento de parede separadamente. Verificações adicionais cobrem vários
virabrequins acoplados ou compartilhados, cilindros fechados e abertos mistos, conservação, posse malformada,
normalização de unidades, falha e recuperação atômicas, ramificações, cancelamento, alocação zero,
compatibilidade portátil e todos os limites de relatório de JSON, MCP e asset.

O Unity inclui uma vista de pistão móvel, conexões de gás e testes de importação e de Play. A evidência
real de Editor, renderização, Play Mode e IL2CPP continua pendente. Veja o
[registro de validação](VALIDATION.pt-BR.md) para as verificações executadas e o [roteiro](ROADMAP.pt-BR.md)
para o trabalho restante de motor, transmissão, controle e calibração.
