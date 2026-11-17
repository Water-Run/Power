# Référence de volume de carburant déformable

[English](COMPLIANT_FUEL_VOLUME.md) · [简体中文](COMPLIANT_FUEL_VOLUME.zh-CN.md) · **Français** · [Русский](COMPLIANT_FUEL_VOLUME.ru.md) · [日本語](COMPLIANT_FUEL_VOLUME.ja.md) · [한국어](COMPLIANT_FUEL_VOLUME.ko.md) · [Deutsch](COMPLIANT_FUEL_VOLUME.de.md) · [Español](COMPLIANT_FUEL_VOLUME.es.md) · [Italiano](COMPLIANT_FUEL_VOLUME.it.md) · [Português](COMPLIANT_FUEL_VOLUME.pt-BR.md)

## Portée constitutive

`CompliantFuelVolume` fournit la fermeture constitutive du réseau de carburant en équilibre. Elle résout liquide, vapeur et coexistence avec stockage élastique depuis masse totale et température ou énergie. Le chemin d'injecteur alimenté par pompe attend son couplage étendu. [Réseau de transport de carburant en équilibre](EQUILIBRIUM_FUEL_NETWORK.fr.md)

## Thermodynamique

La masse volumique et la capacité thermique du liquide sont constantes ; la vapeur est une pseudo-espèce idéale. Les deux phases partagent la référence du film existant. Le potentiel chimique liquide comprend `p/rho_l`. Ainsi, `p_0(T)` est la référence matière à déplacement négligeable, pas la pression de saturation du fluide pur de densité finie. La paroi élastique est à pression extérieure nulle et stocke une seule fois le travail de pression.

`StoredEnergyJoules` contient l'énergie calorique et élastique ; l'énergie chimique du pouvoir calorifique reste un bilan séparé lors du raccordement au graphe.

```text
V = V_0 + C p = m_l / rho_l + m_v R T / p
E = m_l (cl T + a) + m_v cv T + C p^2 / 2
ln(p / p_0(T)) = p / (rho_l R T)       [coexistence]
h = (U_fluid + p V) / M
dE = T dS + mu dM                    [fixed vessel parameters]
```

La coexistence sélectionne la racine de vapeur à faible densité. Son enthalpie latente vaut `L_u + R T - p/rho_l` ; la dérivée de pression satisfait Clapeyron. À masse fixe, la capacité thermique dérive l'énergie du fluide et de la paroi, transfert de phase compris. La paroi ne porte aucune entropie.

## API Core

`Evaluate` reçoit température et masse totale. `Flash` reçoit masse totale et énergie stockée fluide plus élasticité. `AdvanceHeat` échange avec un bain fini par une conductance déclarée et une durée locale en secondes, avec Euler implicite. Les noms des propriétés indiquent les unités SI. Ces appels locaux ne remplacent pas l'horloge entière bornée de `Simulation`.

`FuelVolumeStatus` distingue entrée invalide, limites de température, masse et énergie, et échec du solveur ; une sortie rejetée vaut la valeur par défaut. `Flash` conserve l'énergie demandée et expose l'erreur calorique flottante dans `CaloricResidualJoules`. Les instances n'ont aucun état évolutif et les appels réussis n'allouent aucune mémoire managée.

## Validité

Déclarer volume de référence, compliance, densité liquide et pression maximale positifs. La limite de pression couvre toute la courbe de coexistence et reste sous `rho_l R T_min`. À la température minimale, la dilatation de coexistence doit atteindre au moins `128 * epsilon` du volume de référence, avec `epsilon = 2^-52`. Cela exclut la déformation non résolue et la branche de vapeur dense hors du modèle déclaré ; aucun point critique n'est prédit.

Respecter l'intervalle thermique du matériau et `0 < M <= rho_l (V_0 + C p_max)`. Vide, gaz porteur, mélanges réels, dilatation thermique, inertie de paroi, tension superficielle et cinétique de phase finie sont hors portée. Les paramètres restent `unverified`. Remplissage de pompe entraînée par arbre, raccordement d'injecteur, quantité de mouvement et blocage sonique demandent encore couplage et preuves indépendantes.

`AdiabaticReferenceVolume` déplace le même matériau immuable entre volumes de référence explicites à masse et entropie constantes. Sa variation d'énergie comprend l'élasticité et constitue le travail mécanique. Évaluation variable, flash et bain fini respectent les limites déclarées. [Pompe carburant diphasique entraînée par arbre](FUEL_CHAMBER_PUMP.fr.md)

## Preuves indépendantes

Les contrôles comparent potentiels chimiques, pentes de Clapeyron, dérivées caloriques et entropiques et relation de Gibbs avec stockage élastique. Une maximisation indépendante de l'entropie contrainte n'utilise pas le solveur de saturation. Les limites de récipient rigide, gaz idéal et extensivité et le raffinement RK4 du bain fini servent de références. L'échange thermique conserve l'énergie totale et converge à l'ordre un avec entropie totale non décroissante. Cela ne valide ni carburant mesuré ni Unity.

[NIST: pure-fluid equilibrium](https://pages.nist.gov/teqp-docs/en/main/algorithms/VLE.html) · [Cantera: constant-volume thermodynamics](https://cantera.org/stable/cxx/da/d94/PDSS__ConstVol_8cpp_source.html) · [VALIDATION.md](VALIDATION.fr.md)
