# Retorno de alívio de combustível rastreado

[English](LIQUID_FUEL_RETURN.md) · [简体中文](LIQUID_FUEL_RETURN.zh-CN.md) · [Français](LIQUID_FUEL_RETURN.fr.md) · [Русский](LIQUID_FUEL_RETURN.ru.md) · [日本語](LIQUID_FUEL_RETURN.ja.md) · [한국어](LIQUID_FUEL_RETURN.ko.md) · [Deutsch](LIQUID_FUEL_RETURN.de.md) · [Español](LIQUID_FUEL_RETURN.es.md) · [Italiano](LIQUID_FUEL_RETURN.it.md) · **Português**

## Contrato

`liquid_rail_return` associa alimentação a um `hydraulic_relief` unidirecional exclusivo. A válvula liga o rail à mesma pressão de entrada prescrita da bomba. Registrar cada rota; portas incompatíveis, propriedade duplicada e rotas não rastreadas são rejeitadas.

`fluid_heat_fraction` escolhe explicitamente a fração [0,1] da perda transportada pelo combustível retornado. O restante segue a rota térmica declarada. Mistura simultânea rail/tanque conserva massa, química, trabalho de pressão e calor. Retorno à fonte externa leva massa/energia pela fronteira.

Leia `total_fuel_delivered`, `reservoir_enthalpy`, `fuel_energy_in`, `fluid_heat` e `mass_flow` no retorno ID 1515. Alimentação ID 1511 informa transferência bruta da bomba. Circulação bruta pode exceder estoque inicial; estoque atual é inicial menos bombeamento mais retorno.

## Evidências e limites

`recirculating-liquid-cylinder` e `recirculating-needle-cylinder` mantêm combustível finito, injeção real, evaporação e agulha opcional. v36 guarda conexões/fração e lê v1-v35. Cada retorno acrescenta 8 estados nos mesmos limites.

Passam decaimento/trabalho independentes, refinamento mecânica/pressão/térmico simultâneo, frações, rotas, fronteiras externas, replay, rollback e alocações. Ebulição ou intervalo não resolvido falha o lote inteiro.

Tanque rígido misturado, líquido incompressível e gás ideal. Slosh/forma hidrostática, termodinâmica de misturas reais, cavitação, mapas medidos bomba/válvula, calibração OEM e Unity Editor/Play/Player/IL2CPP real seguem abertos. Parâmetros `unverified`.

[VALIDATION.pt-BR.md](VALIDATION.pt-BR.md)

## Geometria do tanque e espaço gasoso finito

`liquid_fuel_tank.parameters.headspace` declara `capacity` em `m3` ou `l` e `gas_node`. O gás omite `storage`: volume `capacity - liquid_mass / density`, um proprietário e volume positivo. Bomba e retorno usam pressão prescrita nula: o gás finito determina a pressão de entrada.

[Geometria do tanque e espaço gasoso finito](TANK_HEADSPACE.pt-BR.md)

## Equilíbrio de fases do combustível e limites de suprimento

O suprimento padrão rejeita líquido sob saturação. O tanque finito opcional resolve vapor da fonte com potenciais corrigidos por pressão. Trilho e captação ainda levam líquido separado; bolhas e fluxo cavitante precisam de mais física.

A comparação só admite arredondamento binary64: `64 * epsilon * max(p, p_sat)`, com `epsilon = 2^-52`. A margem informada não é limitada. É uma verificação de saturação global; perdas locais e NPSH da bomba exigem modelo de fluxo resolvido. As verificações independentes também exigem entropia total não decrescente no flash adiabático e na troca com parede finita.

[Equilíbrio de fases do combustível e limites de suprimento](FUEL_PHASE_EQUILIBRIUM.pt-BR.md)

## Equilíbrio do tanque com volume líquido finito

Definir `liquid_fuel_tank.parameters.headspace.phase_equilibrium` como `true` para resolver evaporação e condensação do tanque. O filme pareado fornece propriedades `phase_equilibrium`; o gás rastreado compartilha R, cv, poder calorífico e estequiometria. Tanque molhado declara temperatura inicial comum e pressão parcial saturada; tanque seco começa no máximo em saturação. A densidade total fica abaixo da líquida.

`evaporated_fuel_mass`, `condensed_fuel_mass` e `mass_flow` com sinal contam transferência líquida de fase por passo físico. Ler juntos `vapor_partial_pressure`, `saturation_pressure` corrigida, inventário líquido e volume gasoso. A bomba capta líquido separado; transporte bifásico, arraste de bolhas e NPSH local seguem abertos.

[Equilíbrio do tanque com volume líquido finito](TANK_PHASE_EQUILIBRIUM.pt-BR.md)
