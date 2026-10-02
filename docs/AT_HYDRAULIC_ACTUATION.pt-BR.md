# Acionamento hidráulico da transmissão

[English](AT_HYDRAULIC_ACTUATION.md) · [简体中文](AT_HYDRAULIC_ACTUATION.zh-CN.md) · [Français](AT_HYDRAULIC_ACTUATION.fr.md) · [Русский](AT_HYDRAULIC_ACTUATION.ru.md) · [日本語](AT_HYDRAULIC_ACTUATION.ja.md) · [한국어](AT_HYDRAULIC_ACTUATION.ko.md) · [Deutsch](AT_HYDRAULIC_ACTUATION.de.md) · [Español](AT_HYDRAULIC_ACTUATION.es.md) · [Italiano](AT_HYDRAULIC_ACTUATION.it.md) · **Português**

O grafo de pesquisa Ravigneaux agora pode usar pistões hidráulicos reais para as cinco embreagens/freios de faixa e, no experimento de conversor em combustão, para a trava. Uma bomba compartilhada acionada pelo eixo fornece pressão de linha flexível. Válvulas explícitas de enchimento/dreno movem óleo para a câmara de cada atuador. A pressão move um pistão de massa finita através da folga das pastilhas; o contato elástico determina a capacidade da embreagem. Molas de retorno e amortecimento liberam a pastilha depois da drenagem. Um comando de válvula sozinho não implica trava.

## Contrato do conjunto

`HydraulicActuationAssembly` recebe propriedades explícitas de alimentação e de atuador no SI. Devolve definições ordinárias imutáveis, usando a física existente de bomba, pressão, pistão, mola, válvula e embreagem de contato. Quem chama fornece o eixo da bomba, o sumidouro térmico comum e de 1 a 6 embreagens alvo declaradas. Cada ramo exige IDs distintos de câmara/deslizador/pistão/mola/válvula e dois canais de entrada.

O conjunto substitui o engate de atrito prescrito do alvo por uma `piston_clutch`. A entrada antiga de engate está ausente. `ValveInputs` mapeia uma fração de aplicação para comandos de enchimento e de dreno complementar; as duas válvulas também podem ser operadas de forma explícita. É um auxiliar de roteamento hidráulico, não um controlador AT. Só a pressão física, o curso e o contato das pastilhas estabelecem capacidade e trava.

A cilindrada da bomba, o vazamento e o arrasto do eixo são explícitos. O alívio encaminha o óleo ao limite de tanque declarado. Nenhuma fonte de pressão substitui o trabalho da bomba. A flexibilidade armazena `C p^2/2`; pistões em movimento trocam trabalho de pressão com a energia da mola, a cinética e a de contato. As áreas frontal/traseira e a pressão do tanque são explícitas. Com áreas desiguais, o inventário líquido de fluido inclui o termo correspondente de volume varrido. Restrição, alívio, amortecimento de retorno, arrasto do eixo e deslizamento da embreagem encaminham as perdas reais ao sumidouro térmico comum. O trabalho de contrapressão usa o limite explícito de reservatório existente.

Os batentes de curso armazenam energia elástica, em vez de grampear a posição. A pressão manométrica é restringida pelo modelo hidráulico existente; pressão negativa é uma falha numérica/física, não um grampeamento silencioso de cavitação. Vazamento/atrito de vedação, aeração do fluido, cavitação e propriedades medidas dependentes da temperatura exigem mais modelos e dados.

## Experimentos compartilhados

`hydraulic-ravigneaux-transmission` aplica os cinco elementos de faixa por enchimento real e movimento de retorno durante entregas à frente subindo/descendo. `fired-hydraulic-ravigneaux` acrescenta o motor, o conversor e o sexto atuador hidráulico de trava. Os dois conservam giro absoluto dos planetas e inércia orbital, com a geometria e as propriedades de autoria registradas nas descrições de fonte.

A alimentação de pesquisa usa cilindrada 1e-6 m3/rad, vazamento 1e-12 m3/(s Pa), arrasto 0.02 Nm s/rad, flexibilidade de linha 2e-11 m3/Pa e pressão inicial de linha 1 MPa. O alívio abre a 1 MPa com condutância 5e-10 m3/(s Pa); a pressão manométrica do tanque é zero. Cada ramo tem flexibilidade 2e-12 m3/Pa, áreas frontal/traseira 0.001/0 m2, massa do pistão 0.02 kg, curso de 0 a 6 mm e contato das pastilhas em 2 mm. A rigidez de retorno é 10000 N/m, o amortecimento 300 N s/m, e as rigidezes de pastilha/batente 1e6 N/m. Essas propriedades permanecem entradas de pesquisa `unverified`.

Cinco ramos de faixa usam pares de válvula 700/701 até 708/709. A trava em combustão usa 710/711. Os canais expõem pressões de linha/câmara, curso/velocidade do pistão, força de contato, capacidades, modos reais da embreagem, trabalho da bomba, volume de fluido e calor. O estado mecânico, de pressão, de gás e térmico completo participa dos mesmos hashes, ramificações, cancelamento e rollback de lote.

O grafo usa o asset portátil v24 e os registros de componente existentes. JSON, CLI, MCP e os casos preparados de importação/reprodução do Studio compartilham essas definições. Refinamento independente da EDO pressão-movimento da bomba, livros de volume varrido/energia, carregamento real de seis ramos, confirmação real de caminho e cada fronteira portátil têm verificações separadas. Execute `dotnet run --file tools/Build.cs -- verify`; o escopo numérico e os resultados registrados pertencem a [VALIDATION.pt-BR.md](VALIDATION.pt-BR.md).

## Aceitação restante

Os cronogramas continuam prescritos. Sequenciamento AT confirmado por sensor, controle de pressão em malha fechada, coordenação de torque da ECU, dinâmica de válvula/solenoide e falhas abrangentes estão inacabados. Esse roteamento genérico não estabelece a identidade do corpo de válvulas PSA AT8/AL4 nem calibração OEM. A evidência real de Unity Editor/Play/Player/IL2CPP também é separada dos testes gerenciados/Standard. O objetivo completo de powertrain e os limites de evidência das amostras permanecem intactos.
