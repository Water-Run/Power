# Équilibre des phases du carburant et limites d'alimentation

[English](FUEL_PHASE_EQUILIBRIUM.md) · [简体中文](FUEL_PHASE_EQUILIBRIUM.zh-CN.md) · **Français** · [Русский](FUEL_PHASE_EQUILIBRIUM.ru.md) · [日本語](FUEL_PHASE_EQUILIBRIUM.ja.md) · [한국어](FUEL_PHASE_EQUILIBRIUM.ko.md) · [Deutsch](FUEL_PHASE_EQUILIBRIUM.de.md) · [Español](FUEL_PHASE_EQUILIBRIUM.es.md) · [Italiano](FUEL_PHASE_EQUILIBRIUM.it.md) · [Português](FUEL_PHASE_EQUILIBRIUM.pt-BR.md)

## Contrat

L'option `fuel_film.parameters.phase_equilibrium` déclare `reference_pressure` en `pa` ou `bar` et `minimum_temperature` / `maximum_temperature` en `k`. `saturation_temperature` est la température de référence ; `latent_internal_energy` est la différence d'énergie interne vapeur-liquide à cette température. Un film possède chaque récepteur en équilibre. L'état initial déclaré est conservé ; les intervalles acceptés équilibrent gaz et liquide avec échange thermique vers la paroi finie.

`mass_flow` est le transfert net signé sur le pas physique complet. `evaporated_fuel_mass` et `condensed_fuel_mass` cumulent ses masses positive et négative, sans compter deux fois les projections qui s'annulent. `vapor_partial_pressure` donne la pression partielle du carburant. Les bilans thermiques, massiques et chimiques comprennent les deux phases.

## Référence thermodynamique

Le modèle utilise une pseudo-espèce idéale, la constante gazeuse commune du récepteur, des capacités thermiques constantes et un déplacement liquide négligeable. L'égalité des potentiels chimiques donne la relation de saturation ci-dessous. À volume fixé, carburant total et énergie interne déterminent température et masse de vapeur. Le transfert chimique reste interne ; la réaction consomme uniquement la vapeur.

```text
a = (cv - cl) T_ref - L_u,ref
u_l(T) = cl T + a
u_v(T) = cv T
L_u(T) = (cv - cl) T - a
ln(p_sat(T) / p_ref) = (cv + R - cl) / R * ln(T / T_ref)
                        + a / R * (1/T - 1/T_ref)
m_v(T) = min(M_f, p_sat(T) V / (R T))
U_eq(T) = (M_c + m_v) cv T + (M_f - m_v) u_l(T)
```

## Limites d'alimentation et numériques

Un solveur scalaire borné utilise l'énergie d'équilibre monotone. La chaleur de paroi suit Euler implicite. Le couplage au transport et au travail du cylindre présente une convergence du premier ordre. La coexistence liquide reste dans l'intervalle déclaré. Le gaz peut surchauffer après évaporation complète dans cet intervalle, sans extrapoler la courbe de saturation.

L'alimentation liquide par défaut rejette les états humides sous saturation. L'option de réservoir à volume fini résout le transfert vapeur source avec potentiels chimiques corrigés. Rail et aspiration restent liquides séparés ; entraînement de bulles et écoulement cavitant demandent une physique supplémentaire.

La comparaison ne tolère que l'arrondi binary64 : `64 * epsilon * max(p, p_sat)`, avec `epsilon = 2^-52`. La marge affichée n'est pas écrêtée. C'est un contrôle de saturation globale ; pertes locales et NPSH de pompe demandent un modèle d'écoulement résolu. Les contrôles indépendants exigent aussi une entropie totale non décroissante pour le flash adiabatique et l'échange avec la paroi finie.

## Preuves

`equilibrium-fuel-cell` montre condensation, évaporation par ventilation puis nouvelle condensation. `equilibrium-injected-cylinder` ajoute l'équilibre réversible du récepteur et les marges d'alimentation au moteur à réservoir ventilé. Maximisation indépendante d'entropie, limites analytiques, raffinement ODE simultané, rejeu complet, retour arrière et allocations sont vérifiés. Asset v36 conserve ces propriétés et lit v1-v35.

Paramètres `unverified`. Propriétés d'essence mesurées, cinétique multiphase multicomposant ou à vitesse finie, déplacement liquide/pulvérisation résolus et calibration OEM ne sont pas fournis. L'acceptation réelle Unity Editor/Play/Player/IL2CPP reste en attente.

[Cantera: species thermodynamics](https://www.cantera.org/stable/reference/thermo/species-thermo.html) · [Cantera: equilibrium](https://www.cantera.org/stable/python/thermo.html) · [VALIDATION.fr.md](VALIDATION.fr.md)

## Équilibre du réservoir à volume fini

Activer `liquid_fuel_tank.parameters.headspace.phase_equilibrium` avec `true` pour résoudre évaporation et condensation du réservoir. Le film associé fournit les propriétés `phase_equilibrium` ; l'espace gazeux suivi partage R, cv, pouvoir calorifique et rapport stœchiométrique. Déclarer une température initiale commune et une pression partielle saturée pour un réservoir humide ; un réservoir sec commence au plus à saturation. La densité totale reste inférieure à celle du liquide.

`evaporated_fuel_mass`, `condensed_fuel_mass` et `mass_flow` signé cumulent le transfert net par pas physique complet. Lire ensemble `vapor_partial_pressure`, `saturation_pressure` corrigée, inventaire liquide et volume gazeux. La pompe aspire seulement le liquide séparé ; transport diphasique pompe/rail, entraînement et NPSH local restent ouverts.

[Équilibre du réservoir à volume fini](TANK_PHASE_EQUILIBRIUM.fr.md)
