# Fundamento do cilindro fechado

[English](SEALED_CYLINDER.md) · [简体中文](SEALED_CYLINDER.zh-CN.md) · [Français](SEALED_CYLINDER.fr.md) · [Русский](SEALED_CYLINDER.ru.md) · [日本語](SEALED_CYLINDER.ja.md) · [한국어](SEALED_CYLINDER.ko.md) · [Deutsch](SEALED_CYLINDER.de.md) · [Español](SEALED_CYLINDER.es.md) · [Italiano](SEALED_CYLINDER.it.md) · **Português**

`sealed_cylinder` acopla uma biela-manivela rígida a um nó rotacional. O cilindro contém uma massa fixa de gás ideal com razão de calores específicos constante e sem transferência de calor com a parede. É uma referência de compressão/expansão, não um motor em combustão completo. Admissão, escape, combustível, combustão, vazamento, transferência de calor com a parede, inércia alternativa e eventos de controle continuam como trabalho de implementação separado. Todos os parâmetros atuais são sintéticos e `unverified`.

A pressão e a temperatura iniciais valem no ângulo inicial do rotor conectado mais a fase do cilindro. Mudar esse ângulo inicial muda a massa aprisionada, a menos que a pressão e a temperatura sejam ajustadas de forma coerente. O estado do gás deriva da posição do virabrequim e da entropia inicial imutável; acrescenta canais observáveis, mas nenhuma variável de estado independente. Essa redução só vale para o componente adiabático fechado.

## Geometria e estado do gás

Os comprimentos compilam para metros, as pressões para pascais e a fase para radianos. A entrada aceita `m`/`mm`, `pa`/`bar` e `rad`/`deg`. A temperatura está em kelvin; a constante específica do gás usa `j_kg_k`. A razão de compressão e gamma são adimensionais. O diâmetro e o curso devem ser positivos, o comprimento da biela deve superar a metade do curso, a razão de compressão e gamma devem ser maiores que um, e a pressão, a temperatura e a constante do gás iniciais devem ser positivas. A contrapressão pode ser zero.

Com raio da manivela `r = stroke/2`, comprimento da biela `l`, área do pistão `A = π bore²/4` e ângulo `θ` medido a partir do ponto morto superior:

```text
x(θ) = r (1 - cos θ) + l - sqrt(l² - r² sin² θ)
Vc   = A stroke / (compression_ratio - 1)
V(θ) = Vc + A x(θ)
```

A implementação usa uma forma algebricamente equivalente para evitar cancelação perto do ponto morto superior. Essa geometria segue a [relação de volume da biela-manivela centrada da Colorado State University](https://www.engr.colostate.edu/~allan/thermo/page2/page2.html).

Sejam `V0`, `P0` e `T0` o estado inicial. As relações reversíveis do gás ideal são:

```text
m = P0 V0 / (R T0)
P = P0 (V0/V)^gamma
T = T0 (V0/V)^(gamma-1)
U = P V / (gamma-1)
τ = (P - Pback) dV/dθ
```

As relações de pressão/volume e de temperatura seguem a [dedução de compressão isentrópica da NASA](https://www1.grc.nasa.gov/beginners-guide-to-aeronautics/isentrophic-compression/). Com razão de compressão 10 e gamma 1.4, a compressão do ponto morto inferior ao superior multiplica a pressão por cerca de 25.119 e a temperatura por cerca de 2.512. São razões idealizadas, não desempenho medido de motor.

## Integração e energia

A solução de ponto médio eletromecânica existente fornece uma solução de base e uma resposta pré-calculada ao torque em cada virabrequim distinto de cilindro. Uma solução não linear reduzida determina os incrementos angulares desses virabrequins. Cilindros no mesmo virabrequim contribuem para uma soma de torques; virabrequins acoplados são resolvidos juntos. Nenhuma chamada a provedor de modelos, objeto do Unity ou dependência de terceiros participa de um tick de física.

Cada cilindro usa um torque discreto consistente com o trabalho:

```text
τ_discrete Δθ = -(U_next - U_old) - Pback (V_next - V_old)
```

Diferenças divididas analíticas estáveis do volume e avaliações de logaritmo/exponencial de argumento pequeno tratam incrementos pequenos e passagens pelos pontos mortos. A saída instantânea `Torque` continua sendo `(P-Pback) dV/dθ`; ela é distinta do torque médio usado para integrar um tick finito.

A variação global da energia armazenada inclui a variação da energia interna do gás. O trabalho de contrapressão é trabalho de fonte externo, `-Pback ΔV`, então o livro permanece `source_work - heat_rejected - stored_energy_change`. A dissipação do eixo e do motor continua entrando na rede térmica ou no calor rejeitado. As variações de energia do gás são avaliadas diretamente para evitar subtrair energias absolutas grandes quando gamma se aproxima de um.

A iteração de Newton limita-se a 16 iterações, com no máximo 10 tentativas de busca linear por iteração. O preditor linear e o percurso aceito do virabrequim devem permanecer dentro de 0.25 radianos por tick. Valores não finitos, percurso excessivo ou falha de convergência devolvem `NumericalFailure`; a chamada inteira, inclusive entradas programadas e atualizações do livro, sofre rollback. Reduza `step_ns` e recrie o modelo ou a sessão para tentar de novo com um tick fixo menor. A aceitação não é garantia de precisão do passo de tempo. Ângulos acumulados muito grandes também perdem resolução angular em binary64; a precisão de longa duração precisa da própria evidência.

## Experimento observável e portátil

Cada cilindro expõe pressão (Pa), temperatura do gás (K), volume (m³), massa fixa (kg), energia interna absoluta (J), deslocamento do pistão a partir do ponto morto superior (m) e torque no virabrequim (N·m). Os IDs de canal conservam a codificação objeto/campo existente. O modelo informa a fidelidade `sealed_adiabatic_gas` e a calibração `unverified`.

Execute `assets/labs/sealed-cylinder.power.json` pela CLI, ou solicite `get_example_model({"name":"sealed-cylinder"})` pelo MCP. A amostra usa um tick de 100 µs, duração de 0.2 s, duas mudanças de torque e 21 limites de relatório. Os KPIs declarados valem para a amostra final, como nos experimentos existentes; os testes de conservação do núcleo inspecionam limites repetidos ao longo das execuções.

O mesmo documento exporta para `SealedCylinder.powerasset`. O Unity tem uma vista esquemática do pistão comandada pelo canal de deslocamento; uma unidade de cena representa um curso completo. As dimensões físicas e as saídas permanecem no SI. A evidência real de Editor, Play Mode e IL2CPP continua pendente.

`EngineChecks` executa verificações de geometria analítica e de gás ideal, execuções de conservação de dois segundos, refinamento de passo de segunda ordem, rotação inversa, casos de passo pequeno e ponto morto, vários cilindros em virabrequins compartilhados e acoplados, acoplamento elétrico/térmico, falha e recuperação atômicas, cancelamento, ramos independentes, compatibilidade de assets e avanço sem alocação. As mesmas verificações rodam nos dois assemblies de destino no host .NET.
