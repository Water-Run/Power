# Roteiro de desenvolvimento do Power!

[English](ROADMAP.md) · [简体中文](ROADMAP.zh-CN.md) · [Français](ROADMAP.fr.md) · [Русский](ROADMAP.ru.md) · [日本語](ROADMAP.ja.md) · [한국어](ROADMAP.ko.md) · [Deutsch](ROADMAP.de.md) · [Español](ROADMAP.es.md) · [Italiano](ROADMAP.it.md) · **Português**

O objetivo é a plataforma completa de powertrain do Power!: física moderna em C#, um estúdio Unity 3D e operação direta por agentes. Um laboratório sintético aprovado estabelece um resultado numérico limitado; motor, transmissão, controles, calibração do veículo e aceitação de desktop precisam cada um da própria evidência.

## Marcos

| Marco | Base disponível | Trabalho que ainda falta |
|---|---|---|
| Núcleo gerenciado | Física sem dependências e de alvo duplo, topologia, unidades, tempo inteiro, replay e transações atômicas | Validação de powertrain integrado em longa duração |
| Interface de agente | Ferramentas MCP definidas por esquema, diagnósticos estruturados, revisões, ramificações, cancelamento e relatórios compactos | Fluxos de modelagem/controle para o escopo restante de powertrain completo |
| Estúdio Unity | Importação compartilhada de modelo, reprodução de laboratório, componentes 3D esquemáticos e testes preparados | Evidência real de Editor/Play/Player/IL2CPP e empacotamento de desktop |
| Bancada de modelagem | Asset portátil v24, leitores v1-v23 e definições compartilhadas de JSON/CLI/MCP | Edição de grafo, gravação e gráficos de canais selecionáveis |
| Física do motor | Massa/energia de gás independentes, trabalho de biela-manivela, válvulas cronometradas, combustão prescrita, medição de combustível gasoso e líquido, trilhos flexíveis finitos, evaporação de filme e acionamento físico da agulha | Bomba/reabastecimento do trilho, comportamento magnético/eletrônico/de pulverização refinado, acoplamento de volume líquido finito, controle de ignição, admissão/escape detalhados, perdas mecânicas, termoquímica e calibração medida |
| Transmissão | Embreagens acopladas, engrenagens/planetárias, conversor/trava mapeados, hidráulica e caminhos DCT de sete marchas à frente/ré e Ravigneaux de quatro faixas à frente/ré com giro/inércia orbital dos planetas resolvidos | Flexibilidade/perdas de engrenamento e divisão de carga, acionamento DCT, controle completo de pressão/troca da AT e roteamento medido, mapas medidos, comportamento de válvula/vedação/cavitação e dinâmica mais rica do conversor |
| Controles e integração elétrica | PI de pressão amostrado, controle de fechamento da agulha e entrega DCT escalonada confirmada por sensor, tensão/duty limitados, posse do atuador, circuito equivalente de bateria e acessórios | Ciclos coordenados de ECU/TCU, sensores/atuadores, solicitações de torque, falhas, BMS e comportamento térmico/elétrico medido |
| Evidência do veículo e lançamento | Amostras de pesquisa com fronteiras e procedência completas | Dois powertrains medidos completos, orçamentos de incerteza, estabilidade, aceitação de desktop e distribuição |

Os pontos de controle numéricos atuais, as fixtures autênticas de asset e os registros de verificação por plataforma estão em [VALIDATION.pt-BR.md](VALIDATION.pt-BR.md). Veja [DEVELOPMENT_STATUS.pt-BR.md](DEVELOPMENT_STATUS.pt-BR.md) para o estado da implementação e [ARCHITECTURE.pt-BR.md](ARCHITECTURE.pt-BR.md) para os invariantes. A evidência de CI publicada vale para a revisão registrada; mudanças locais novas precisam de aceitação separada em cada plataforma.

## Próximo trabalho gerenciado

Continue o [contrato de injeção líquida](LIQUID_FUEL_INJECTION.pt-BR.md) com bomba/reabastecimento do trilho e acionamento magnético/eletrônico refinado. Massa finita da fonte flexível e energia de pressão, movimento real da agulha, realimentação amostrada de dose, reposição do filme e evaporação estão implementados. Veja [o contrato da agulha](NEEDLE_ACTUATION.pt-BR.md) e a [predição limitada de fechamento](CLOSURE_PREDICTION.pt-BR.md). O receptor ainda exporta trabalho de pressão de deslocamento sob a fronteira declarada de volume líquido desprezível; pulverização/deslocamento resolvidos precisam substituí-la por geometria verificada e acoplamento de momento/trabalho. Mantenha a entrega, a disponibilidade de vapor e a reação prescrita separadas, e conserve a evidência analítica/de conservação/de convergência.

Depois estenda ignição/controle, dinâmica de admissão/escape e perdas mecânicas do motor. A queima de Wiebe atual é prescrita e não estabelece combustão preditiva, detonação, emissões nem calibração OEM. O [contrato de medição gasosa](FUEL_METERING.pt-BR.md) continua um caminho independente suportado.

Construa sobre o [grafo DCT de sete marchas à frente/ré](DUAL_CLUTCH_TRANSMISSION.pt-BR.md) com acionamento detalhado de sincronizador/garras/embreagem. Construa sobre o [grafo Ravigneaux](RAVIGNEAUX_TRANSMISSION.pt-BR.md) com [propriedades medidas dos planetas e comportamento de engrenamento](RESOLVED_PLANETS.pt-BR.md), [acionamento completo por pistão alimentado pela bomba](AT_HYDRAULIC_ACTUATION.pt-BR.md) e controle AT usando os primitivos acoplados de conversor, engrenagens, embreagens e hidráulica. Construa sobre o [controle DCT amostrado](DCT_CONTROL.pt-BR.md) em direção a trocas com mistura de torque e coordenação limitada de ECU/TCU, incluindo solicitações de torque, sensores/atuadores e falhas recuperáveis. Estenda os modelos de perda constante da bomba, bateria e válvula/acumulador quando houver dados medidos de propriedade/controle; os valores de pesquisa fornecidos continuam não verificados.

## Estúdio e aceitação medida

Defina `POWER_UNITY_EDITOR` com o Editor fixado e execute `unity-test`. Obtenha evidência real de importação/Play/renderização e, em seguida, evidência de Player/IL2CPP. Seleção geral de canais, edição de grafo e gravação continuam recursos separados do Studio. CLI, MCP e Unity precisam continuar consumindo a mesma semântica de modelo.

EA211 DJS + DQ200 e PSA EC5 + AT8 conservam fronteiras completas de powertrain, aplicabilidade do veículo e manifestos de evidência. Medições OEM ausentes não são substituídas por padrões silenciosos. Conclusão funcional, correção numérica e credibilidade medida do veículo exigem aceitação separada.

O [acervo Zig](NATIVE_ZIG.pt-BR.md) conserva os hashes originais e a procedência Git em `legacy/native/migration-manifest.json`, incluindo a revisão C original `c342d4c`. Ele continua separado do aplicativo C#/Unity ativo. A migração nativa não conclui a migração de recursos gerenciados nem a aceitação do powertrain. Introduza paralelismo adicional, solução esparsa ou Burst quando as medições justificarem e os contratos do núcleo continuarem estáveis.
