# Notas de retomada do desenvolvimento do motor

[English](NEXT_ENGINE_STEP.md) · [简体中文](NEXT_ENGINE_STEP.zh-CN.md) · [Français](NEXT_ENGINE_STEP.fr.md) · [Русский](NEXT_ENGINE_STEP.ru.md) · [日本語](NEXT_ENGINE_STEP.ja.md) · [한국어](NEXT_ENGINE_STEP.ko.md) · [Deutsch](NEXT_ENGINE_STEP.de.md) · [Español](NEXT_ENGINE_STEP.es.md) · [Italiano](NEXT_ENGINE_STEP.it.md) · **Português**

## Ponto de retomada

A bomba do domínio de combustível agora alimenta agulha física e descarga de título congelado em um receptor em combustão com volume líquido finito. Os inventários completos incluem mudança de fase, trabalho de deslocamento, energia cinética e impulso na parede imóvel.

[Bomba de combustível com mudança de fase acionada por eixo](FUEL_CHAMBER_PUMP.pt-BR.md)

`LiquidColumn` é uma referência C# independente para tubo horizontal rígido com seção, velocidade uniforme, densidade líquida e temperatura constantes. As pressões absolutas são prescritas por intervalo. Resistências linear e quadrática são entradas de pesquisa explícitas. Não é componente do grafo e não altera rede ou bocal. [Referência de quantidade de movimento na entrada líquida](LIQUID_COLUMN.pt-BR.md)

Um `fuel_inertial_pipe` conecta dois nós finitos de combustível e uma parede térmica finita. A linha horizontal rígida possui massa líquida, energia calórica e cinética próprias além dos vasos. Densidade constante e temperatura variável perfeitamente misturada. Portas e linha ficam líquidas; conexões quase estáticas mantêm seu modelo bifásico separado. [Quantidade de movimento da linha finita de combustível líquido](FUEL_INERTIAL_PIPE.pt-BR.md)

Um `equilibrium_fuel_duct` transporta massa, quantidade de movimento e energia total por células explícitas `compressible_fuel`. O líquido é compressível e o vapor ideal; as fases compartilham pressão, temperatura e velocidade. O equilíbrio local permite condensação e vaporização súbita. Dois reservatórios finitos estacionários e uma parede térmica finita fecham os balanços. [Dutos de combustível compressível em equilíbrio](EQUILIBRIUM_FUEL_DUCT.pt-BR.md)

## Próximo incremento

Conecte dutos compressíveis a câmaras acionadas por eixo e fronteiras de agulha/bocal compatíveis, com trabalho de pressão e hipóteses de fase explícitos. Depois avance relaxação/deslizamento, ignição, admissão/escape, perdas mecânicas e coordenação ECU/TCU. Calibração medida e aceitação real no Unity permanecem separadas. [Dutos de combustível compressível em equilíbrio](EQUILIBRIUM_FUEL_DUCT.pt-BR.md)

## Aceitação

Exigir referências analíticas/limite independentes, balanços completos de massa/energia e refinamento apropriado. Preservar rollback do lote, cancelamento, ramos, canais estáveis, replay portável e leitores antigos. Continuar depois ignição, admissão/escape, perdas mecânicas, transmissão e coordenação ECU/TCU.

Os parâmetros continuam `unverified`. Respeite as faixas declaradas de temperatura e pressão, entalpia latente positiva, volumes de coexistência separados e vapor diluído. O duto é rígido, horizontal, de seção constante e equilíbrio homogêneo instantâneo. Não inclui nucleação com taxa finita, deslizamento, tensão superficial, gravidade, elasticidade ou comportamento crítico real. A difusão de primeira ordem pode alargar frentes; conservação e reprodução não demonstram precisão espacial nem validade de hardware medido. A aceitação real no Unity permanece pendente.

[DEVELOPMENT_STATUS.pt-BR.md](DEVELOPMENT_STATUS.pt-BR.md) · [ROADMAP.pt-BR.md](ROADMAP.pt-BR.md) · [VALIDATION.pt-BR.md](VALIDATION.pt-BR.md)

[Descarga de combustível com título congelado e receptores finitos](FUEL_DISCHARGE.pt-BR.md)
