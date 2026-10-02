# Sincronização amostrada de embreagem dupla e entrega escalonada

[English](DCT_CONTROL.md) · [简体中文](DCT_CONTROL.zh-CN.md) · [Français](DCT_CONTROL.fr.md) · [Русский](DCT_CONTROL.ru.md) · [日本語](DCT_CONTROL.ja.md) · [한국어](DCT_CONTROL.ko.md) · [Deutsch](DCT_CONTROL.de.md) · [Español](DCT_CONTROL.es.md) · [Italiano](DCT_CONTROL.it.md) · **Português**

`dct_controller` é dono dos dois canais de embreagem de tração e dos oito canais de seletor de um [grafo de pesquisa de sete marchas à frente/ré](DUAL_CLUTCH_TRANSMISSION.pt-BR.md). O comando inteiro de marcha pedida é separado da marcha real confirmada, dos caminhos selecionados, da fase da troca, do erro de sincronização medido e da falha do controlador.

Esta é uma máquina de estados de pesquisa guiada por sensores, com uma interrupção explícita de torque. Ela não estabelece a mescla completa de torque TCU/ECU, o comportamento detalhado de dente de engate, anel de bloqueio ou atuador de embreagem, uma estratégia de troca calibrada nem a gestão completa de falhas do veículo.

## Estados e confirmação física

| Fase | Política de comando retido e transição |
|---|---|
| Ponto morto | As duas embreagens de tração e os seletores, liberados |
| Preparação | O caminho oposto alvo está descarregado e pré-selecionado enquanto a tração precedente permanece engatada |
| Liberação | A abertura da tração precedente desce em rampa; a tração alvo permanece liberada |
| Sincronização | O seletor alvo sobe em rampa até a abertura plena, com as duas trações liberadas; espera o deslizamento medido e a trava física |
| Engate | A tração alvo sobe em rampa; a outra tração permanece liberada |
| Condução | Tração/seletor alvo confirmados; a pré-seleção do caminho descarregado adjacente é permitida |
| Falha | As duas trações e todos os seletores, liberados; a falha é retida até o ponto morto ou um pedido diferente |

O alvo fica retido enquanto uma entrega está em curso. Pedidos posteriores que não sejam ponto morto são processados depois dessa entrega; o ponto morto aborta numa amostra vencida. Mudanças no mesmo caminho de entrada liberam a tração dele antes de mudar os seletores. Mudanças no caminho de entrada oposto podem preparar o alvo descarregado antes de liberar a tração. Cada caminho comanda no máximo um seletor, e não se usa sobreposição comandada das embreagens de tração.

As rampas de abertura do seletor usam a duração de engate configurada. Um seletor só fica pronto depois do comando pleno, de deslizamento medido dentro da tolerância fornecida e do modo físico `Locked`. A tração só é confirmada depois do comando de engate pleno, de deslizamento pequeno da tração e da trava física. Aceitar o comando não anuncia uma relação instantânea nem a conclusão física da marcha.

A pré-seleção inativa pode perturbar por pouco tempo um caminho confirmado. A marcha real do instantâneo é zero enquanto a tração ou o caminho selecionado não está fisicamente travado. A perda persistente é cronometrada à parte; o controlador não confunde uma amostra transitória com uma falha sustentada. Esse temporizador reinicia nas mudanças de fase e na recuperação.

## Marcha pedida, sentido e falhas

A marcha pedida é um inteiro em `[-1,7]`, com zero como ponto morto e -1 como ré. A validação estática, imediata e programada da entrada rejeita frações. O comando de origem usa unidades explícitas `state_code`; nenhuma fração ordinária de embreagem é interpretada como número de marcha.

O tempo esgotado de sincronização devolve uma falha observável e descarregada. Um pedido de ré contra movimento positivo do veículo acima do limite de velocidade fornecido, ou um pedido à frente contra movimento negativo, é bloqueado como falha de mudança de sentido. A perda sustentada de uma trava confirmada de tração/seletor usa o mesmo tempo limite fornecido e um código de falha distinto. Esses desfechos do controlador são estados de política física, não falhas numéricas nem KPIs implícitos de troca bem-sucedida.

| Código de falha | Significado |
|---:|---|
| 0 | Sem falha |
| 1 | Tempo esgotado de sincronização/engate |
| 2 | Pedido de mudança de sentido bloqueado pelo movimento do veículo |
| 3 | Perda persistente da trava confirmada |

O ponto morto limpa a falha e libera o trem. Um pedido válido diferente pode iniciar uma nova tentativa; submeter de novo o mesmo alvo que falhou não reinicia o tempo limite a cada amostra. Decisões de falha de nível mais alto, verificações de plausibilidade, falhas de sensor e funções de segurança do condutor/veículo continuam trabalho separado.

## Definição e posse

O controlador declara o nó A do motor, o nó do veículo, os IDs das embreagens de tração ímpar/par, oito seletores na ordem 1-7 à frente/ré e a entrada de marcha pedida. Os dez canais de atuador precisam ser distintos, inicialmente liberados e ter um único dono. O compilador verifica os tipos ordinários de embreagem, a topologia de eixo/cubo/redução final, a atribuição ímpar/par, o caminho da intermediária de ré e as referências estáveis. As listas de seletores são copiadas para dados imutáveis de definição/compilação.

O tempo explícito consiste em nanossegundos de amostra, liberação, engate e tempo limite de sincronização. A amostragem alinha aos ticks físicos; os outros tempos são múltiplos positivos da amostra e no máximo dez segundos. A tolerância de sincronização e o limite de velocidade de sentido usam `rad_s` ou `rpm`. Nenhum valor OEM, mapa de atuador ou curva de perda é fornecido em silêncio.

Os agentes escrevem a marcha pedida. Escritas diretas de tração/seletor devolvem `controlled_input` com o nome/canal de comando correto e deixam revisão/estado inalterados. As leituras expõem o pedido ao vivo, a marcha confirmada, as seleções ímpar/par comandadas, a fase, o deslizamento do seletor alvo e a falha. Esses códigos de estado e canais físicos conservam semânticas diferentes.

## Relógios inteiros e transações completas

As amostras correm no tempo inteiro limitado da simulação. Escritas de entrada não avançam a memória de controle. As frações retidas são aplicadas ao solver físico normal; inércia, reações de engrenagem, sincronização e calor de tração ficam nos livros existentes. O estado do controlador contém marcha retida/ativa, seleções, fase/falha, relógio de fase, erro medido e temporizador de trava persistente. Ramificações, cancelamento, lotes que falham tarde e intervalos especulativos copiam/hasheiam/revertem essa memória e cada comando retido juntos. O avanço bem-sucedido e os instantâneos não alocam memória gerenciada.

Execuções longas de marcha controlada usam incrementos de coordenada compensados a partir da velocidade de ponto médio. A compensação é transacional e hasheada; as tolerâncias estritas de fase permanecem inalteradas. Isso resolve o arredondamento acumulado exposto pelo novo cenário longo de sincronização sob carga. Os caminhos de modelo anteriores conservam a integração e o comportamento de replay precedentes.

O limite de estado informado é explicitamente **128**, com 32 nós e 64 componentes inalterados. Isso permite a composição completa de pesquisa em combustão/DCT/controlador, que excede o limite anterior de 64 estados. Compilação/avanço no limite exato e modelos físicos/de controlador acima do limite são conferidos; compilações e testes continuam em série.

## Experimentos portáteis e compartilhados

O asset v22 acrescenta um registro tipado de 104 bytes de rota/tempo/tolerância por controlador DCT. Conserva leitores v1–v21, IDs precedentes estáveis e contagem/comprimento limitados, digest, posse tipada, unidades e verificações de compilação física. Modelos de controlador acrescentam a etiqueta de impressão digital 26. Os campos pedido/real/seleção/fase/erro/falha são anexados sem mudar os IDs precedentes. O fixture autêntico de grafo v21 conserva o digest e o replay atualizado no mesmo runtime.

`controlled-dual-clutch` emite pedidos de marcha por todos os sete caminhos e descidas selecionadas. Observa a entrega final e a pré-seleção inativa até a conclusão física, em vez de assumir um tempo nominal. `controlled-fired-dual-clutch` combina a mesma política amostrada com combustão premisturada de cilindro aberto. JSON, CLI, replay portátil e um servidor filho MCP real compartilham as definições.

[VALIDATION.pt-BR.md](VALIDATION.pt-BR.md) registra estado/intertravamento, falha/recuperação, posse, entrada inteira, rota imutável, capacidade, preservação de fase longa, conservação e evidência de replay completo. Todos os parâmetros permanecem `unverified`. Mescla de torque, comportamento completo de atuador/sensor/ECU, AT completo, Unity real e powertrains alvo calibrados continuam inacabados.
