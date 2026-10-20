# Rail de combustível líquido alimentado por bomba

[English](PUMP_FED_FUEL.md) · [简体中文](PUMP_FED_FUEL.zh-CN.md) · [Français](PUMP_FED_FUEL.fr.md) · [Русский](PUMP_FED_FUEL.ru.md) · [日本語](PUMP_FED_FUEL.ja.md) · [한국어](PUMP_FED_FUEL.ko.md) · [Deutsch](PUMP_FED_FUEL.de.md) · [Español](PUMP_FED_FUEL.es.md) · [Italiano](PUMP_FED_FUEL.it.md) · **Português**

## Contrato

`liquid_rail_feed` associa um injetor líquido a uma bomba volumétrica existente e a uma fronteira explícita de matéria/calor. O nó de saída hidráulica deve corresponder à complacência e pressão absoluta inicial do rail. Bomba e injetor possuem esse nó; outras rotas fluidas não contabilizadas são rejeitadas.

A energia de pressão é armazenada uma única vez no nó hidráulico. Vazão e reação do eixo seguem a solução acoplada conservativa. Combustível entrante traz energia térmica e química; o armazenamento térmico do rail mistura a temperatura. O fluxo reverso com sinal devolve combustível à temperatura atual do rail. Descarga, aquecimento da parede, vapor e combustão prescrita permanecem separados.

`pump-fed-liquid-cylinder` e `pump-fed-needle-cylinder` preservam injeção física e movimento opcional da agulha. Os KPI de pressão usam um limite declarado da bomba com unidades explícitas. Leia `total_fuel_delivered`, `reservoir_enthalpy` e `fuel_energy_in` no ID 1511; a bomba ID 1510 mostra o trabalho real eixo-fluido.

## Evidências e limites

v34 preserva conexões e temperatura da fonte e lê v1-v33. Troca analítica eixo/pressão, refinamento ODE simultâneo independente, mistura térmica, balanços massa/combustível/energia/volume, retorno e rollback completo têm verificações separadas.

Tanque rígido misturado, líquido incompressível e gás ideal. Slosh/forma hidrostática, termodinâmica de misturas reais, cavitação, mapas medidos bomba/válvula, calibração OEM e Unity Editor/Play/Player/IL2CPP real seguem abertos. Parâmetros `unverified`.

## Tanque finito de combustível líquido

`liquid_fuel_tank` guarda massa líquida finita e energia térmica com densidade, referência térmica do filme e poder calorífico do injetor associado. A alimentação o escolhe por `tank_component` e omite `supply_temperature`. Cada tanque pertence a uma alimentação compatível.

Energias térmica e química do tanque entram no armazenamento completo. Transferência interna não adiciona massa ou energia química externa. A pressão de entrada prescrita mantém sua fronteira de trabalho de pressão. Admissão/escape gasoso ainda podem transportar energia química.

Tanque rígido misturado, líquido incompressível e gás ideal. Slosh/forma hidrostática, termodinâmica de misturas reais, cavitação, mapas medidos bomba/válvula, calibração OEM e Unity Editor/Play/Player/IL2CPP real seguem abertos. Parâmetros `unverified`.

[LIQUID_FUEL_TANK.pt-BR.md](LIQUID_FUEL_TANK.pt-BR.md)

## Retorno de alívio de combustível rastreado

`liquid_rail_return` associa alimentação a um `hydraulic_relief` unidirecional exclusivo. A válvula liga o rail à mesma pressão de entrada prescrita da bomba. Registrar cada rota; portas incompatíveis, propriedade duplicada e rotas não rastreadas são rejeitadas.

`fluid_heat_fraction` escolhe explicitamente a fração [0,1] da perda transportada pelo combustível retornado. O restante segue a rota térmica declarada. Mistura simultânea rail/tanque conserva massa, química, trabalho de pressão e calor. Retorno à fonte externa leva massa/energia pela fronteira.

[LIQUID_FUEL_RETURN.pt-BR.md](LIQUID_FUEL_RETURN.pt-BR.md)

## Geometria do tanque e espaço gasoso finito

`liquid_fuel_tank.parameters.headspace` declara `capacity` em `m3` ou `l` e `gas_node`. O gás omite `storage`: volume `capacity - liquid_mass / density`, um proprietário e volume positivo. Bomba e retorno usam pressão prescrita nula: o gás finito determina a pressão de entrada.

O solver acoplado troca trabalho de pressão entre eixo, trilho e gás sem fonte externa. Orifícios gasosos e conexões térmicas fornecem ventilação/calor explícitos. Ler `pressure`, `fill_fraction`, `hydraulic_work` acumulado com sinal e massa, energia, volume do gás. Dose, líquido, evaporação e combustão continuam separados.

[Geometria do tanque e espaço gasoso finito](TANK_HEADSPACE.pt-BR.md)

## Equilíbrio de fases do combustível e limites de suprimento

O suprimento padrão rejeita líquido sob saturação. O tanque finito opcional resolve vapor da fonte com potenciais corrigidos por pressão. Trilho e captação ainda levam líquido separado; bolhas e fluxo cavitante precisam de mais física.

A comparação só admite arredondamento binary64: `64 * epsilon * max(p, p_sat)`, com `epsilon = 2^-52`. A margem informada não é limitada. É uma verificação de saturação global; perdas locais e NPSH da bomba exigem modelo de fluxo resolvido. As verificações independentes também exigem entropia total não decrescente no flash adiabático e na troca com parede finita.

[Equilíbrio de fases do combustível e limites de suprimento](FUEL_PHASE_EQUILIBRIUM.pt-BR.md)

## Equilíbrio do tanque com volume líquido finito

Definir `liquid_fuel_tank.parameters.headspace.phase_equilibrium` como `true` para resolver evaporação e condensação do tanque. O filme pareado fornece propriedades `phase_equilibrium`; o gás rastreado compartilha R, cv, poder calorífico e estequiometria. Tanque molhado declara temperatura inicial comum e pressão parcial saturada; tanque seco começa no máximo em saturação. A densidade total fica abaixo da líquida.

`evaporated_fuel_mass`, `condensed_fuel_mass` e `mass_flow` com sinal contam transferência líquida de fase por passo físico. Ler juntos `vapor_partial_pressure`, `saturation_pressure` corrigida, inventário líquido e volume gasoso. A bomba capta líquido separado; transporte bifásico, arraste de bolhas e NPSH local seguem abertos.

[Equilíbrio do tanque com volume líquido finito](TANK_PHASE_EQUILIBRIUM.pt-BR.md)

O transporte agora compõe com `fuel_chamber` por eixo e `fuel_check_valve` ideal. Injetor, inércia/escoamento crítico e misturas reais medidas seguem separados. Os parâmetros são `unverified`. [Bomba de combustível com mudança de fase acionada por eixo](FUEL_CHAMBER_PUMP.pt-BR.md)
