# Mouvement planétaire Ravigneaux résolu

[English](RESOLVED_PLANETS.md) · [简体中文](RESOLVED_PLANETS.zh-CN.md) · **Français** · [Русский](RESOLVED_PLANETS.ru.md) · [日本語](RESOLVED_PLANETS.ja.md) · [한국어](RESOLVED_PLANETS.ko.md) · [Deutsch](RESOLVED_PLANETS.de.md) · [Español](RESOLVED_PLANETS.es.md) · [Italiano](RESOLVED_PLANETS.it.md) · [Português](RESOLVED_PLANETS.pt-BR.md)

L'assemblage résolu inclut la rotation propre absolue des deux jeux de satellites internes et l'inertie de masse orbitale autour du porte-satellites. Quatre contraintes d'engrènement physiques relient six rotors. Les cinq embrayages et freins de plage et le convertisseur externe restent des composants ordinaires. La réduction à quatre éléments reste disponible comme simplification déclarée séparée ; elle ne fournit pas de preuve de rotation propre des satellites.

La connectivité d'engrènement et les relations de pas primitif ont une [référence structurelle](https://www.mathworks.com/help/sdl/ref/ravigneauxgear.html) séparée. Power! dérive et implémente son propre graphe de rotors conservatif et des contrôles indépendants de matrice de masse. Aucune implémentation ni aucun paquet de modèle d'éditeur n'est inclus.

## Géométrie et énergie

Pour un rayon primitif de couronne `R`, et des rapports de grand et petit solaire `kL` et `kS`, la géométrie primitive rigide est :

```text
large sun radius = R/kL
small sun radius = R/kS
outer planet radius = (R-large sun radius)/2
inner planet radius = (large sun radius-small sun radius)/2
outer orbit radius = (R+large sun radius)/2
inner orbit radius = (large sun radius+small sun radius)/2
```

`RavigneauxPlanetParameters` exige un rayon de couronne SI, des masses et des inerties de rotation propre positives par satellite, et 1..32 paires de satellites égales et synchrones. L'espacement pair doit loger les deux jeux sans chevauchement des cercles primitifs. La géométrie et l'inertie agrégée doivent rester représentables. Toutes ces entrées sont des propriétés de recherche explicites ; l'auxiliaire ne fournit pas de valeurs mesurées.

Pour `n` paires égales, le porte-satellites reçoit l'inertie orbitale `n (mInner orbitInner^2 + mOuter orbitOuter^2)`. Le paramètre `CarrierInertia` existant est l'inertie de structure du porte-satellites dans ce chemin résolu. Chaque nouveau rotor a `n` fois son inertie de rotation propre par satellite. Leurs vitesses sont des vitesses angulaires absolues, donc l'énergie cinétique est le `J omega^2/2` ordinaire ; la co-rotation conserve l'énergie de rotation propre des satellites. Utiliser une rotation propre relative avec ce stockage diagonal omettrait le couplage du porte-satellites.

## Contrat d'engrènement

`carrier_gear` impose `A - ratio B + (ratio-1) C = 0`, où C est le porte-satellites mobile réel. Les engrènements externes utilisent un rapport de rayons primitifs négatif ; l'engrènement interne couronne/satellite extérieur utilise un rapport positif. Les rapports signés finis et non nuls, y compris un, sont pris en charge. Trois ports de rotation distincts et des vitesses initiales compatibles sont requis.

Les quatre engrènements sont grand solaire/satellite extérieur, petit solaire/satellite intérieur, couronne/satellite extérieur et satellite intérieur/satellite extérieur. Les trois couples de réaction entrent dans la même projection de point milieu et ont une puissance de port sommée nulle et une somme de couples nulle. La réaction du porte-satellites n'est pas envoyée silencieusement au bâti immobile. Un raffinement borné du résidu relatif améliore les petites réponses d'effort. La résolution au point milieu impose un résidu de vitesse nul à l'extrémité suivante, ce qui évite de réfléchir de façon répétée l'arrondi précédent. Les deux opérations utilisent les réponses réelles d'effort de contrainte et conservent leurs multiplicateurs de correction dans les historiques de réaction réels. Le brouillon appartient à chaque simulation ; les facteurs compilés restent immuables. Les lignes normalisées, les coordonnées compensées et les historiques de réaction complets conservent la phase, les forks, l'annulation et le rollback de lot.

La référence libre indépendante utilise les coordonnées couronne/porte-satellites. Avec `aOuter = R/outerRadius` et `aInner = R/innerRadius` :

```text
outer planet speed = aOuter ring + (1-aOuter) carrier
inner planet speed = -aInner ring + (1+aInner) carrier
M = sum over rotors of J [ring coefficient, carrier coefficient]^T
                         [ring coefficient, carrier coefficient]
```

Cela inclut les deux énergies de rotation propre et l'inertie orbitale ajoutée séparément. Des charges généralisées indépendantes, les inerties ramenées de tous les chemins avant et arrière, le moment cinétique et l'impulsion et la chaleur de capture du porte-satellites contrôlent la résolution assemblée.

## Graphe partagé et preuves

`CreateResolvedGraph` prend les ports d'origine, quatre ID distincts de nœud et d'engrènement de satellites, et les propriétés de satellites déclarées. Il renvoie des définitions ordinaires immuables : six rotors internes, quatre engrènements de porte-satellites, une démultiplication finale et cinq éléments de frottement. Le JSON plat conserve les inerties totales des rotors et les rapports d'engrènement signés ; la description de l'exemple enregistre la géométrie génératrice et les propriétés par satellite. Les condensés de source conservent cette preuve de rédaction déclarée.

`resolved-ravigneaux-transmission` exerce toutes les passations avant en montée et en descente. `fired-resolved-ravigneaux-converter` ajoute le moteur, les cartes de convertisseur signées et le verrouillage. Les deux déclarent trois paires, R=0.1 m, des masses intérieur/extérieur de 0.3/1 kg et des inerties de rotation propre par satellite de 0.000015/0.0005 kg m2. La structure du porte-satellites est 0.03 kg m2 ; l'ajout d'orbite explicite est 0.0184375 kg m2. Ce sont des entrées de recherche.

L'asset portable v24 conserve l'engrènement de porte-satellites signé et lit les versions antérieures. La primitive ajoute la balise d'empreinte 28 ; les graphes précédents conservent leurs empreintes et leur rejeu. Des marqueurs Studio préparés identifient les trois ports d'engrènement. La vérification réelle Unity Editor/Play/Player/IL2CPP reste séparée. Exécutez la vérification en série requise `dotnet run --file tools/Build.cs -- verify` ; les résultats numériques et le périmètre sont dans [VALIDATION.fr.md](VALIDATION.fr.md).

## Comportement restant

Des jeux de satellites égaux, rigides et synchrones ne modélisent pas la compliance des dents, le partage de charge de fabrication, le jeu, les pertes d'engrènement, la lubrification ni les propriétés dépendantes de la température. L'[actionnement par piston hydraulique alimenté par pompe](AT_HYDRAULIC_ACTUATION.fr.md) est disponible. Le contrôle de passage complet, la coordination ECU et les géométries et cartes OEM mesurées restent inachevés. L'assemblage générique ne prouve pas l'identité PSA AT8/AL4. Les frontières d'échantillon et les mesures manquantes restent intactes.
