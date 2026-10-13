# Retorno de alívio de combustível rastreado

[English](LIQUID_FUEL_RETURN.md) · [简体中文](LIQUID_FUEL_RETURN.zh-CN.md) · [Français](LIQUID_FUEL_RETURN.fr.md) · [Русский](LIQUID_FUEL_RETURN.ru.md) · [日本語](LIQUID_FUEL_RETURN.ja.md) · [한국어](LIQUID_FUEL_RETURN.ko.md) · [Deutsch](LIQUID_FUEL_RETURN.de.md) · [Español](LIQUID_FUEL_RETURN.es.md) · [Italiano](LIQUID_FUEL_RETURN.it.md) · **Português**

## Contrato

`liquid_rail_return` associa alimentação a um `hydraulic_relief` unidirecional exclusivo. A válvula liga o rail à mesma pressão de entrada prescrita da bomba. Registrar cada rota; portas incompatíveis, propriedade duplicada e rotas não rastreadas são rejeitadas.

`fluid_heat_fraction` escolhe explicitamente a fração [0,1] da perda transportada pelo combustível retornado. O restante segue a rota térmica declarada. Mistura simultânea rail/tanque conserva massa, química, trabalho de pressão e calor. Retorno à fonte externa leva massa/energia pela fronteira.

Leia `total_fuel_delivered`, `reservoir_enthalpy`, `fuel_energy_in`, `fluid_heat` e `mass_flow` no retorno ID 1515. Alimentação ID 1511 informa transferência bruta da bomba. Circulação bruta pode exceder estoque inicial; estoque atual é inicial menos bombeamento mais retorno.

## Evidências e limites

`recirculating-liquid-cylinder` e `recirculating-needle-cylinder` mantêm combustível finito, injeção real, evaporação e agulha opcional. v30 guarda conexões/fração e lê v1-v29. Cada retorno acrescenta 8 estados nos mesmos limites.

Passam decaimento/trabalho independentes, refinamento mecânica/pressão/térmico simultâneo, frações, rotas, fronteiras externas, replay, rollback e alocações. Ebulição ou intervalo não resolvido falha o lote inteiro.

Tanque rígido misturado, líquido incompressível e gás ideal. Slosh/forma hidrostática, equilíbrio de fases, cavitação, mapas medidos bomba/válvula, calibração OEM e Unity Editor/Play/Player/IL2CPP real seguem abertos. Parâmetros `unverified`.

[VALIDATION.pt-BR.md](VALIDATION.pt-BR.md)

## Geometria do tanque e espaço gasoso finito

`liquid_fuel_tank.parameters.headspace` declara `capacity` em `m3` ou `l` e `gas_node`. O gás omite `storage`: volume `capacity - liquid_mass / density`, um proprietário e volume positivo. Bomba e retorno usam pressão prescrita nula: o gás finito determina a pressão de entrada.

[Geometria do tanque e espaço gasoso finito](TANK_HEADSPACE.pt-BR.md)

## Equilíbrio de fases do combustível e limites de suprimento

Trilhos, tanques finitos e entradas expõem `saturation_pressure` e `cavitation_margin` em Pa. A margem é pressão absoluta local menos saturação na temperatura líquida. Margem negativa com líquido rejeita todo o lote. Isso delimita o suprimento monofásico; transferência de vapor do tanque, bolhas e fluxo cavitante da bomba continuam abertos.

A comparação só admite arredondamento binary64: `64 * epsilon * max(p, p_sat)`, com `epsilon = 2^-52`. A margem informada não é limitada. É uma verificação de saturação global; perdas locais e NPSH da bomba exigem modelo de fluxo resolvido. As verificações independentes também exigem entropia total não decrescente no flash adiabático e na troca com parede finita.

[Equilíbrio de fases do combustível e limites de suprimento](FUEL_PHASE_EQUILIBRIUM.pt-BR.md)
