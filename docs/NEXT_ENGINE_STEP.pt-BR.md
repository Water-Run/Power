# Notas de retomada do desenvolvimento do motor

[English](NEXT_ENGINE_STEP.md) · [简体中文](NEXT_ENGINE_STEP.zh-CN.md) · [Français](NEXT_ENGINE_STEP.fr.md) · [Русский](NEXT_ENGINE_STEP.ru.md) · [日本語](NEXT_ENGINE_STEP.ja.md) · [한국어](NEXT_ENGINE_STEP.ko.md) · [Deutsch](NEXT_ENGINE_STEP.de.md) · [Español](NEXT_ENGINE_STEP.es.md) · [Italiano](NEXT_ENGINE_STEP.it.md) · **Português**

## Ponto de retomada

O receptor suporta equilíbrio reversível e verificações de saturação do suprimento.

[Equilíbrio de fases do combustível e limites de suprimento](FUEL_PHASE_EQUILIBRIUM.pt-BR.md)

## Próximo incremento

Resolver em seguida transferência de vapor do tanque e fluxo bifásico bomba/trilho com propriedades explícitas e acoplamento conservativo volume/trabalho. Manter ausentes as medições OEM faltantes. Ignição, admissão/escape, perdas mecânicas e coordenação ECU/TCU continuam trabalhos separados.

## Aceitação

Exigir referências analíticas/limite independentes, balanços completos de massa/energia e refinamento apropriado. Preservar rollback do lote, cancelamento, ramos, canais estáveis, replay portável e leitores antigos. Continuar depois ignição, admissão/escape, perdas mecânicas, transmissão e coordenação ECU/TCU.

Parâmetros `unverified`. Não são fornecidas propriedades medidas de gasolina, cinética multicomponente ou de velocidade finita, deslocamento líquido/spray resolvidos ou calibração OEM. A aceitação real Unity Editor/Play/Player/IL2CPP continua pendente.

[DEVELOPMENT_STATUS.pt-BR.md](DEVELOPMENT_STATUS.pt-BR.md) · [ROADMAP.pt-BR.md](ROADMAP.pt-BR.md) · [VALIDATION.pt-BR.md](VALIDATION.pt-BR.md)
