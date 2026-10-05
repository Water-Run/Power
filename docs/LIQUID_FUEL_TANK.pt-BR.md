# Tanque finito de combustível líquido

[English](LIQUID_FUEL_TANK.md) · [简体中文](LIQUID_FUEL_TANK.zh-CN.md) · [Français](LIQUID_FUEL_TANK.fr.md) · [Русский](LIQUID_FUEL_TANK.ru.md) · [日本語](LIQUID_FUEL_TANK.ja.md) · [한국어](LIQUID_FUEL_TANK.ko.md) · [Deutsch](LIQUID_FUEL_TANK.de.md) · [Español](LIQUID_FUEL_TANK.es.md) · [Italiano](LIQUID_FUEL_TANK.it.md) · **Português**

## Contrato

`liquid_fuel_tank` guarda massa líquida finita e energia térmica com densidade, referência térmica do filme e poder calorífico do injetor associado. A alimentação o escolhe por `tank_component` e omite `supply_temperature`. Cada tanque pertence a uma alimentação compatível.

A vazão positiva é limitada pelo inventário restante no intervalo aceito. O mesmo deslocamento efetivo preenchido define reação do eixo e transferência de pressão, conservando trabalho eixo/fluido. Rotação adiante vazia não entrega líquido ou trabalho fluido; retorno com sinal mistura a energia térmica atual do rail no tanque.

Energias térmica e química do tanque entram no armazenamento completo. Transferência interna não adiciona massa ou energia química externa. A pressão de entrada prescrita mantém sua fronteira de trabalho de pressão. Admissão/escape gasoso ainda podem transportar energia química.

`finite-tank-liquid-cylinder` e `finite-tank-needle-cylinder` usam tanque ID 1513 e alimentação ID 1511. Leia `mass`, `temperature`, `internal_energy`, `chemical_energy` e `tank_state`; 0 indica líquido e 1 vazio. A temperatura seca informa a referência inicial declarada.

## Evidências e limites

v27 preserva tanque e seleção e lê v1-v26. Cada tanque acrescenta 4 estados dentro dos limites mantidos. Troca úmida independente, pressão/energia do eixo esgotadas analíticas, mistura de retorno, balanços completos, rollback, ramos e passos sem alocação são verificados.

Capacidade geométrica, ventilação/espaço gasoso/oscilação, cavitação, enchimento/eficiência/regulação medidos e spray resolvido seguem abertos. Parâmetros `unverified`; Unity Editor/Play/Player/IL2CPP real e calibração OEM seguem não verificados.

[VALIDATION.pt-BR.md](VALIDATION.pt-BR.md)
