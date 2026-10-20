# Équilibre du réservoir à volume fini

[English](TANK_PHASE_EQUILIBRIUM.md) · [简体中文](TANK_PHASE_EQUILIBRIUM.zh-CN.md) · **Français** · [Русский](TANK_PHASE_EQUILIBRIUM.ru.md) · [日本語](TANK_PHASE_EQUILIBRIUM.ja.md) · [한국어](TANK_PHASE_EQUILIBRIUM.ko.md) · [Deutsch](TANK_PHASE_EQUILIBRIUM.de.md) · [Español](TANK_PHASE_EQUILIBRIUM.es.md) · [Italiano](TANK_PHASE_EQUILIBRIUM.it.md) · [Português](TANK_PHASE_EQUILIBRIUM.pt-BR.md)

## Contrat

Activer `liquid_fuel_tank.parameters.headspace.phase_equilibrium` avec `true` pour résoudre évaporation et condensation du réservoir. Le film associé fournit les propriétés `phase_equilibrium` ; l'espace gazeux suivi partage R, cv, pouvoir calorifique et rapport stœchiométrique. Déclarer une température initiale commune et une pression partielle saturée pour un réservoir humide ; un réservoir sec commence au plus à saturation. La densité totale reste inférieure à celle du liquide.

`evaporated_fuel_mass`, `condensed_fuel_mass` et `mass_flow` signé cumulent le transfert net par pas physique complet. Lire ensemble `vapor_partial_pressure`, `saturation_pressure` corrigée, inventaire liquide et volume gazeux. La pompe aspire seulement le liquide séparé ; transport diphasique pompe/rail, entraînement et NPSH local restent ouverts.

## Référence thermodynamique

Le récipient rigide contient un liquide incompressible et un gaz idéal avec pseudo-espèce et porteur. Le volume liquide modifie le volume gazeux. Le potentiel chimique liquide inclut `p/rho`, d'où une correction de la référence `p_0(T)` à déplacement négligeable du récepteur. `reference_pressure` garde ce sens et n'est pas réinterprété comme saturation réelle du fluide pur à densité finie.

[Équilibre des phases du carburant et limites d'alimentation](FUEL_PHASE_EQUILIBRIUM.fr.md)

```text
M_f = m_l + m_v
V_g = V_tank - m_l / rho_l
p = (M_c + m_v) R T / V_g
p_v = m_v R T / V_g
ln(p_v / p_0(T)) = p / (rho_l R T)
U = (M_c + m_v) cv T + m_l (cl T + a)
```

Un flash borné à U,V constants conserve carburant total et énergie thermique. Les orifices explicites transportent vapeur, porteur et énergie chimique ; les liens thermiques échangent avec des parois finies. Pompe/retour échangent le travail de déplacement avec le gaz, puis l'équilibre redistribue l'énergie interne. `hydraulic_work` compte le travail de déplacement de pompe, hors travail interne de changement de phase.

## Preuves et limites

`equilibrium-tank-supply` exerce inversion de pompe, évent avec carburant et paroi chauffée finie. `equilibrium-tank-fired-cylinder` relie cette source au moteur à combustion. Maximisation indépendante d'entropie, potentiels chimiques, dérivées caloriques, raffinement ODE, bilans, rejeu, retour arrière et allocations sont vérifiés. Asset v32 conserve le drapeau et lit v1-v31.

Les exemples couplés pompe/chaleur/évent montrent un raffinement du premier ordre. Les deux phases sources restent dans l'intervalle thermique déclaré et sous la densité liquide avec marge d'arrondi binary64. Densité et propriétés gazeuses constantes, équilibre instantané et interface plane sont des approximations de recherche. Mélanges réels, tension superficielle, ballottement, cinétique finie, calibration mesurée et Unity Editor/Play/Player/IL2CPP réel restent non vérifiés.

[Cantera: constant-volume species](https://cantera.org/stable/cxx/da/d94/PDSS__ConstVol_8cpp_source.html) · [IUPAC: Poynting correction](https://goldbook.iupac.org/terms/view/15347) · [VALIDATION.fr.md](VALIDATION.fr.md)
