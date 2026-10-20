# Notas de retomada do desenvolvimento do motor

[English](NEXT_ENGINE_STEP.md) · [简体中文](NEXT_ENGINE_STEP.zh-CN.md) · [Français](NEXT_ENGINE_STEP.fr.md) · [Русский](NEXT_ENGINE_STEP.ru.md) · [日本語](NEXT_ENGINE_STEP.ja.md) · [한국어](NEXT_ENGINE_STEP.ko.md) · [Deutsch](NEXT_ENGINE_STEP.de.md) · [Español](NEXT_ENGINE_STEP.es.md) · [Italiano](NEXT_ENGINE_STEP.it.md) · **Português**

## Ponto de retomada

A bomba do domínio de combustível agora alimenta agulha física e descarga de título congelado em um receptor em combustão com volume líquido finito. Os inventários completos incluem mudança de fase, trabalho de deslocamento, energia cinética e impulso na parede imóvel.

[Bomba de combustível com mudança de fase acionada por eixo](FUEL_CHAMBER_PUMP.pt-BR.md)

## Próximo incremento

Resolver inércia e perdas locais de entrada, sensibilidade à relaxação de fase e deslizamento; depois ignição, admissão/escape, perdas mecânicas e ECU/TCU coordenados. Cada equação exige referências independentes e refinamento temporal; calibração medida e aceitação Unity real são separadas.

## Aceitação

Exigir referências analíticas/limite independentes, balanços completos de massa/energia e refinamento apropriado. Preservar rollback do lote, cancelamento, ramos, canais estáveis, replay portável e leitores antigos. Continuar depois ignição, admissão/escape, perdas mecânicas, transmissão e coordenação ECU/TCU.

Os parâmetros seguem `unverified`. Respeite temperaturas declaradas, volume gasoso positivo e domínio de vapor diluído. Faltam inércia local de entrada, nucleação finita, deslizamento de fases, spray resolvido, comportamento crítico real e calibração medida. Combustão prescrita não comprova equilíbrio químico ou entropia de reação. Continue entrada, ignição, admissão/escape, perdas e coordenação ECU/TCU.

[DEVELOPMENT_STATUS.pt-BR.md](DEVELOPMENT_STATUS.pt-BR.md) · [ROADMAP.pt-BR.md](ROADMAP.pt-BR.md) · [VALIDATION.pt-BR.md](VALIDATION.pt-BR.md)

[Descarga de combustível com título congelado e receptores finitos](FUEL_DISCHARGE.pt-BR.md)
