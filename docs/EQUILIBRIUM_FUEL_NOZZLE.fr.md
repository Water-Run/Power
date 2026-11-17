# Transit de carburant à l’équilibre dans les buses

[English](EQUILIBRIUM_FUEL_NOZZLE.md) · [简体中文](EQUILIBRIUM_FUEL_NOZZLE.zh-CN.md) · **Français** · [Русский](EQUILIBRIUM_FUEL_NOZZLE.ru.md) · [日本語](EQUILIBRIUM_FUEL_NOZZLE.ja.md) · [한국어](EQUILIBRIUM_FUEL_NOZZLE.ko.md) · [Deutsch](EQUILIBRIUM_FUEL_NOZZLE.de.md) · [Español](EQUILIBRIUM_FUEL_NOZZLE.es.md) · [Italiano](EQUILIBRIUM_FUEL_NOZZLE.it.md) · [Português](EQUILIBRIUM_FUEL_NOZZLE.pt-BR.md)

## Modèle physique

Régler `phase_mode` de `compressible_fuel_nozzle` sur `equilibrium` impose l’équilibre homogène instantané pendant le transit. Liquide et vapeur partagent pression, température et vitesse ; le titre change sur l’isentrope. La buse et les réservoirs finis utilisent le même potentiel liquide compressible et la même référence de vapeur idéale. `frozen` reste une hypothèse distincte prise en charge.

Pression et entropie spécifique déterminent liquide stable, vapeur ou coexistence. La fermeture utilise l’égalité des potentiels chimiques et les bornes thermiques et de pression existantes. Les extrémités de température n’admettent que le voisinage d’arrondi binary64. Section, chaleur de paroi, commande et aiguille suivent le contrat commun. Le jet est idéalement détendu jusqu’au récepteur ; son impulsion stationnaire n’ajoute ni force axiale d’aiguille ni travail mécanique.

## Encadrement global du flux

Le carré positif de la vitesse du son d’équilibre donne une densité croissante avec la pression à entropie fixe. La chute d’enthalpie décroît. Ces facteurs monotones bornent le flux sur chaque intervalle, y compris frontières de phase et extrema multiples. La séparation-évaluation renvoie débit inférieur échantillonné et borne supérieure restante à largeur relative au plus 1e-4. Limites : 512 intervalles sur pile et 8192 divisions. Différences caloriques, marges d’arrondi et bornes d’intégrales monotones utilisent jusqu’à 256 panneaux de Riemann et 80 itérations de fermeture. Un cas non résolu rejette le lot. Un maximum intérieur lisse est sonique ; une frontière de phase peut être non lisse.

```text
g_liquid(T,p) = g_vapor(T,p)
s(p,rho,T,x) = s_source
D(p) = h_source - h(p,s_source) = integral(p,p_source) v(q,s_source) dq
G(p) = rho(p,s_source) sqrt(2 D(p))
drho/dp at fixed s = 1/c_equilibrium^2 > 0
G_upper([p_low,p_high]) = rho(p_high) sqrt(2 D_upper(p_low))
G_lower = max(sampled rho(p) sqrt(2 D_lower(p)))
flux_relative_uncertainty = G_upper/G_lower - 1 <= 1e-4
mass_flow_local = opening Cd A G_lower
velocity_throat = c_equilibrium  (smooth interior maximum only)
```

Des sous-pas conservatifs du premier ordre échangent la même masse et enthalpie ; l’énergie cinétique du jet se répartit entre récepteur et paroi. Chaque acceptation vérifie les états et l’entropie totale de la paire. Limites : 4096 sous-pas, 64 réductions de moitié par candidat, transfert initial borné à 0.1 fois la plus petite masse. La fermeture tolère 128 fois epsilon binary64 multiplié par le plus grand module volumique ou la pression des ports, sans écrêter les pressions. Un échec restaure tout le lot. Réduire le pas et examiner inventaire, limites, course et section.

## Utilisation et preuves

Exécuter `equilibrium-flashing-nozzle` ou `equilibrium-pump-needle`. `flux_relative_uncertainty` décrit le dernier encadrement constitutif local accepté ; il ne borne ni débit moyen, ni masse livrée, ni intégration temporelle du premier ordre, ni étalonnage. Les vérifications emploient inversion indépendante de saturation/entropie, blocage analytique gaz/liquide, recherche globale dense distincte, identités d’énergie/entropie et raffinement analytique de vidange vapeur. Inventaires, arbre/conduite/aiguille, restauration, annulation, branches, allocations, assets et MCP réel sont vérifiés séparément. Résultats et anciennes frontières figurent dans les preuves de validation.

[VALIDATION.fr.md](VALIDATION.fr.md)

## Validité et suite

Les paramètres restent `unverified`. Cette limite suppose un pseudo-fluide pur, vapeur idéale, dilatation liquide nulle et vitesse commune. Temps finis de nucléation/relaxation, glissement, pulvérisation, gaz porteur et récepteur avec combustion sont omis. L’encadrement utilise des marges binary64 déclarées, sans preuve formelle par arrondis dirigés. Près des transitions critiques, interpréter la gorge avec l’enveloppe de flux. Validité mesurée et acceptation Unity réelle restent ouvertes.

Étendre les conditions aux limites compatibles avec la relaxation finie et obtenir des mesures du matériau, puis ajouter spray résolu et récepteurs en combustion. Poursuivre allumage, admission/échappement, pertes mécaniques, actionnement de transmission et contrôle ECU/TCU coordonné. L’étalonnage mesuré et l’acceptation Unity restent distincts.

[NASA: injector flow and finite phase-transfer times](https://ntrs.nasa.gov/api/citations/20190001326/downloads/20190001326.pdf) · [NIST: thermodynamic derivatives](https://pages.nist.gov/teqp-docs/en/latest/derivs/derivs.html) · [Lund: two-phase relaxation hierarchy](https://www.sintef.no/globalassets/project/co2-dynamics/publications/lund_two-phase_relaxation_hierarchy.pdf)
