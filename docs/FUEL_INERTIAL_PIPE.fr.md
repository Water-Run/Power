# Quantité de mouvement d'une ligne finie de carburant liquide

[English](FUEL_INERTIAL_PIPE.md) · [简体中文](FUEL_INERTIAL_PIPE.zh-CN.md) · **Français** · [Русский](FUEL_INERTIAL_PIPE.ru.md) · [日本語](FUEL_INERTIAL_PIPE.ja.md) · [한국어](FUEL_INERTIAL_PIPE.ko.md) · [Deutsch](FUEL_INERTIAL_PIPE.de.md) · [Español](FUEL_INERTIAL_PIPE.es.md) · [Italiano](FUEL_INERTIAL_PIPE.it.md) · [Português](FUEL_INERTIAL_PIPE.pt-BR.md)

## Contrat physique

Un `fuel_inertial_pipe` relie deux nœuds de carburant finis et une paroi thermique finie. La ligne horizontale rigide possède masse liquide, énergie calorique et cinétique propres, en plus des réservoirs. Sa densité est constante, sa température variable et parfaitement mélangée. Les ports et toute la ligne restent liquides ; les liens quasi statiques gardent leur comportement diphasique séparé.

Déclarer longueur, aire, résistances linéaire/quadratique, débit initial, température initiale et `fluid_heat_fraction`. Les unités sont `pa_s_m3` et `pa_s2_m6`. Propriétés et densité correspondent aux deux ports. Aucune commande directe d'ouverture : fermer une autre vanne n'efface pas l'impulsion. La référence matérielle et chimique vient des ports.

## Énergie et ports

L'enthalpie du donneur quitte le volume amont. La ligne mélange l'énergie sensible entrante avec son inventaire, puis livre son enthalpie mélangée à la pression aval. Le travail de pression modifie l'énergie cinétique et fournit une seule fois la chaleur de friction. La fraction déclarée chauffe la ligne ; le reste chauffe sa paroi finie. Une inversion échange donneur et récepteur avec les mêmes bilans.

```text
M_line = rho L A; I = rho L / A
E_line = U_line + I Q^2/2 + M_line LHV
D = R Q^2 + K abs(Q)^3
dM_A/dt = -rho Q; dM_B/dt = rho Q
I dQ/dt = p_A - p_B - R Q - K Q abs(Q)
dU_line/dt = rho abs(Q) (u_donor-u_line) + xi D
dE_kinetic/dt = (p_A-p_B) Q - D
dE_wall/dt = (1-xi) D
h_port = u + p/rho
```

La quantité de mouvement utilise le débit médian et les pressions finales acceptées. Énergie des ports et mélange utilisent Euler implicite ; le couplage complet converge au premier ordre. Même sans friction, la discrétisation des pressions transforme une partie de l'oscillation en chaleur numérique, non comptée comme friction physique. L'entropie inclut mélange et discrétisation. Les événements de débit nul séparent les historiques directs et inverses.

## Utilisation et preuves

Exécuter `inertial-fuel-line` pour inversion/mélange, ou `inertial-pump-fed-cylinder` pour entrée, pompe, aiguille électromagnétique et combustion prescrite. Lire `volume_flow`, `mass_flow`, `forward_volume`, `reverse_volume`, `kinetic_energy`, `energy_at_a`, `energy_at_b`, inventaires thermiques et bilans. Le débit volumique est instantané ; le débit massique est la moyenne du dernier pas. La ligne ajoute masse et énergie chimique propres.

Une solution harmonique indépendante à compliance finie et une RK4 indépendante du mouvement/température non linéaires vérifient exactitude et premier ordre. Les contrôles couvrent chaleur, entropie, inversion, bilans complets, intervalles spéculatifs d'embrayage, allocations, rollback, annulation et branches indépendantes. Assets portables et MCP réel vérifient chaque frontière. Cela ne valide ni matériel calibré ni Unity réel.

## Validité et suite

Les paramètres restent `unverified`. Respecter les plages de température/pression et la stabilité liquide corrigée en pression. Un intervalle échoué est entièrement annulé ; réduire le pas et vérifier épuisement, pression et phase. Densité variable diphasique, cavitation, acoustique, gravité, géométrie mobile et fronts thermiques résolus sont omis. La combustion prescrite ne valide pas chimie d'équilibre ni entropie réactionnelle détaillées.

Relier les conduites compressibles à des chambres entraînées par arbre et à des frontières aiguille/buse compatibles, avec travail de pression et hypothèses de phase explicites. Poursuivre relaxation/glissement, allumage, admission/échappement, pertes mécaniques et coordination ECU/TCU. Étalonnage mesuré et acceptation Unity réelle restent distincts. [Conduites de carburant compressible à l’équilibre](EQUILIBRIUM_FUEL_DUCT.fr.md)

[Modelica: momentum and energy balances](https://doc.modelica.org/Modelica%203.2.3/Resources/helpWSM/Modelica/Modelica.Fluid.UsersGuide.ComponentDefinition.BalanceEquations.html) · [Modelica: dynamic pipe boundaries](https://doc.modelica.org/om/Modelica.Fluid.Pipes.DynamicPipe.html) · [VALIDATION.md](VALIDATION.md)
