# Geometria do tanque e espaço gasoso finito

[English](TANK_HEADSPACE.md) · [简体中文](TANK_HEADSPACE.zh-CN.md) · [Français](TANK_HEADSPACE.fr.md) · [Русский](TANK_HEADSPACE.ru.md) · [日本語](TANK_HEADSPACE.ja.md) · [한국어](TANK_HEADSPACE.ko.md) · [Deutsch](TANK_HEADSPACE.de.md) · [Español](TANK_HEADSPACE.es.md) · [Italiano](TANK_HEADSPACE.it.md) · **Português**

## Contrato

`liquid_fuel_tank.parameters.headspace` declara `capacity` em `m3` ou `l` e `gas_node`. O gás omite `storage`: volume `capacity - liquid_mass / density`, um proprietário e volume positivo. Bomba e retorno usam pressão prescrita nula: o gás finito determina a pressão de entrada.

O solver acoplado troca trabalho de pressão entre eixo, trilho e gás sem fonte externa. Orifícios gasosos e conexões térmicas fornecem ventilação/calor explícitos. Ler `pressure`, `fill_fraction`, `hydraulic_work` acumulado com sinal e massa, energia, volume do gás. Dose, líquido, evaporação e combustão continuam separados.

## Evidência e limites

`vented-tank-liquid-cylinder` e `vented-tank-needle-cylinder` usam tanque 1513, gás 1520 e entrada de ventilação 960. Asset v32 guarda geometria e lê v1-v31. Passam trabalho/derivadas analíticos, convergência ODE independente, balanços, replay portable/MCP, rollback e passos sem alocação.

Tanque rígido misturado, líquido incompressível e gás ideal. Slosh/forma hidrostática, termodinâmica de misturas reais, cavitação, mapas medidos bomba/válvula, calibração OEM e Unity Editor/Play/Player/IL2CPP real seguem abertos. Parâmetros `unverified`.

[NASA](https://www.grc.nasa.gov/www/k-12/airplane/isentrop.html) · [Cantera](https://www.cantera.org/stable/reference/reactors/interactions.html) · [VALIDATION.md](VALIDATION.pt-BR.md)

## Equilíbrio do tanque com volume líquido finito

Definir `liquid_fuel_tank.parameters.headspace.phase_equilibrium` como `true` para resolver evaporação e condensação do tanque. O filme pareado fornece propriedades `phase_equilibrium`; o gás rastreado compartilha R, cv, poder calorífico e estequiometria. Tanque molhado declara temperatura inicial comum e pressão parcial saturada; tanque seco começa no máximo em saturação. A densidade total fica abaixo da líquida.

`evaporated_fuel_mass`, `condensed_fuel_mass` e `mass_flow` com sinal contam transferência líquida de fase por passo físico. Ler juntos `vapor_partial_pressure`, `saturation_pressure` corrigida, inventário líquido e volume gasoso. A bomba capta líquido separado; transporte bifásico, arraste de bolhas e NPSH local seguem abertos.

[Equilíbrio do tanque com volume líquido finito](TANK_PHASE_EQUILIBRIUM.pt-BR.md)
