# Equilíbrio do tanque com volume líquido finito

[English](TANK_PHASE_EQUILIBRIUM.md) · [简体中文](TANK_PHASE_EQUILIBRIUM.zh-CN.md) · [Français](TANK_PHASE_EQUILIBRIUM.fr.md) · [Русский](TANK_PHASE_EQUILIBRIUM.ru.md) · [日本語](TANK_PHASE_EQUILIBRIUM.ja.md) · [한국어](TANK_PHASE_EQUILIBRIUM.ko.md) · [Deutsch](TANK_PHASE_EQUILIBRIUM.de.md) · [Español](TANK_PHASE_EQUILIBRIUM.es.md) · [Italiano](TANK_PHASE_EQUILIBRIUM.it.md) · **Português**

## Contrato

Definir `liquid_fuel_tank.parameters.headspace.phase_equilibrium` como `true` para resolver evaporação e condensação do tanque. O filme pareado fornece propriedades `phase_equilibrium`; o gás rastreado compartilha R, cv, poder calorífico e estequiometria. Tanque molhado declara temperatura inicial comum e pressão parcial saturada; tanque seco começa no máximo em saturação. A densidade total fica abaixo da líquida.

`evaporated_fuel_mass`, `condensed_fuel_mass` e `mass_flow` com sinal contam transferência líquida de fase por passo físico. Ler juntos `vapor_partial_pressure`, `saturation_pressure` corrigida, inventário líquido e volume gasoso. A bomba capta líquido separado; transporte bifásico, arraste de bolhas e NPSH local seguem abertos.

## Referência termodinâmica

O recipiente rígido contém líquido incompressível e gás ideal de pseudoespécie/portador. O líquido muda o volume gasoso. Seu potencial químico inclui `p/rho`, corrigindo a referência `p_0(T)` de deslocamento desprezível. `reference_pressure` mantém esse significado, sem ser reinterpretada como saturação real de fluido puro com densidade finita.

[Equilíbrio de fases do combustível e limites de suprimento](FUEL_PHASE_EQUILIBRIUM.pt-BR.md)

```text
M_f = m_l + m_v
V_g = V_tank - m_l / rho_l
p = (M_c + m_v) R T / V_g
p_v = m_v R T / V_g
ln(p_v / p_0(T)) = p / (rho_l R T)
U = (M_c + m_v) cv T + m_l (cl T + a)
```

Um flash limitado a U,V constantes conserva combustível e energia térmica. Orifícios explícitos carregam vapor, portador e energia química; ligações térmicas trocam com nós finitos. Bomba/retorno trocam trabalho de deslocamento com o gás e depois o equilíbrio redistribui energia. `hydraulic_work` conta deslocamento de bomba, excluindo trabalho interno de fase.

## Evidência e limites

`equilibrium-tank-supply` verifica reversão da bomba, ventilação com combustível e parede quente finita. `equilibrium-tank-fired-cylinder` conecta a fonte a um motor com combustão. Verificam-se máximo independente de entropia, potenciais, derivadas calóricas, refinamento ODE, balanços, replay, rollback e alocações. Asset v35 mantém o sinalizador e lê v1-v34.

Casos acoplados de bomba, calor e ventilação demonstram primeira ordem. As duas fases da fonte ficam no intervalo declarado; densidade total abaixo da líquida com margem binary64. Densidade/propriedades constantes, equilíbrio instantâneo e interface plana são aproximações de pesquisa. Misturas reais, tensão superficial, sloshing, cinética finita, calibração medida e Unity Editor/Play/Player/IL2CPP real seguem não verificados.

[Cantera: constant-volume species](https://cantera.org/stable/cxx/da/d94/PDSS__ConstVol_8cpp_source.html) · [IUPAC: Poynting correction](https://goldbook.iupac.org/terms/view/15347) · [VALIDATION.pt-BR.md](VALIDATION.pt-BR.md)
