# Descarga de combustível com título congelado e receptores finitos

[English](FUEL_DISCHARGE.md) · [简体中文](FUEL_DISCHARGE.zh-CN.md) · [Français](FUEL_DISCHARGE.fr.md) · [Русский](FUEL_DISCHARGE.ru.md) · [日本語](FUEL_DISCHARGE.ja.md) · [한국어](FUEL_DISCHARGE.ko.md) · [Deutsch](FUEL_DISCHARGE.de.md) · [Español](FUEL_DISCHARGE.es.md) · [Italiano](FUEL_DISCHARGE.it.md) · **Português**

## Modelo físico

Um `fuel_discharge` conecta uma fonte finita do domínio de combustível ao nó gasoso de um `finite_fuel_film`. Declare `phase_mode` como `frozen`: o título mássico de vapor fica fixo na passagem pelo bocal, enquanto os volumes das extremidades voltam ao equilíbrio. As fases homogêneas compartilham temperatura e velocidade; líquido incompressível e vapor como pseudoespécie ideal. Propriedades e densidade devem coincidir.

Para pressão e temperatura de estagnação a montante, maximize este fluxo mássico entre pressão receptora e da fonte. O único máximo interno limita o escoamento estrangulado. Líquido e vapor puros resultam em Bernoulli e na relação sônica do gás ideal. O coeficiente de descarga escala a área efetiva da garganta.

```text
x = upstream vapor mass fraction; r = p/p0
cp = (1-x) cl + x (cv+R); k = x R/cp; b = (1-x)/rho_l
T(p) = T0 r^k
h0-h(p) = cp T0 (1-r^k) + b p0 (1-r)
v(p) = b + x R T(p)/p
G(p) = sqrt(2 (h0-h(p))) / v(p)
mass_rate = opening Cd A max(G)
jet_kinetic_energy_per_kg = h0-h(p_receiver)
gas_volume = cavity_volume - liquid_mass/rho_l
```

## Energia, volume e quantidade de movimento

A fonte perde a entalpia de estagnação transportada. `wall_kinetic_fraction` atribui parte da energia cinética do jato ideal totalmente expandido à parede finita imóvel; o restante entra no fluido receptor. A velocidade de referência supõe expansão ideal até a pressão receptora e não prevê uma velocidade de saída medida. O impulso chega ao receptor estacionário, com reação oposta na carcaça; essas reações não fazem trabalho nem acrescentam força ao pistão ou agulha.

Declare `finite_volume_density`. O `volume` gasoso é a cavidade menos massa líquida/densidade; o `volume` do filme é o deslocamento líquido. O cilindro móvel calcula trabalho com massa e entropia constantes do receptor inteiro. O trabalho de deslocamento é interno. Cavidades fixas e cilindros de manivela são suportados; pistões gasosos lineares não.

## Uso e evidências

Execute `finite-volume-fuel-discharge` para estrangulamento de fonte bifásica, ou `pump-fed-finite-volume-cylinder` para bomba elétrica, agulha eletromagnética física e combustão prescrita. `needle` e `valve_timing` opcionais multiplicam a abertura. O exemplo usa pulsos explícitos de tensão sem massa solicitada por ciclo ou controle de dose amostrado. Leia `jet_kinetic_energy`, `jet_impulse`, `force`, `throat_pressure`, `exit_temperature`, `exit_velocity` e `choked` com os inventários completos. Os quatro últimos mantêm o último estado de fluxo aceito quando a vazão para.

Limites independentes de Bernoulli e gás ideal, maximização separada do fluxo bifásico, energia/entropia e ODE independente bocal/receptor verificam as equações. O refinamento temporal é de primeira ordem. Replay portátil, MCP real, rollback, cancelamento, ramos independentes e alocações verificam a integração. Unity Editor/Play/Player/IL2CPP real segue sem verificação.

## Validade e próximos trabalhos

Os parâmetros seguem `unverified`. Respeite temperaturas declaradas, volume gasoso positivo e domínio de vapor diluído. Faltam inércia local de entrada, nucleação finita, deslizamento de fases, spray resolvido, comportamento crítico real e calibração medida. Combustão prescrita não comprova equilíbrio químico ou entropia de reação. Continue entrada, ignição, admissão/escape, perdas e coordenação ECU/TCU.

[NASA: ideal-gas mass-flow choking](https://www.grc.nasa.gov/www/k-12/BGP/mflchk.html) · [NASA-CR-64098: two-phase mass-limiting experiments](https://ntrs.nasa.gov/citations/19650020869) · [VALIDATION.md](VALIDATION.md)
