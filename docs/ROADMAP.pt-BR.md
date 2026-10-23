# Roteiro de desenvolvimento do Power!

[English](ROADMAP.md) · [简体中文](ROADMAP.zh-CN.md) · [Français](ROADMAP.fr.md) · [Русский](ROADMAP.ru.md) · [日本語](ROADMAP.ja.md) · [한국어](ROADMAP.ko.md) · [Deutsch](ROADMAP.de.md) · [Español](ROADMAP.es.md) · [Italiano](ROADMAP.it.md) · **Português**

O objetivo é a plataforma completa de powertrain do Power!: física moderna em C#, um estúdio Unity 3D e operação direta por agentes. Um laboratório sintético aprovado estabelece um resultado numérico limitado; motor, transmissão, controles, calibração do veículo e aceitação de desktop precisam cada um da própria evidência.

## Marcos

| Marco | Base disponível | Trabalho que ainda falta |
|---|---|---|
| Núcleo gerenciado | Física sem dependências e de alvo duplo, topologia, unidades, tempo inteiro, replay e transações atômicas | Validação de powertrain integrado em longa duração |
| Interface de agente | Ferramentas MCP definidas por esquema, diagnósticos estruturados, revisões, ramificações, cancelamento e relatórios compactos; `list-labs` / `get_capabilities.examples` | Fluxos de modelagem/controle para o escopo restante de powertrain completo |
| Estúdio Unity | Importação compartilhada de modelo, reprodução de laboratório, componentes 3D esquemáticos e testes preparados | Evidência real de Editor/Play/Player/IL2CPP e empacotamento de desktop |
| Bancada de modelagem | Asset portátil v35, leitores v1-v34 e definições compartilhadas de JSON/CLI/MCP | Edição de grafo, gravação e gráficos de canais selecionáveis |
| Física do motor | Massa/energia de gás independentes, trabalho de biela-manivela, válvulas cronometradas, combustão prescrita, medição de combustível gasoso e líquido, trilhos flexíveis finitos, evaporação de filme e acionamento físico da agulha; [Rail de combustível líquido alimentado por bomba](PUMP_FED_FUEL.pt-BR.md); tanques finitos e retornos conservativos de alívio; [Geometria do tanque e espaço gasoso finito](TANK_HEADSPACE.pt-BR.md); [Equilíbrio de fases do combustível e limites de suprimento](FUEL_PHASE_EQUILIBRIUM.pt-BR.md); [Equilíbrio do tanque com volume líquido finito](TANK_PHASE_EQUILIBRIUM.pt-BR.md); [Descarga de combustível com título congelado e receptores finitos](FUEL_DISCHARGE.pt-BR.md) | Resolver inércia e perdas locais de entrada, sensibilidade à relaxação de fase e deslizamento; depois ignição, admissão/escape, perdas mecânicas e ECU/TCU coordenados. Cada equação exige referências independentes e refinamento temporal; calibração medida e aceitação Unity real são separadas. |
| Transmissão | Embreagens acopladas, engrenagens/planetárias, conversor/trava mapeados, hidráulica e caminhos DCT de sete marchas à frente/ré e Ravigneaux de quatro faixas à frente/ré com giro/inércia orbital dos planetas resolvidos | Flexibilidade/perdas de engrenamento e divisão de carga, acionamento DCT, controle completo de pressão/troca da AT e roteamento medido, mapas medidos, comportamento de válvula/vedação/cavitação e dinâmica mais rica do conversor |
| Controles e integração elétrica | PI de pressão amostrado, controle de fechamento da agulha e entrega DCT escalonada confirmada por sensor, tensão/duty limitados, posse do atuador, circuito equivalente de bateria e acessórios; [`at_controller`](AT_CONTROL.pt-BR.md) | Ciclos coordenados de ECU/TCU, sensores/atuadores, solicitações de torque, falhas, BMS e comportamento térmico/elétrico medido |
| Evidência do veículo e lançamento | Amostras de pesquisa com fronteiras e procedência completas | Dois powertrains medidos completos, orçamentos de incerteza, estabilidade, aceitação de desktop e distribuição |

Os pontos de controle numéricos atuais, as fixtures autênticas de asset e os registros de verificação por plataforma estão em [VALIDATION.pt-BR.md](VALIDATION.pt-BR.md). Veja [DEVELOPMENT_STATUS.pt-BR.md](DEVELOPMENT_STATUS.pt-BR.md) para o estado da implementação e [ARCHITECTURE.pt-BR.md](ARCHITECTURE.pt-BR.md) para os invariantes. A evidência de CI publicada vale para a revisão registrada; mudanças locais novas precisam de aceitação separada em cada plataforma.

## Próximo trabalho gerenciado

Estender movimento a densidade variável e mudança de fase com fechamento energético/entrópico compatível antes de afirmar entrada cavitante. Continuar relaxação/deslizamento, ignição, admissão/escape, perdas, transmissão e ECU/TCU. Calibração medida e Unity real seguem separados. [Quantidade de movimento da linha finita de combustível líquido](FUEL_INERTIAL_PIPE.pt-BR.md)

Depois estenda ignição/controle, dinâmica de admissão/escape e perdas mecânicas do motor. A queima de Wiebe atual é prescrita e não estabelece combustão preditiva, detonação, emissões nem calibração OEM. O [contrato de medição gasosa](FUEL_METERING.pt-BR.md) continua um caminho independente suportado.

Construa sobre o [grafo DCT de sete marchas à frente/ré](DUAL_CLUTCH_TRANSMISSION.pt-BR.md) com acionamento detalhado de sincronizador/garras/embreagem. Construa sobre o [grafo Ravigneaux](RAVIGNEAUX_TRANSMISSION.pt-BR.md) com [propriedades medidas dos planetas e comportamento de engrenamento](RESOLVED_PLANETS.pt-BR.md), [acionamento completo por pistão alimentado pela bomba](AT_HYDRAULIC_ACTUATION.pt-BR.md) e controle AT usando os primitivos acoplados de conversor, engrenagens, embreagens e hidráulica. Construa sobre o [controle DCT amostrado](DCT_CONTROL.pt-BR.md) em direção a trocas com mistura de torque e coordenação limitada de ECU/TCU, incluindo solicitações de torque, sensores/atuadores e falhas recuperáveis. Estenda os modelos de perda constante da bomba, bateria e válvula/acumulador quando houver dados medidos de propriedade/controle; os valores de pesquisa fornecidos continuam não verificados.

A bomba do domínio de combustível agora alimenta agulha física e descarga de título congelado em um receptor em combustão com volume líquido finito. Os inventários completos incluem mudança de fase, trabalho de deslocamento, energia cinética e impulso na parede imóvel. [Bomba de combustível com mudança de fase acionada por eixo](FUEL_CHAMBER_PUMP.pt-BR.md)

## Estúdio e aceitação medida

Defina `POWER_UNITY_EDITOR` com o Editor fixado e execute `unity-test`. Obtenha evidência real de importação/Play/renderização e, em seguida, evidência de Player/IL2CPP. Seleção geral de canais, edição de grafo e gravação continuam recursos separados do Studio. CLI, MCP e Unity precisam continuar consumindo a mesma semântica de modelo.

EA211 DJS + DQ200 e PSA EC5 + AT8 conservam fronteiras completas de powertrain, aplicabilidade do veículo e manifestos de evidência. Medições OEM ausentes não são substituídas por padrões silenciosos. Conclusão funcional, correção numérica e credibilidade medida do veículo exigem aceitação separada.

O [acervo Zig](NATIVE_ZIG.pt-BR.md) conserva os hashes originais e a procedência Git em `legacy/native/migration-manifest.json`, incluindo a revisão C original `c342d4c`. Ele continua separado do aplicativo C#/Unity ativo. A migração nativa não conclui a migração de recursos gerenciados nem a aceitação do powertrain. Introduza paralelismo adicional, solução esparsa ou Burst quando as medições justificarem e os contratos do núcleo continuarem estáveis.

[Descarga de combustível com título congelado e receptores finitos](FUEL_DISCHARGE.pt-BR.md)
