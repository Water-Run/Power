# Câmaras de combustível compressível acionadas por eixo

[English](COMPRESSIBLE_FUEL_CHAMBER.md) · [简体中文](COMPRESSIBLE_FUEL_CHAMBER.zh-CN.md) · [Français](COMPRESSIBLE_FUEL_CHAMBER.fr.md) · [Русский](COMPRESSIBLE_FUEL_CHAMBER.ru.md) · [日本語](COMPRESSIBLE_FUEL_CHAMBER.ja.md) · [한국어](COMPRESSIBLE_FUEL_CHAMBER.ko.md) · [Deutsch](COMPRESSIBLE_FUEL_CHAMBER.de.md) · [Español](COMPRESSIBLE_FUEL_CHAMBER.es.md) · [Italiano](COMPRESSIBLE_FUEL_CHAMBER.it.md) · **Português**

## Contrato físico

Uma `compressible_fuel_chamber` conecta um eixo a um reservatório finito `compressible_fuel`. O movimento muda o volume; o mesmo trabalho de pressão muda energia do fluido e torque. O reservatório pode alimentar ou receber um `equilibrium_fuel_duct`, com vaporização súbita e condensação em equilíbrio.

`node_a` é rotacional e `node_b` tem um único proprietário geométrico. Declare `minimum_reference_volume`, `swept_volume` e `phase`; o volume inicial deve corresponder ao ângulo inicial. O reservatório misturado tem velocidade média zero e não pode ser célula interna. O mecanismo senoidal sem massa não acrescenta inércia de pistão ou came; a inércia do eixo é explícita. A pressão externa da carcaça é zero.

## Trabalho e transporte

O passo fechado de trabalho mantém massa e entropia. Energia de compressão fria e energia calórica usam o potencial de equilíbrio do duto. O líquido puro usa a integral exata da pressão e o vapor ideal sua adiabática analítica. A mudança de fase inverte entropia e verifica a energia final. A diferença aceita fornece torque discreto na solução mecânica conjunta. `hydraulic_work` positivo significa energia do eixo para o fluido.

```text
V(theta) = Vmin + Vs sin^2((theta-phase)/2)
dE_fluid = -p dV; dM = 0; dS = 0
tau_pressure = -Delta_E_fluid / Delta_theta
tau_pressure(Delta_theta=0) = p dV/dtheta
Delta_E_shaft + Delta_E_fluid = 0
p_liquid = K (M/(rho0 V)-1)
Delta_E_liquid = K [Delta_V - (M/rho0) ln(V_next/V_old)]
T_vapor_next = T_old (V_old/V_next)^(R/cv)
Delta_E_vapor = M cv (T_next-T_old)
```

O reservatório móvel usa meio transporte, trabalho completo do eixo e meio transporte. O transporte continua de primeira ordem com difusão numérica; a separação simétrica não eleva a ordem. O movimento líquido fechado e suave converge em segunda ordem. Incrementos angulares são limitados a 0.25 rad por intervalo mecânico. CFL, limites materiais, reversão completa, cancelamento e ramificações independentes continuam válidos. O impulso estacionário das portas/carcaça é separado do trabalho do pistão.

## Uso e evidências

Execute `compressible-fuel-chamber-duct` ou `electric-compressible-fuel-duct`. Leia massa, volume, pressão, temperatura, entropia e fração de vapor junto a células, torque, potência e trabalho. As verificações usam trabalho líquido e limites de vapor independentes, isentrópica de equilíbrio invertida separadamente e quadratura da pressão, EDO do eixo líquido e refinamento RK4 das equações semidiscretas do vapor ideal. Conservação, composição elétrica/engrenagens/hidráulica/embreagem, reprodução portável, MCP real e transações sem alocação são verificados separadamente. Os resultados estão nas evidências de validação.

[VALIDATION.pt-BR.md](VALIDATION.pt-BR.md)

## Validade e trabalho restante

Os parâmetros continuam `unverified`. O pseudofluido puro em equilíbrio usa líquido compressível sem expansão térmica e vapor ideal, câmara misturada e células rígidas. Respeite limites térmicos e de pressão. A câmara não resolve energia cinética média nem inclui válvula de retenção automática. Nucleação com taxa finita, deslizamento, comportamento crítico real, calibração medida e aceitação real no Unity permanecem abertos.

Adicionar fronteiras compatíveis de válvula direcional e agulha/bocal compressíveis, com entalpia, energia cinética e hipóteses de fase explícitas. Depois avançar relaxação/deslizamento, ignição, admissão/escape, perdas mecânicas e coordenação ECU/TCU. Calibração medida e aceitação real no Unity permanecem separadas.

[Modelica: swept volume and pressure work](https://doc.modelica.org/Modelica%204.0.0/Resources/helpWSM/Modelica/Modelica.Fluid.Machines.SweptVolume.html) · [NIST: consistent thermodynamic derivatives](https://pages.nist.gov/teqp-docs/en/latest/derivs/derivs.html)
