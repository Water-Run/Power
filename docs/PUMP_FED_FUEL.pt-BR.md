# Rail de combustível líquido alimentado por bomba

[English](PUMP_FED_FUEL.md) · [简体中文](PUMP_FED_FUEL.zh-CN.md) · [Français](PUMP_FED_FUEL.fr.md) · [Русский](PUMP_FED_FUEL.ru.md) · [日本語](PUMP_FED_FUEL.ja.md) · [한국어](PUMP_FED_FUEL.ko.md) · [Deutsch](PUMP_FED_FUEL.de.md) · [Español](PUMP_FED_FUEL.es.md) · [Italiano](PUMP_FED_FUEL.it.md) · **Português**

## Contrato

`liquid_rail_feed` associa um injetor líquido a uma bomba volumétrica existente e a uma fronteira explícita de matéria/calor. O nó de saída hidráulica deve corresponder à complacência e pressão absoluta inicial do rail. Bomba e injetor possuem esse nó; outras rotas fluidas não contabilizadas são rejeitadas.

A energia de pressão é armazenada uma única vez no nó hidráulico. Vazão e reação do eixo seguem a solução acoplada conservativa. Combustível entrante traz energia térmica e química; o armazenamento térmico do rail mistura a temperatura. O fluxo reverso com sinal devolve combustível à temperatura atual do rail. Descarga, aquecimento da parede, vapor e combustão prescrita permanecem separados.

`pump-fed-liquid-cylinder` e `pump-fed-needle-cylinder` preservam injeção física e movimento opcional da agulha. Os KPI de pressão usam um limite declarado da bomba com unidades explícitas. Leia `total_fuel_delivered`, `reservoir_enthalpy` e `fuel_energy_in` no ID 1511; a bomba ID 1510 mostra o trabalho real eixo-fluido.

## Evidências e limites

v26 preserva conexões e temperatura da fonte e lê v1-v25. Troca analítica eixo/pressão, refinamento ODE simultâneo independente, mistura térmica, balanços massa/combustível/energia/volume, retorno e rollback completo têm verificações separadas.

A fonte é uma fronteira externa explícita, não um tanque finito modelado. Esgotamento, eficiência/regulação da bomba, perdas de linhas, cavitação, propriedades dependentes da pressão e spray de volume finito seguem abertos. Parâmetros `unverified`; isso não comprova calibração OEM ou aceitação real Unity Editor/Play/Player/IL2CPP.
