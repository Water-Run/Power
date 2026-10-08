# Tanque finito de combustível líquido

[English](LIQUID_FUEL_TANK.md) · [简体中文](LIQUID_FUEL_TANK.zh-CN.md) · [Français](LIQUID_FUEL_TANK.fr.md) · [Русский](LIQUID_FUEL_TANK.ru.md) · [日本語](LIQUID_FUEL_TANK.ja.md) · [한국어](LIQUID_FUEL_TANK.ko.md) · [Deutsch](LIQUID_FUEL_TANK.de.md) · [Español](LIQUID_FUEL_TANK.es.md) · [Italiano](LIQUID_FUEL_TANK.it.md) · **Português**

## Contrato

`liquid_fuel_tank` guarda massa líquida finita e energia térmica com densidade, referência térmica do filme e poder calorífico do injetor associado. A alimentação o escolhe por `tank_component` e omite `supply_temperature`. Cada tanque pertence a uma alimentação compatível.

A vazão positiva é limitada pelo inventário restante no intervalo aceito. O mesmo deslocamento efetivo preenchido define reação do eixo e transferência de pressão, conservando trabalho eixo/fluido. Rotação adiante vazia não entrega líquido ou trabalho fluido; retorno com sinal mistura a energia térmica atual do rail no tanque.

Energias térmica e química do tanque entram no armazenamento completo. Transferência interna não adiciona massa ou energia química externa. A pressão de entrada prescrita mantém sua fronteira de trabalho de pressão. Admissão/escape gasoso ainda podem transportar energia química.

`finite-tank-liquid-cylinder` e `finite-tank-needle-cylinder` usam tanque ID 1513 e alimentação ID 1511. Leia `mass`, `temperature`, `internal_energy`, `chemical_energy` e `tank_state`; 0 indica líquido e 1 vazio. A temperatura seca informa a referência inicial declarada.

## Geometria do tanque e espaço gasoso finito

`liquid_fuel_tank.parameters.headspace` declara `capacity` em `m3` ou `l` e `gas_node`. O gás omite `storage`: volume `capacity - liquid_mass / density`, um proprietário e volume positivo. Bomba e retorno usam pressão prescrita nula: o gás finito determina a pressão de entrada.

[Geometria do tanque e espaço gasoso finito](TANK_HEADSPACE.pt-BR.md)

## Evidências e limites

v29 preserva tanque e seleção e lê v1-v28. Cada tanque acrescenta 4 estados dentro dos limites mantidos. Troca úmida independente, pressão/energia do eixo esgotadas analíticas, mistura de retorno, balanços completos, rollback, ramos e passos sem alocação são verificados.

Tanque rígido misturado, líquido incompressível e gás ideal. Slosh/forma hidrostática, equilíbrio de fases, cavitação, mapas medidos bomba/válvula, calibração OEM e Unity Editor/Play/Player/IL2CPP real seguem abertos. Parâmetros `unverified`.

[VALIDATION.pt-BR.md](VALIDATION.pt-BR.md)

## Retorno de alívio de combustível rastreado

`liquid_rail_return` associa alimentação a um `hydraulic_relief` unidirecional exclusivo. A válvula liga o rail à mesma pressão de entrada prescrita da bomba. Registrar cada rota; portas incompatíveis, propriedade duplicada e rotas não rastreadas são rejeitadas.

`fluid_heat_fraction` escolhe explicitamente a fração [0,1] da perda transportada pelo combustível retornado. O restante segue a rota térmica declarada. Mistura simultânea rail/tanque conserva massa, química, trabalho de pressão e calor. Retorno à fonte externa leva massa/energia pela fronteira.

[LIQUID_FUEL_RETURN.pt-BR.md](LIQUID_FUEL_RETURN.pt-BR.md)
