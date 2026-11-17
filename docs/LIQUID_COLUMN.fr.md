# Référence de quantité de mouvement à l'entrée liquide

[English](LIQUID_COLUMN.md) · [简体中文](LIQUID_COLUMN.zh-CN.md) · **Français** · [Русский](LIQUID_COLUMN.ru.md) · [日本語](LIQUID_COLUMN.ja.md) · [한국어](LIQUID_COLUMN.ko.md) · [Deutsch](LIQUID_COLUMN.de.md) · [Español](LIQUID_COLUMN.es.md) · [Italiano](LIQUID_COLUMN.it.md) · [Português](LIQUID_COLUMN.pt-BR.md)

## Frontière physique

`LiquidColumn` est une référence C# autonome pour un tube horizontal rigide à section, vitesse uniforme, masse volumique liquide et température constantes. Les pressions absolues aux ports sont prescrites pendant chaque intervalle. Les résistances linéaire et quadratique sont des entrées de recherche explicites. Ce n'est pas un composant du graphe ; les équations du réseau et de la tuyère restent inchangées.

## Quantité de mouvement et énergie

Les ports ont le même débit et la même vitesse ; les flux cinétiques advectés se compensent. Le travail de pression modifie l'énergie cinétique stockée et produit la chaleur de friction. Un bain isotherme reçoit cette chaleur ; température et énergie interne du fluide restent constantes. La production d'entropie vaut chaleur divisée par température déclarée. Cette frontière ne représente pas un stock thermique fini.

```text
I = rho L / A                         [Pa s^2 / m3]
R                                    [Pa s / m3]
K                                    [Pa s^2 / m6]
I dQ/dt = p_A - p_B - R Q - K Q abs(Q)
E_kinetic = I Q^2 / 2
Q_mean = (Q_before + Q_after) / 2
I (Q_after-Q_before) = dt (p_A-p_B-R Q_mean-K Q_mean abs(Q_mean))
delta_V = dt Q_mean
W_pressure = (p_A-p_B) delta_V
H_friction = dt (R Q_mean^2 + K abs(Q_mean)^3)
W_pressure = delta_E_kinetic + H_friction
S_produced = H_friction / T
```

Le point milieu implicite résout l'équation scalaire monotone. Une inversion est divisée à débit nul avec la même équation, conservant volumes directs/inverses et chaleur. Un appel accepté emploie au plus deux intervalles. Un dépassement de l'équilibre stationnaire renvoie `StepTooLarge` ; réduire la durée. Résidus d'énergie et d'impulsion sont contrôlés à l'arrondi binary64. Un pas grossier accepté demande encore un contrôle d'exactitude.

## API du cœur et preuves numériques

Construire `LiquidColumn` avec géométrie SI, masse volumique, résistances, température et pression absolue minimale explicites. Passer `LiquidColumnState` et une durée en secondes à `Advance`. Les propriétés portent les unités SI. Le résultat comprend transferts signés et directionnels, travail, variation cinétique, chaleur et entropie. Un rejet laisse résultat par défaut et entrée intacte ; aucun appel n'alloue de mémoire gérée. La durée locale ne remplace pas l'horloge entière de simulation.

Accélération sans pertes, solution exponentielle linéaire et tangente hyperbolique quadratique indépendantes vérifient les limites. Une RK4 indépendante à pression variable vérifie le second ordre, inversion comprise. Décroissance passive, équilibre, bilan énergétique complet, entropie non négative, rejets, partage immuable et allocations sont testés sur les deux cibles.

## Couplage suivant

Les paramètres restent `unverified`. Le plancher de pression est déclaré par l'appelant pour la validité liquide ; il ne prédit ni pression de vapeur ni marge NPSH. Compressibilité, acoustique, gravité, géométrie mobile, stockage thermique, cavitation, glissement et relaxation de phase sont hors référence. Les liens diphasiques restent quasi statiques. Acceptation Unity réelle et calibration mesurée sont séparées.

Un `fuel_inertial_pipe` relie deux nœuds de carburant finis et une paroi thermique finie. La ligne horizontale rigide possède masse liquide, énergie calorique et cinétique propres, en plus des réservoirs. Sa densité est constante, sa température variable et parfaitement mélangée. Les ports et toute la ligne restent liquides ; les liens quasi statiques gardent leur comportement diphasique séparé. [Quantité de mouvement d'une ligne finie de carburant liquide](FUEL_INERTIAL_PIPE.fr.md)

[Modelica: momentum and energy balances](https://doc.modelica.org/Modelica%203.2.3/Resources/helpWSM/Modelica/Modelica.Fluid.UsersGuide.ComponentDefinition.BalanceEquations.html) · [Modelica: pipe flow models](https://doc.modelica.org/om/Modelica.Fluid.Pipes.BaseClasses.FlowModels.html) · [VALIDATION.md](VALIDATION.md)
