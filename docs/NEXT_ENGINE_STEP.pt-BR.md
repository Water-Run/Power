# Notas de retomada do desenvolvimento do motor

[English](NEXT_ENGINE_STEP.md) · [简体中文](NEXT_ENGINE_STEP.zh-CN.md) · [Français](NEXT_ENGINE_STEP.fr.md) · [Русский](NEXT_ENGINE_STEP.ru.md) · [日本語](NEXT_ENGINE_STEP.ja.md) · [한국어](NEXT_ENGINE_STEP.ko.md) · [Deutsch](NEXT_ENGINE_STEP.de.md) · [Español](NEXT_ENGINE_STEP.es.md) · [Italiano](NEXT_ENGINE_STEP.it.md) · **Português**

## Ponto de retomada

O suprimento inclui tanques finitos, retornos conservativos, espaço gasoso geométrico e ventilação explícita. Bomba, trilho e gás trocam trabalho interno; líquido, evaporação e reação prescrita seguem separados. Retomar dos contratos e evidências atuais.

[Geometria do tanque e espaço gasoso finito](TANK_HEADSPACE.pt-BR.md)

## Próximo incremento

Focar o próximo incremento em equilíbrio de fases por pressão e cavitação com propriedades explícitas. Manter ausentes as medições OEM faltantes e não verificados os parâmetros de pesquisa. Enchimento/regulação medidos de bombas/válvulas, acionamento magnético/eletrônico e spray resolvido seguem pendentes.

## Aceitação

Exigir referências analíticas/limite independentes, balanços completos de massa/energia e refinamento apropriado. Preservar rollback do lote, cancelamento, ramos, canais estáveis, replay portável e leitores antigos. Continuar depois ignição, admissão/escape, perdas mecânicas, transmissão e coordenação ECU/TCU.

Tanque rígido misturado, líquido incompressível e gás ideal. Slosh/forma hidrostática, equilíbrio de fases, cavitação, mapas medidos bomba/válvula, calibração OEM e Unity Editor/Play/Player/IL2CPP real seguem abertos. Parâmetros `unverified`.

[DEVELOPMENT_STATUS.pt-BR.md](DEVELOPMENT_STATUS.pt-BR.md) · [ROADMAP.pt-BR.md](ROADMAP.pt-BR.md) · [VALIDATION.pt-BR.md](VALIDATION.pt-BR.md)
