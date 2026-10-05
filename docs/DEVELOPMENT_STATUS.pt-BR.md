# Estado de desenvolvimento

[English](DEVELOPMENT_STATUS.md) · [简体中文](DEVELOPMENT_STATUS.zh-CN.md) · [Français](DEVELOPMENT_STATUS.fr.md) · [Русский](DEVELOPMENT_STATUS.ru.md) · [日本語](DEVELOPMENT_STATUS.ja.md) · [한국어](DEVELOPMENT_STATUS.ko.md) · [Deutsch](DEVELOPMENT_STATUS.de.md) · [Español](DEVELOPMENT_STATUS.es.md) · [Italiano](DEVELOPMENT_STATUS.it.md) · **Português**

O Power! tem um núcleo de simulação gerenciado, documentos de modelo compartilhados e assets portáteis, uma CLI sem interface, um serviço de agente MCP e um estúdio Unity preparado. Laboratórios sintéticos exercitam comportamentos de motor, transmissão, hidráulica e elétrica. Powertrains completos, controle coordenado de ECU/TCU, calibração medida e um aplicativo Unity de desktop aceito continuam inacabados.

## Implementação atual

| Área | Implementado | Aceitação restante |
|---|---|---|
| Núcleo | Unidades explícitas e IDs estáveis; compilação imutável; tempo inteiro limitado; balanços observáveis; replay, cancelamento, ramificações independentes e rollback do lote inteiro | Evidência de longa duração e de powertrain completo |
| Motor | Cilindros fechados/abertos, trabalho de pressão da biela-manivela, escoamento de gás bidirecional, válvulas cronometradas pelo virabrequim, calor de parede e combustão premisturada prescrita | Admissão/escape detalhados, ignição, perdas mecânicas, termoquímica mais rica e comportamento medido do motor |
| Combustível | Combustível/ar/produtos rastreados, trilhos gasosos finitos com medição de dose por ciclo, trilhos líquidos finitos e flexíveis que alimentam filmes, evaporação paga pela parede e reação apenas do vapor; [Rail de combustível líquido alimentado por bomba](PUMP_FED_FUEL.pt-BR.md) | geometria/ventilação do tanque e enchimento/regulação medidos, comportamento magnético/eletrônico/de pulverização não linear, equilíbrio de fases dependente da pressão e propriedades de combustível medidas |
| Transmissão | Embreagens estática/deslizante, acionamento por contato, engrenagens/planetárias com sinal, conversor/trava mapeados e caminhos DCT de sete marchas à frente/ré e Ravigneaux de quatro faixas à frente/ré com giro/inércia orbital dos planetas resolvidos | Flexibilidade/perdas de engrenamento e divisão de carga, acionamento DCT, controle completo de pressão/troca da AT e roteamento medido, trocas coordenadas, perdas medidas e comportamento mais rico do conversor |
| Hidráulica | Volumes flexíveis, restrições, bombas com vazamento/arrasto explícitos, alívio, pistões dinâmicos, carretéis dosados e acumuladores de gás de energia finita | Mapas medidos de válvula/acumulador/bomba, atrito de vedação, cavitação e hidráulica completa da transmissão |
| Elétrica | Motores RL, solenoides recíprocos de indutância variável, bateria de carga finita, polarização por resistência/RC, conversão de duty médio e acessórios | Comportamento químico/térmico medido, BMS, controle de corrente e integração completa da alimentação |
| Controles | PI de pressão amostrado, realimentação da agulha/predição de fechamento e controle DCT escalonado confirmado por sensor, com posse do atuador, relógios inteiros e memória transacional | Coordenação de torque ECU/TCU, sensores, atuadores e tratamento de falhas |
| Documentos e assets | 40 laboratórios JSON/CLI, 39 exemplos MCP, asset v27 e leitores v1-v26 | Edição/salvamento e coleções de modelos calibrados |
| Agentes | Doze ferramentas MCP definidas por esquema; evidência compacta, verificações de revisão e diagnósticos acionáveis | Fluxos de trabalho completos para o escopo físico/de controle restante |
| Unity | Importação de modelo, reprodução em ticks exatos, componentes 3D esquemáticos, controles, reinício e testes de ciclo de vida preparados | Aceitação real de Editor/Play, gráficos selecionáveis, edição/gravação de grafo e Player/IL2CPP |
| Acervo nativo | Protótipos de pesquisa Zig 0.15.2, ABI preservado e procedência original das fontes | Referência histórica; a migração gerenciada continua separada da funcionalidade completa |

Core e Assets visam tanto `net10.0` quanto `netstandard2.1`; o Core não tem dependência de Unity, transporte, provedor de modelos nem de terceiros. Os scripts de Assets do Unity usam C# 9. O Unity carrega os assemblies Standard compilados pelo SDK externo; ele não compila código-fonte .NET 10/C# 14.

## Evidência e limites

O [grafo de acionamento hidráulico da AT](AT_HYDRAULIC_ACTUATION.pt-BR.md) alimenta os cinco elementos de faixa e a trava opcional do conversor a partir de uma bomba compartilhada acionada pelo eixo. Caminhos explícitos de enchimento/drenagem, movimento finito do pistão, molas de retorno e capacidade derivada da pastilha conservam o trabalho de deslocamento hidráulico e o comportamento real de captura/liberação. Válvulas prescritas não são controle AT confirmado por sensor nem aceitação medida do corpo de válvulas. O comportamento completo do powertrain e a aceitação medida continuam inacabados.

O `dotnet run --file tools/Build.cs -- verify` em série exigido passa localmente no Windows x64. Ele cobre os dois alvos de assembly hospedados no .NET 10, um processo filho MCP real, todos os relatórios de laboratório, o acervo Zig e o ABI C#. Contagens atuais, caminhos de log, conferências de esquema, resultados numéricos e a procedência de CI retida estão em [VALIDATION.pt-BR.md](VALIDATION.pt-BR.md). Testes de assembly Standard no .NET 10 não estabelecem compatibilidade com o runtime do Unity.

O [incremento do filme líquido](FUEL_FILM.pt-BR.md) agora tem verificações analíticas de aquecimento/saturação/secagem, referências independentes de EDO simultânea, conservação de massa/química/térmica, replay portátil e transações completas de sessão. A ordenação simétrica do filme dá refinamento suave de segunda ordem para filmes que compartilham uma parede. O acoplamento a outras fontes de calor de parede conserva o limite existente de parede explícita de primeira ordem. O replay exato é separado da precisão do passo de tempo, dos KPIs aprovados e da física calibrada.

O [grafo de pesquisa Ravigneaux](RAVIGNEAUX_TRANSMISSION.pt-BR.md) acrescenta restrições de pinhão simples/duplo, cinco caminhos de atrito, quatro faixas à frente e ré. Referências independentes de massa livre, inércia refletida e captura de freio conferem reações de porta e calor. Experimentos de torque compartilhado e de combustão/conversor conservam o replay completo e fronteiras explícitas de pesquisa. O caminho reduzido omite o giro dos planetas; hidráulica detalhada, controle AT e topologia/calibração OEM continuam inacabados. A [opção resolvida](RESOLVED_PLANETS.pt-BR.md) acrescenta quatro engrenamentos reais, dois rotores de giro absoluto e inércia orbital explícita; referências independentes de massa de seis rotores, momento angular e captura conservam essas energias. Comportamento detalhado de dente/lubrificação/divisão de carga ainda precisa de evidência.

O [controlador DCT amostrado](DCT_CONTROL.pt-BR.md) agora é dono dos comandos de tração/seletor, pré-seleciona caminhos sem carga, espera a sincronização/travamento físicos e faz liberação/engate exclusivos e escalonados. Solicitações inteiras de marcha, aborto para ponto morto, falhas de direção/tempo esgotado/perda persistente de travamento e recuperação por nova solicitação são observáveis. A marcha confirmada pode ficar temporariamente em zero durante um deslizamento transitório, mesmo depois de uma confirmação anterior. Corridas longas de marcha controlada usam coordenadas compensadas transacionais, com as tolerâncias estritas de fase inalteradas. O limite explícito de estado é 128; os limites de nó e de componente continuam 32/64, o que permite a composição de 70 estados de combustão/controlador. Trocas completas com mistura de torque, atuadores e falhas abrangentes de ECU/TCU continuam abertos.

O [incremento de embreagem dupla](DUAL_CLUTCH_TRANSMISSION.pt-BR.md) agora monta sete caminhos à frente, uma intermediária de ré no caminho par, três ramos de saída/redução final e seletores de atrito explícitos. Referências independentes de inércia com sinal/refletida e de impulso/calor de pré-seleção verificam os caminhos de potência. Experimentos de torque e de combustão fazem replay por todas as camadas ordinárias de grafo/asset/agente. Uma alternativa limitada de travamento linear normalizado resolve a entrega seis/sete que falhava antes, mantendo as trajetórias existentes como regressões. Seleção/entrega prescritas não são uma TCU completa nem comportamento detalhado de garras/anel de bloqueio/atuador; parâmetros e amostras OEM continuam não verificados.

O [incremento de compensação de fechamento](CLOSURE_PREDICTION.pt-BR.md) reproduz um futuro limitado da planta com entradas mantidas, sem confirmar estado. Ele prevê o escoamento residual da agulha e agenda a remoção da tensão na grade de ticks físicos. O rastreamento isolado da dose melhora, enquanto o fechamento/ricochete reais e os históricos de combustível/energia continuam os mesmos mecanismos físicos. Predições somente leitura, limites inteiros, refinamento de horizonte, zero alocações e transações completas estão verificados. A predição mantém os outros comandos e omite eventos futuros de entrada externa; seu modelo e o horizonte finito são limites explícitos, e não calibração nem aceitação completa de ECU.

O [incremento da agulha](NEEDLE_ACTUATION.pt-BR.md) acopla a energia do fluxo magnético e a força recíproca à massa real da agulha, mola/amortecimento e batentes elásticos. A amostragem inteira é dona da tensão da bobina a partir da realimentação da dose entregue. O fluido continua governado pelo levantamento físico através do atraso de fechamento e do ricochete; não é cortado no valor alvo. Referências independentes de magnético/RL/movimento, balanços de fonte/fase/elétrico/térmico, replay portátil/MCP e transações completas do controlador passam. A entrega em excesso e o líquido restante na fronteira do experimento continuam observáveis; esses resultados não estabelecem rastreamento calibrado de dose nem eletrônica/magnética completas do injetor.

O [incremento de injeção líquida](LIQUID_FUEL_INJECTION.pt-BR.md) agora parte de um filme seco e retira de uma fonte flexível finita. Pressão/trabalho analíticos do trilho, refinamento simultâneo independente, balanços completos de fonte/filme/químico/térmico, replay MCP real e rollback especulativo de embreagem passam. A energia de pressão do trilho é armazenada; o calor do bico e o trabalho de pressão exportado do receptor continuam distintos. Capacidade geométrica, ventilação/espaço gasoso/oscilação, cavitação, enchimento/eficiência/regulação medidos e spray resolvido seguem abertos. Parâmetros `unverified`; Unity Editor/Play/Player/IL2CPP real e calibração OEM seguem não verificados.

O `film-fired-cylinder` contém inicialmente um inventário líquido declarado. Ele aquece e evapora esse inventário antes da reação prescrita; não implementa um injetor líquido. Marcadores e testes preparados do Studio consomem as mesmas definições. `POWER_UNITY_EDITOR` não está definido, então Editor/Play reais, renderização e Player/IL2CPP continuam não verificados.

Todos os parâmetros de pesquisa continuam `unverified`. EA211 DJS + DQ200 e PSA EC5 + AT8 conservam suas fronteiras completas de powertrain e os manifestos de evidência em [assets/samples](../assets/samples). Medições OEM ausentes continuam ausentes. Licenças e a procedência histórica das fontes são preservadas.

## Próxima sequência de desenvolvimento

1. Estender o tanque finito com capacidade geométrica, dinâmica de ventilação/espaço gasoso, enchimento/regulação medidos e acionamento magnético/eletrônico refinado. Substitua a fronteira declarada de trabalho de deslocamento exportado quando o volume líquido finito e o momento da pulverização estiverem resolvidos. Mantenha o líquido entregue, o combustível evaporado e a reação observáveis em separado e conserve referências independentes.
2. Estenda o motor com controle de ignição, dinâmica de admissão/escape, perdas mecânicas e termoquímica mais rica. Conserve o objetivo completo do motor.
3. Estenda os caminhos de pesquisa DCT verificados com acionamento detalhado, propriedades/perdas medidas dos planetas e hidráulica/controle completos da AT, e então construa a coordenação de troca/torque de ECU/TCU a partir dos primitivos verificados de engrenagem, embreagem, conversor e hidráulica. Acrescente estado limitado de controlador, comportamento de sensor/atuador e recuperação de falha.
4. Execute `unity-test` com o Editor fixado e depois obtenha evidência de Player/IL2CPP. Complete a seleção de canais, a edição de grafo e a gravação como recursos distintos.
5. Obtenha mapas medidos, dados OEM e orçamentos de incerteza para os dois powertrains alvo antes de declarar amostras calibradas ou prontidão de lançamento.

## Realimentação de AT hidráulica

`at_controller` aceita uma marcha solicitada inteira em [-1,4]; zero indica neutro. Ele controla cinco pares de válvulas de enchimento/drenagem e o bloqueio opcional do conversor. A ordem é entrada do portasatélites, solar pequeno, solar grande, freio do portasatélites, freio do solar grande e bloqueio.

`controlled-hydraulic-ravigneaux` e `controlled-fired-hydraulic-ravigneaux` usam o canal 900 e o ID 1400. Preservam 99 e 122 estados relatados dentro do limite inalterado de 128. v27 preserva rotas, ganhos e relógios e lê v1-v26.

São controles de pesquisa e os parâmetros continuam `unverified`. Coordenação de torque ECU, sensores/válvulas detalhados, falhas completas do veículo e calibração OEM permanecem pendentes. Verificações managed e Standard não comprovam aceitação real Unity Editor/Play/Player/IL2CPP.

[AT_CONTROL.pt-BR.md](AT_CONTROL.pt-BR.md)

## Rail de combustível líquido alimentado por bomba

`liquid_rail_feed` associa um injetor líquido a uma bomba volumétrica existente e a uma fronteira explícita de matéria/calor. O nó de saída hidráulica deve corresponder à complacência e pressão absoluta inicial do rail. Bomba e injetor possuem esse nó; outras rotas fluidas não contabilizadas são rejeitadas.

v27 preserva conexões e temperatura da fonte e lê v1-v26. Troca analítica eixo/pressão, refinamento ODE simultâneo independente, mistura térmica, balanços massa/combustível/energia/volume, retorno e rollback completo têm verificações separadas.

Capacidade geométrica, ventilação/espaço gasoso/oscilação, cavitação, enchimento/eficiência/regulação medidos e spray resolvido seguem abertos. Parâmetros `unverified`; Unity Editor/Play/Player/IL2CPP real e calibração OEM seguem não verificados.

[PUMP_FED_FUEL.pt-BR.md](PUMP_FED_FUEL.pt-BR.md)

## Tanque finito de combustível líquido

`liquid_fuel_tank` guarda massa líquida finita e energia térmica com densidade, referência térmica do filme e poder calorífico do injetor associado. A alimentação o escolhe por `tank_component` e omite `supply_temperature`. Cada tanque pertence a uma alimentação compatível.

Energias térmica e química do tanque entram no armazenamento completo. Transferência interna não adiciona massa ou energia química externa. A pressão de entrada prescrita mantém sua fronteira de trabalho de pressão. Admissão/escape gasoso ainda podem transportar energia química.

Capacidade geométrica, ventilação/espaço gasoso/oscilação, cavitação, enchimento/eficiência/regulação medidos e spray resolvido seguem abertos. Parâmetros `unverified`; Unity Editor/Play/Player/IL2CPP real e calibração OEM seguem não verificados.

[LIQUID_FUEL_TANK.pt-BR.md](LIQUID_FUEL_TANK.pt-BR.md)
