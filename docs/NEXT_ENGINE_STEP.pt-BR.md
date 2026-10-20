# Notas de retomada do desenvolvimento do motor

[English](NEXT_ENGINE_STEP.md) · [简体中文](NEXT_ENGINE_STEP.zh-CN.md) · [Français](NEXT_ENGINE_STEP.fr.md) · [Русский](NEXT_ENGINE_STEP.ru.md) · [日本語](NEXT_ENGINE_STEP.ja.md) · [한국어](NEXT_ENGINE_STEP.ko.md) · [Deutsch](NEXT_ENGINE_STEP.de.md) · [Español](NEXT_ENGINE_STEP.es.md) · [Italiano](NEXT_ENGINE_STEP.it.md) · **Português**

## Ponto de retomada

O tanque resolve vapor de volume finito com potenciais corrigidos e captação líquida separada.

[Equilíbrio do tanque com volume líquido finito](TANK_PHASE_EQUILIBRIUM.pt-BR.md)

## Próximo incremento

Resolver depois fluxo bifásico e perdas locais com propriedades explícitas e acoplamento conservativo de volume, momento e trabalho. Manter ausentes as medições OEM faltantes. Continuar separadamente ignição, admissão/escape, perdas mecânicas e ECU/TCU.

## Aceitação

Exigir referências analíticas/limite independentes, balanços completos de massa/energia e refinamento apropriado. Preservar rollback do lote, cancelamento, ramos, canais estáveis, replay portável e leitores antigos. Continuar depois ignição, admissão/escape, perdas mecânicas, transmissão e coordenação ECU/TCU.

Casos acoplados de bomba, calor e ventilação demonstram primeira ordem. As duas fases da fonte ficam no intervalo declarado; densidade total abaixo da líquida com margem binary64. Densidade/propriedades constantes, equilíbrio instantâneo e interface plana são aproximações de pesquisa. Misturas reais, tensão superficial, sloshing, cinética finita, calibração medida e Unity Editor/Play/Player/IL2CPP real seguem não verificados.

[DEVELOPMENT_STATUS.pt-BR.md](DEVELOPMENT_STATUS.pt-BR.md) · [ROADMAP.pt-BR.md](ROADMAP.pt-BR.md) · [VALIDATION.pt-BR.md](VALIDATION.pt-BR.md)
