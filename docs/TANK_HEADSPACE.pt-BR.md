# Geometria do tanque e espaço gasoso finito

[English](TANK_HEADSPACE.md) · [简体中文](TANK_HEADSPACE.zh-CN.md) · [Français](TANK_HEADSPACE.fr.md) · [Русский](TANK_HEADSPACE.ru.md) · [日本語](TANK_HEADSPACE.ja.md) · [한국어](TANK_HEADSPACE.ko.md) · [Deutsch](TANK_HEADSPACE.de.md) · [Español](TANK_HEADSPACE.es.md) · [Italiano](TANK_HEADSPACE.it.md) · **Português**

## Contrato

`liquid_fuel_tank.parameters.headspace` declara `capacity` em `m3` ou `l` e `gas_node`. O gás omite `storage`: volume `capacity - liquid_mass / density`, um proprietário e volume positivo. Bomba e retorno usam pressão prescrita nula: o gás finito determina a pressão de entrada.

O solver acoplado troca trabalho de pressão entre eixo, trilho e gás sem fonte externa. Orifícios gasosos e conexões térmicas fornecem ventilação/calor explícitos. Ler `pressure`, `fill_fraction`, `hydraulic_work` acumulado com sinal e massa, energia, volume do gás. Dose, líquido, evaporação e combustão continuam separados.

## Evidência e limites

`vented-tank-liquid-cylinder` e `vented-tank-needle-cylinder` usam tanque 1513, gás 1520 e entrada de ventilação 960. Asset v29 guarda geometria e lê v1-v28. Passam trabalho/derivadas analíticos, convergência ODE independente, balanços, replay portable/MCP, rollback e passos sem alocação.

Tanque rígido misturado, líquido incompressível e gás ideal. Slosh/forma hidrostática, equilíbrio de fases, cavitação, mapas medidos bomba/válvula, calibração OEM e Unity Editor/Play/Player/IL2CPP real seguem abertos. Parâmetros `unverified`.

[NASA](https://www.grc.nasa.gov/www/k-12/airplane/isentrop.html) · [Cantera](https://www.cantera.org/stable/reference/reactors/interactions.html) · [VALIDATION.md](VALIDATION.pt-BR.md)
