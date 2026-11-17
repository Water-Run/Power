# Notas de retomada do desenvolvimento do motor

[English](NEXT_ENGINE_STEP.md) · [简体中文](NEXT_ENGINE_STEP.zh-CN.md) · [Français](NEXT_ENGINE_STEP.fr.md) · [Русский](NEXT_ENGINE_STEP.ru.md) · [日本語](NEXT_ENGINE_STEP.ja.md) · [한국어](NEXT_ENGINE_STEP.ko.md) · [Deutsch](NEXT_ENGINE_STEP.de.md) · [Español](NEXT_ENGINE_STEP.es.md) · [Italiano](NEXT_ENGINE_STEP.it.md) · **Português**

## Ponto de retomada

A bomba do domínio de combustível agora alimenta agulha física e descarga de título congelado em um receptor em combustão com volume líquido finito. Os inventários completos incluem mudança de fase, trabalho de deslocamento, energia cinética e impulso na parede imóvel.

[Bomba de combustível com mudança de fase acionada por eixo](FUEL_CHAMBER_PUMP.pt-BR.md)

`LiquidColumn` é uma referência C# independente para tubo horizontal rígido com seção, velocidade uniforme, densidade líquida e temperatura constantes. As pressões absolutas são prescritas por intervalo. Resistências linear e quadrática são entradas de pesquisa explícitas. Não é componente do grafo e não altera rede ou bocal. [Referência de quantidade de movimento na entrada líquida](LIQUID_COLUMN.pt-BR.md)

Um `fuel_inertial_pipe` conecta dois nós finitos de combustível e uma parede térmica finita. A linha horizontal rígida possui massa líquida, energia calórica e cinética próprias além dos vasos. Densidade constante e temperatura variável perfeitamente misturada. Portas e linha ficam líquidas; conexões quase estáticas mantêm seu modelo bifásico separado. [Quantidade de movimento da linha finita de combustível líquido](FUEL_INERTIAL_PIPE.pt-BR.md)

Um `equilibrium_fuel_duct` transporta massa, quantidade de movimento e energia total por células explícitas `compressible_fuel`. O líquido é compressível e o vapor ideal; as fases compartilham pressão, temperatura e velocidade. O equilíbrio local permite condensação e vaporização súbita. Dois reservatórios finitos estacionários e uma parede térmica finita fecham os balanços. [Dutos de combustível compressível em equilíbrio](EQUILIBRIUM_FUEL_DUCT.pt-BR.md)

Uma `compressible_fuel_chamber` conecta um eixo a um reservatório finito `compressible_fuel`. O movimento muda o volume; o mesmo trabalho de pressão muda energia do fluido e torque. O reservatório pode alimentar ou receber um `equilibrium_fuel_duct`, com vaporização súbita e condensação em equilíbrio. [Câmaras de combustível compressível acionadas por eixo](COMPRESSIBLE_FUEL_CHAMBER.pt-BR.md)

Um `compressible_fuel_nozzle` leva fluxo unidirecional entre reservatórios finitos `compressible_fuel`, incluindo câmaras acionadas por eixo. Compressão líquida e vapor ideal compartilham o material. Declare `phase_mode` como `frozen`: o título mássico fica constante no trânsito e os reservatórios se reequilibram. [Bocais de combustível compressível com título congelado](COMPRESSIBLE_FUEL_NOZZLE.pt-BR.md)

Defina `phase_mode` de `compressible_fuel_nozzle` como `equilibrium` para equilíbrio homogêneo instantâneo durante o trânsito. Líquido e vapor compartilham pressão, temperatura e velocidade; o título muda na isentrópica. Bocal e reservatórios finitos usam o mesmo potencial líquido compressível e referência de vapor ideal. `frozen` permanece uma hipótese separada suportada. [Trânsito de combustível em equilíbrio nos bocais](EQUILIBRIUM_FUEL_NOZZLE.pt-BR.md)

Um nó `relaxing_fuel` tem fração mássica de vapor independente. Líquido e vapor compartilham pressão, temperatura e velocidade, mas o equilíbrio químico exige um tempo finito declarado. Um `relaxing_fuel_duct` transporta massa, quantidade de movimento, energia total e massa de vapor em células explícitas. A residência depende da geometria e da velocidade real. [Relaxação de fase do combustível com taxa finita](RELAXING_FUEL.pt-BR.md)

## Próximo incremento

Ampliar acoplamentos de contorno compatíveis com taxa finita e obter medições de relaxação do material, depois incluir spray resolvido e receptores com combustão. Continuar ignição, admissão/escape, perdas mecânicas, atuação da transmissão e controle ECU/TCU coordenado. Calibração medida e aceitação real do Unity seguem separadas. [Dutos de combustível compressível em equilíbrio](EQUILIBRIUM_FUEL_DUCT.pt-BR.md)

## Aceitação

Exigir referências analíticas/limite independentes, balanços completos de massa/energia e refinamento apropriado. Preservar rollback do lote, cancelamento, ramos, canais estáveis, replay portável e leitores antigos. Continuar depois ignição, admissão/escape, perdas mecânicas, transmissão e coordenação ECU/TCU.

Os parâmetros continuam `unverified`. Declare `initial_vapor_mass_fraction` em [0,1] e tempo inteiro de 1 ns a uma hora. Respeite temperatura e pressão positiva do material. O modelo pressupõe duto rígido horizontal de área constante, pseudofluido puro, expansão térmica líquida nula e vapor ideal. Não inclui deslizamento de fases, atraso térmico interfacial, tensão superficial, spray ou gás portador. Acoplamentos diretos do domínio relaxante com bombas, bocais, câmaras móveis e receptores com combustão continuam pendentes. Conservação, reprodução e refinamento não comprovam validade experimental nem aceitação real do Unity. Use vapor diluído longe da região crítica real; o material deve manter entalpia latente positiva e volumes específicos de coexistência separados.

[DEVELOPMENT_STATUS.pt-BR.md](DEVELOPMENT_STATUS.pt-BR.md) · [ROADMAP.pt-BR.md](ROADMAP.pt-BR.md) · [VALIDATION.pt-BR.md](VALIDATION.pt-BR.md)

[Descarga de combustível com título congelado e receptores finitos](FUEL_DISCHARGE.pt-BR.md)
