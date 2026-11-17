# Relaxation de phase du carburant à vitesse finie

[English](RELAXING_FUEL.md) · [简体中文](RELAXING_FUEL.zh-CN.md) · **Français** · [Русский](RELAXING_FUEL.ru.md) · [日本語](RELAXING_FUEL.ja.md) · [한국어](RELAXING_FUEL.ko.md) · [Deutsch](RELAXING_FUEL.de.md) · [Español](RELAXING_FUEL.es.md) · [Italiano](RELAXING_FUEL.it.md) · [Português](RELAXING_FUEL.pt-BR.md)

## Modèle physique

Un nœud `relaxing_fuel` possède un titre massique de vapeur indépendant. Liquide et vapeur partagent pression, température et vitesse, mais l’équilibre chimique demande un temps fini déclaré. Un `relaxing_fuel_duct` transporte masse, quantité de mouvement, énergie totale et masse de vapeur dans des cellules explicites. Le temps de séjour dépend de leur géométrie et de la vitesse réelle.

La fermeture conserve le potentiel froid du liquide compressible et la vapeur idéale du modèle d’équilibre. Le titre initial est explicite, sans projection à l’équilibre. À masse, volume et énergie interne fixes, le titre tend exponentiellement vers sa cible avec `phase_relaxation_time_ns` constant. La chaleur latente reste dans le bilan interne. L’affinité chimique est le potentiel du liquide moins celui de la vapeur ; son produit par le transfert de phase, divisé par la température, donne une production d’entropie non négative. Cette loi de recherche n’est pas une corrélation de nucléation mesurée.

[Lund: A hierarchy of relaxation models for two-phase flow](https://www.sintef.no/globalassets/project/co2-dynamics/publications/lund_two-phase_relaxation_hierarchy.pdf)

## Transport et méthode numérique

Les flux conservatifs de Rusanov transportent quatre champs. Chaque intervalle CFL global applique une demi-source locale, le transport puis une demi-source. Un réservoir partagé reçoit chaque source une seule fois, quel que soit le nombre de conduits. La source est exacte pour un état isolé à volume fixe ; le transport couplé reste du premier ordre. Le CFL utilise une borne acoustique à titre gelé. Les entropies de phase et du conduit sont vérifiées séparément. Un état hors limites annule le lot entier.

```text
x = vapor_mass / total_mass; v = 1/rho
v_liquid(p) = 1 / (rho0 (1 + p/K)); v_vapor(T,p) = R T/p
v = (1-x) v_liquid + x v_vapor
u = (1-x) [c_liquid T + a + u_cold(p)] + x c_vapor T
u_cold(p) = K/rho0 [ln(1+p/K) - (p/K)/(1+p/K)]

dx/dt = (x_equilibrium(rho,u) - x)/tau
x(t+h) = x_equilibrium + (x(t)-x_equilibrium) exp(-h/tau)
d(total_mass)/dt = d(total_internal_energy)/dt = d(volume)/dt = 0  (local source)
affinity = mu_liquid - mu_vapor
(ds/dx) at fixed u,v = affinity/T
ds/dt = affinity (x_equilibrium-x)/(T tau) >= 0

cp_frozen = (1-x) c_liquid + x (c_vapor+R)
alpha = x R/cp_frozen
c_frozen^2 = v^2 / [(1-x) v_liquid/(K+p) + (1-alpha) x R T/p^2]
U = (rho, rho velocity, rho total_specific_energy, rho x)
F = (rho velocity, rho velocity^2+p,
     velocity (rho total_specific_energy+p), rho x velocity)
```

## Utilisation, preuves et limites

Exécuter `relaxing-fuel-evaporation` ou `relaxing-fuel-duct`. Observer les titres réel et d’équilibre, l’affinité, l’entropie de phase, les inventaires de vapeur aux ports et le résidu global. Les historiques d’évaporation et de condensation séparent par signe le transfert net de source de chaque pas accepté ; ils ne comptent pas les échanges microscopiques bruts. Les vérifications couvrent la solution exponentielle indépendante, les dérivées d’entropie, l’acoustique gelée, la limite raide d’équilibre, l’auto-raffinement temporel et un raffinement spatial acoustique linéaire indépendant. Transactions, rejeu portable et MCP réel sont vérifiés séparément ; les valeurs figurent dans les preuves de validation.

Les paramètres restent `unverified`. Déclarer `initial_vapor_mass_fraction` dans [0,1] et un temps entier de 1 ns à une heure. Respecter les bornes de température et de pression positive du matériau. Le modèle suppose un conduit rigide horizontal de section constante, un pseudo-fluide pur, un liquide sans dilatation thermique et une vapeur idéale. Il omet glissement, retard thermique interfacial, tension superficielle, spray et gaz porteur. Le couplage direct du domaine relaxant aux pompes, buses, chambres mobiles et récepteurs en combustion reste à faire. Conservation, rejeu et raffinement ne valident ni le matériel mesuré ni Unity réel. Utiliser une vapeur diluée loin de la région critique réelle ; le matériau doit conserver une enthalpie latente positive et des volumes spécifiques de coexistence distincts.

Étendre les conditions aux limites compatibles avec la relaxation finie et obtenir des mesures du matériau, puis ajouter spray résolu et récepteurs en combustion. Poursuivre allumage, admission/échappement, pertes mécaniques, actionnement de transmission et contrôle ECU/TCU coordonné. L’étalonnage mesuré et l’acceptation Unity restent distincts.

[VALIDATION.fr.md](VALIDATION.fr.md)
