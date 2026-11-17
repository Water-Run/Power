# Bocais de combustível compressível com título congelado

[English](COMPRESSIBLE_FUEL_NOZZLE.md) · [简体中文](COMPRESSIBLE_FUEL_NOZZLE.zh-CN.md) · [Français](COMPRESSIBLE_FUEL_NOZZLE.fr.md) · [Русский](COMPRESSIBLE_FUEL_NOZZLE.ru.md) · [日本語](COMPRESSIBLE_FUEL_NOZZLE.ja.md) · [한국어](COMPRESSIBLE_FUEL_NOZZLE.ko.md) · [Deutsch](COMPRESSIBLE_FUEL_NOZZLE.de.md) · [Español](COMPRESSIBLE_FUEL_NOZZLE.es.md) · [Italiano](COMPRESSIBLE_FUEL_NOZZLE.it.md) · **Português**

## Contrato físico

Um `compressible_fuel_nozzle` leva fluxo unidirecional entre reservatórios finitos `compressible_fuel`, incluindo câmaras acionadas por eixo. Compressão líquida e vapor ideal compartilham o material. Declare `phase_mode` como `frozen`: o título mássico fica constante no trânsito e os reservatórios se reequilibram.

Declare área, coeficiente de área efetiva e `wall_kinetic_fraction`, com parede térmica finita. `initial_input` é abertura em [0,1], alterável por canal opcional. Os opcionais `needle_node`, `closed_position` e `full_open_position` multiplicam o comando pela abertura real da agulha translacional. As portas têm velocidade média zero, mesmo material e poder calorífico e não podem ser células internas. Pressão reversa fecha a fronteira ideal sem limiar de abertura.

## Energia e bloqueio sônico

As fases compartilham pressão, temperatura e velocidade; a transferência mássica de fase é suprimida. Entropia e energia de compressão fria determinam a queda isentrópica de entalpia. O máximo único de fluxo usa som congelado, diferente do som de equilíbrio. O coeficiente escala área efetiva. No bloqueio, a energia de saída descreve um jato ideal totalmente expandido à pressão receptora. A entalpia de estagnação paga uma vez a energia receptora e o calor explícito da parede. O impulso mede termalização estacionária, não empuxo total da carcaça ou força axial na agulha.

```text
cp_f = (1-x) cl + x (cv+R); alpha = x R/cp_f
T(p) = T0 (p/p0)^alpha
v(p) = (1-x)/[rho0 (1+p/K)] + x R T(p)/p
Delta_s = cp_f ln(T/T0) - x R ln(p/p0) = 0
D(p) = h0-h(p)
     = cp_f T0 [1-(p/p0)^alpha] + (1-x) K/rho0 ln[(K+p0)/(K+p)]
G(p) = sqrt(2 D(p))/v(p)
c_frozen^2 = -v(p)^2 / (dv/dp at fixed s,x)
F(p) = v(p)^2 + 2 D(p) v'(p); F'(p) = 2 D(p) v''(p) > 0
G_throat = max(G(p), p_receiver <= p <= p0)
mass_flow = opening Cd A G_throat
Delta_E_A = -Delta_m h0
Delta_E_B = Delta_m h0 - xi Delta_m D(p_receiver)
Delta_E_wall = xi Delta_m D(p_receiver)
```

Subpassos conservativos de primeira ordem transferem igual massa e entalpia e dividem a energia cinética entre fluido e parede. Verificam-se estados e entropia total do par. Limites: 4096 subpassos, 64 reduções à metade por candidato e transferência inicial de até 0.1 da menor massa. O fechamento permite 128 epsilon binary64 vezes o maior módulo volumétrico ou pressão, sem limitar pressões informadas. Falhas revertem todo o lote. Examine passo, inventário, limites, curso e área.

## Uso e evidências

Execute `compressible-fuel-nozzle` para descarga finita ou `compressible-pump-needle` para duto, eixo, entrada unidirecional e agulha solenoide. Leia massa, entalpia, energia cinética, calor, impulso e entropia acumulados. Fluxo e força médios abrangem o tick inteiro; garganta, saída e título retêm o último trânsito aceito. Comparam-se trabalho líquido exato, bloqueio analítico de gás/líquido, quadratura independente, entropia e derivada sonora congeladas, varredura densa e refinamento do esvaziamento crítico analítico. Conservação, movimento real, reprodução, ramos, reversão, cancelamento, alocações, assets e MCP são verificados separadamente.

[VALIDATION.pt-BR.md](VALIDATION.pt-BR.md)

## Validade e próximos passos

Os parâmetros continuam `unverified`. O pseudofluido puro usa vapor ideal e líquido compressível sem expansão térmica. Respeite pressão e temperatura declaradas, incluindo o jato ideal. O trânsito congelado pode manter líquido superaquecido sem prever limites medidos de nucleação ou metaestabilidade. Relaxação finita, deslizamento, spray resolvido, gás portador não condensável, receptor com combustão, mapas medidos e Unity real permanecem abertos. Conservação e reprodução não demonstram validade de hardware.

Resolver residência finita no bocal com evidências do material, depois adicionar spray resolvido e receptores com combustão compatíveis. Continuar ignição, admissão/escape, perdas mecânicas, atuação da transmissão e coordenação ECU/TCU. Calibração medida e aceitação real do Unity seguem separadas.

[NASA: injector flow and finite phase-transfer times](https://ntrs.nasa.gov/api/citations/20190001326/downloads/20190001326.pdf) · [NIST: thermodynamic derivatives](https://pages.nist.gov/teqp-docs/en/latest/derivs/derivs.html) · [Lund: two-phase relaxation hierarchy](https://www.sintef.no/globalassets/project/co2-dynamics/publications/lund_two-phase_relaxation_hierarchy.pdf)
