# Notas de retomada do desenvolvimento do motor

[English](NEXT_ENGINE_STEP.md) · [简体中文](NEXT_ENGINE_STEP.zh-CN.md) · [Français](NEXT_ENGINE_STEP.fr.md) · [Русский](NEXT_ENGINE_STEP.ru.md) · [日本語](NEXT_ENGINE_STEP.ja.md) · [한국어](NEXT_ENGINE_STEP.ko.md) · [Deutsch](NEXT_ENGINE_STEP.de.md) · [Español](NEXT_ENGINE_STEP.es.md) · [Italiano](NEXT_ENGINE_STEP.it.md) · **Português**

## Ponto de retomada

Recipientes de combustível trocam misturas líquido/vapor por restrições dissipativas conservativas e elos térmicos finitos. Enchimento de bomba, eixo e descarga de injetor ainda precisam de conexão.

[Rede de transporte de combustível em equilíbrio](EQUILIBRIUM_FUEL_NETWORK.pt-BR.md)

## Próximo incremento

Conectar volumes de combustível a enchimento de bomba por eixo e descarga física de injetor com acoplamento conservativo de matéria, quantidade de movimento e trabalho. Resolver inércia local de entrada e escoamento crítico com referências independentes. Continuar ignição, admissão/escape, perdas, atuação de transmissão e ECU/TCU sem considerar esta rede um trem de força completo.

## Aceitação

Exigir referências analíticas/limite independentes, balanços completos de massa/energia e refinamento apropriado. Preservar rollback do lote, cancelamento, ramos, canais estáveis, replay portável e leitores antigos. Continuar depois ignição, admissão/escape, perdas mecânicas, transmissão e coordenação ECU/TCU.

Os limites de material e deformação representável do fechamento continuam válidos. Parâmetros permanecem `unverified`. Enchimento de bomba e trabalho de eixo, conexão de injetor, inércia local, escoamento crítico, atraso de nucleação, deslizamento e medidas de misturas reais seguem separados. Os rails de injetor alimentados por bomba mantêm o caminho líquido separado até o acoplamento.

[DEVELOPMENT_STATUS.pt-BR.md](DEVELOPMENT_STATUS.pt-BR.md) · [ROADMAP.pt-BR.md](ROADMAP.pt-BR.md) · [VALIDATION.pt-BR.md](VALIDATION.pt-BR.md)
