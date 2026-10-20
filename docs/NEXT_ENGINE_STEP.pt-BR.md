# Notas de retomada do desenvolvimento do motor

[English](NEXT_ENGINE_STEP.md) · [简体中文](NEXT_ENGINE_STEP.zh-CN.md) · [Français](NEXT_ENGINE_STEP.fr.md) · [Русский](NEXT_ENGINE_STEP.ru.md) · [日本語](NEXT_ENGINE_STEP.ja.md) · [한국어](NEXT_ENGINE_STEP.ko.md) · [Deutsch](NEXT_ENGINE_STEP.de.md) · [Español](NEXT_ENGINE_STEP.es.md) · [Italiano](NEXT_ENGINE_STEP.it.md) · **Português**

## Ponto de retomada

Câmaras finitas trocam trabalho de pressão com eixos e enchem/entregam por restrições unidirecionais ideais, com mudança de fase sob entrada insuficiente. Injetor físico e receptor ainda precisam de conexão.

[Bomba de combustível com mudança de fase acionada por eixo](FUEL_CHAMBER_PUMP.pt-BR.md)

## Próximo incremento

Conectar a bomba a injetor físico e receptores cilindro/filme com transporte bifásico, deslocamento e trabalho cinético explícitos. Resolver inércia de entrada e escoamento crítico com referências independentes. Continuar ignição, admissão/escape, perdas, atuação de transmissão e ECU/TCU rumo ao objetivo completo.

## Aceitação

Exigir referências analíticas/limite independentes, balanços completos de massa/energia e refinamento apropriado. Preservar rollback do lote, cancelamento, ramos, canais estáveis, replay portável e leitores antigos. Continuar depois ignição, admissão/escape, perdas mecânicas, transmissão e coordenação ECU/TCU.

Parâmetros permanecem `unverified`. Temperatura, pressão e volume representável são limitados durante todo o ciclo. Assume-se líquido incompressível, vapor ideal de pseudoespécie, equilíbrio instantâneo em temperatura comum e pressão externa de carcaça zero. Inércia local/NPSH, escoamento crítico, atraso de nucleação, inércia/histerese de válvula, misturas reais e calibração medida seguem abertos. Injetor físico e trabalho de deslocamento/cinética do receptor são separados.

[DEVELOPMENT_STATUS.pt-BR.md](DEVELOPMENT_STATUS.pt-BR.md) · [ROADMAP.pt-BR.md](ROADMAP.pt-BR.md) · [VALIDATION.pt-BR.md](VALIDATION.pt-BR.md)
