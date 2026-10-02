# Rede quase estacionária de conversor de torque

[English](CONVERTER_NETWORK.md) · [简体中文](CONVERTER_NETWORK.zh-CN.md) · [Français](CONVERTER_NETWORK.fr.md) · [Русский](CONVERTER_NETWORK.ru.md) · [日本語](CONVERTER_NETWORK.ja.md) · [한국어](CONVERTER_NETWORK.ko.md) · [Deutsch](CONVERTER_NETWORK.de.md) · [Español](CONVERTER_NETWORK.es.md) · [Italiano](CONVERTER_NETWORK.it.md) · **Português**

`torque_converter` participa da mesma solução de eixo, motor elétrico, cilindro, embreagem e engrenagem ideal. Bomba e turbina são nós rotacionais distintos com inércia explícita. O estator é solo estacionário; a reação dele é observável, mas não realiza trabalho. A perda de fluido alimenta um nó térmico opcional ou o livro externo de rejeição de calor. Uma `clutch` paralela separada fornece a trava. Todos os parâmetros do laboratório são sintéticos e `unverified`.

## Mapas e equações explícitos

Quatro mapas são obrigatórios: `pump_positive`, `pump_negative`, `turbine_positive` e `turbine_negative`. O membro de maior velocidade absoluta é o acionador de referência; a bomba vence um empate exato. O sinal desse membro seleciona o mapa positivo/negativo dele. Esta é uma convenção matemática de membro de referência, inclusive durante a contrarrotação. Ela não infere uma característica de ré ou de costa indisponível.

Para a velocidade do acionador `wD` e a velocidade do seguidor `wF`, `s = wF/wD` fica em `[-1,1]`. Cada mapa contém de 2 a 32 pontos explícitos, com `speed_ratio` adimensional, `torque_ratio` R e `capacity_coefficient` C em `nm_s2_rad2`. C multiplica a velocidade ao quadrado; não é um K-factor inverso. R e C interpolam linearmente e nunca são extrapolados. As duas velocidades nulas produzem reações nulas e modo zero.

```text
Tdriver   = -C(s) * wD * abs(wD)
Tfollower =  R(s) * C(s) * wD * abs(wD)
Tstator   = -(Tpump + Tturbine)
Qdot      = C(s) * abs(wD)^3 * (1 - s*R(s))
```

A compilação exige nós estritamente crescentes que cubram `[-1,1]`, C/R não negativos e `s*R(s) <= 1` ao longo de cada segmento. Conferir só os nós não basta: R linear em s torna a eficiência quadrática; qualquer máximo interior também é conferido. As inclinações precisam ser finitas. Em `s=1`, C precisa ser zero e R um, dando torque de fluido zero em velocidade igual e no mesmo sentido. Em `s=-1`, os mapas bomba-positiva/turbina-negativa e bomba-negativa/turbina-positiva precisam dar reações físicas coincidentes. Isso evita um salto quando o membro de referência muda durante a contrarrotação. A comparação dos extremos admite só a tolerância de arredondamento `64*epsilon*(abs(a)+abs(b))`. Os arranjos de mapa são próprios e imutáveis; os coeficientes normalizados entram na impressão digital do modelo.

Essas restrições definem o modelo passivo atual de mapa com sinal do Power!. Elas não reivindicam cobrir curvas medidas arbitrárias de conversor. Convenções gerais de tração/costa baseadas em mapa estão documentadas pelo [MathWorks Torque Converter](https://www.mathworks.com/help/sdl/ref/torqueconverter.html) e pelo [exemplo de dois modos](https://de.mathworks.com/help/sdl/ug/torque-converter-two-mode.html). Os quatro mapas com sinal, a validação da interpolação e o solver abaixo são projeto do Power!; nenhum código-fonte nem conjunto de parâmetros medidos foi copiado dessas referências.

## Integração acoplada e limites

As reações do conversor usam as velocidades de ponto médio do intervalo. Quando há cilindros, um sistema de Newton conjunto resolve o trabalho discreto no ângulo do virabrequim e as duas velocidades de porta do conversor. Cada resposta de torque unitário inclui o sistema eletromecânico e a projeção de engrenagem permanente. As iterações de restrição da embreagem chamam essa mesma solução não linear; a subdivisão de captura e de reversão regenera as respostas do intervalo. Não há torque de conversor atrasado aplicado depois da integração do cilindro ou da embreagem.

A solução não linear tem 24 iterações e 12 tentativas de busca em linha por redução à metade, por iteração. A tolerância do resíduo de ângulo do cilindro é `2e-14 rad`. Um resíduo de velocidade do conversor usa `2e-12 + 64*epsilon*(abs(value)+abs(free_prediction)) rad/s`. Jacobianos analíticos por trechos do mapa e derivadas de trabalho do cilindro por diferença finita montam o sistema conjunto. Os limites existentes de curso do cilindro de 0.25 rad, de resolução de válvula/queima, de iteração/evento da embreagem e de restrição de engrenagem continuam valendo. No máximo oito conversores são suportados dentro dos orçamentos existentes de 32 nós, 64 componentes e 64 estados. Cada conversor acrescenta quatro estados lógicos de histórico observável: dois torques médios, potência média de calor e calor acumulado. A soma de calor compensada também participa de cópia/hash/rollback.

O calor aceito do intervalo é `-h*(Tp*wp_mid + Tt*wt_mid)`, de modo que o trabalho mecânico removido é o calor registrado. Calor negativo além da tolerância de arredondamento da velocidade resolvida rejeita o intervalo; só resíduo negativo da ordem do arredondamento é limitado a zero. Torques e potência de calor são ponderados pela duração ao longo dos intervalos internos aceitos e depois divididos pelo tick completo. Ensaios especulativos de evento nunca confirmam o calor nem as reações. Todo o estado de conversor, embreagem, engrenagem, gás, histórico de queima, entrada e livro global sofre rollback num lote que falha ou é cancelado. As ramificações são donas dos próprios espaços de trabalho. Os testes exercitam captura sem alocação.

A integração de ponto médio é de segunda ordem para o movimento suave do conversor isolado. O modelo em combustão acoplado conserva o acoplamento explícito de temperatura de parede e a ruptura da embreagem pela média do intervalo, então não reivindica segunda ordem uniforme através de todas as transições. Refine os ticks para uma falha numérica ou um estudo de precisão; inspecione inclinações de mapa, escalas de inércia/velocidade e restrições de embreagem antes de recriar uma sessão. Um mapa válido não garante que todo passo escolhido seja resolvível.

## Contrato compartilhado de modelo e de observáveis

O JSON usa `node_a` obrigatório (bomba), `node_b` (turbina), quatro arranjos em `parameters` e `heat_node` opcional. Não aceita canal de entrada do conversor, porta de porta-planetas nem padrões implícitos de mapa. `ComponentDefinition.TorqueConverter` expõe o mesmo modelo do Core. Os erros de mapa carregam o ID do componente e um campo acionável como `converter.pump_positive` ou `converter.counter_rotation`.

| Campo | Significado | Unidade |
|---|---|---|
| `torque` | Torque médio da bomba no último tick completo | Nm |
| `torque_at_b` | Torque médio da turbina no último tick completo | Nm |
| `torque_at_c` | Reação média do estator estacionário no último tick completo | Nm |
| `heat_flow` | Potência média de calor do fluido no último tick completo | W |
| `fluid_heat` | Calor de fluido aceito, acumulado | J |
| `speed_ratio` | Relação de velocidade seguidor/acionador com sinal, atual; zero quando parado | fraction |
| `converter_drive` | 0 parado, 1 bomba positiva, 2 bomba negativa, 3 turbina positiva, 4 turbina negativa | state code |

Torques e potência médios começam em zero. Atualizações de entrada na fronteira não reescrevem as médias do tick anterior. `torque_at_c` nomeia aqui a reação do estator; este componente não tem terceiro rotor. Modelos que contêm conversor anunciam `quasisteady_converter_powertrain` e acrescentam a etiqueta de impressão digital 10. Impressões digitais e trajetórias de modelos sem conversor permanecem inalteradas. O asset portátil v10 retém os quatro mapas; leitores v1–v9 e os fixtures autênticos permanecem.

## Laboratório e evidência

Peça o exemplo MCP `fired-converter`, ou execute:

```sh
dotnet src/Power.Cli/bin/Release/net10.0/Power.Cli.dll assets/labs/fired-converter.power.json --output artifacts/reports/fired-converter.json
```

O laboratório de 0.8 s começa com uma bomba a 600 rpm e uma turbina a 300 rpm, alimentando o sol de uma planetária e uma redução final 3:1. A frenagem da coroa seleciona redução; uma embreagem sol/coroa seleciona a direta. Trava, liberação e recaptura programadas de forma independente exercitam os caminhos de fluido e de atrito. São eventos prescritos, não um controlador de transmissão automática.

Em ticks de 50,000 ns, todas as 87 fronteiras do relatório reproduzem exatamente via CLI, assets portáteis e MCP. A impressão digital do modelo é `839d03901973668d` e o hash de estado final é `834a679376b7a6fd`. A velocidade final bomba/turbina é aproximadamente 73.37748 rad/s, a velocidade da carga 6.988331 rad/s, o calor de fluido 24.27663 J e o calor da trava 22.84709 J. A embreagem de troca e o freio acrescentam 157.18199 J e 83.42289 J. O nó de calor compartilhado chega a 301.438643 K; o resíduo total de energia é cerca de `3.30e-11 J`. Esses números descrevem um transitório sintético.

Testes independentes cobrem a solução analítica de duas inércias `C(s)=k(1-s), R=1`, o decaimento analítico de estol, o refinamento de segunda ordem, o encaminhamento térmico e de calor externo, o balanço do estator, estados de ré/costa/contrarrotação, portas de conversor compartilhadas, reflexão de engrenagem, trava, rollback do lote completo, cancelamento, ramificações e alocação zero. O refinamento do modelo em combustão confere uma distância combinada adimensional de velocidade final, calor de fluido e calor da trava contra uma execução de 3,125 ns, usando cinco tamanhos de tick. Também limita as diferenças absolutas abaixo de 0.0002 rad/s ou J, respectivamente. Os erros individuais de calor não precisam diminuir a cada redução pela metade perto de eventos. Essa verificação é separada da ordem analítica do caso isolado. Posse/validação do mapa com sinal e registros portáteis malformados reassinados têm verificações dedicadas.

A [rede hidráulica](HYDRAULIC_NETWORK.pt-BR.md) agora fornece capacidade de trava e de troca derivada da pressão. Momento angular do fluido, dinâmica de enchimento/pressão do conversor, mecânica de estator girante/de roda livre, propriedades dependentes da temperatura, dinâmica de bomba/regulador e de pistão, topologia DCT/AT completa e coordenação ECU/TCU continuam em aberto. Comportamento completo do motor, medições OEM e calibração de veículo também continuam em aberto. O Studio tem portas de fluido esquemáticas e testes preparados; a evidência real de Editor/Play/IL2CPP continua separada e indisponível neste ambiente.
