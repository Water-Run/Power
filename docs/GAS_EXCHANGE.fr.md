# Primitives d'échange gazeux

[English](GAS_EXCHANGE.md) · [简体中文](GAS_EXCHANGE.zh-CN.md) · **Français** · [Русский](GAS_EXCHANGE.ru.md) · [日本語](GAS_EXCHANGE.ja.md) · [한국어](GAS_EXCHANGE.ko.md) · [Deutsch](GAS_EXCHANGE.de.md) · [Español](GAS_EXCHANGE.es.md) · [Italiano](GAS_EXCHANGE.it.md) · [Português](GAS_EXCHANGE.pt-BR.md)

Ce document consigne la première tranche de l'incrément d'échange gazeux décrit dans
[les notes de reprise moteur](NEXT_ENGINE_STEP.fr.md) : la physique d'écoulement et de volume de contrôle, validée
pour elle-même, avant qu'aucune partie ne soit raccordée au graphe de modèle compilé.

Les primitives se trouvent dans `src/Power.Core/GasExchange.cs` et sont couvertes par
`tests/Power.Tests/GasChecks.cs`. Un [jalon ultérieur de réseau gazeux du cœur](GAS_NETWORK.fr.md)
relie maintenant des nœuds gazeux finis, des restrictions, des liaisons thermiques et des bilans de conservation au
modèle compilé. L'intégration du 2026-09-22 ajoute le JSON, la CLI/le MCP et l'asset portable v3,
tout en conservant les lecteurs v1/v2 pour leurs jeux de modèles d'origine. Les vues schématiques Unity et les
tests sont préparés ; la vérification Editor réelle reste en attente. Le cylindre adiabatique fermé reste une
référence analytique inchangée, encore sans échange de masse à travers sa chambre couplée au vilebrequin.

## Ce qui est implémenté

| Type | Responsabilité |
|---|---|
| `IdealGas` | Gaz caloriquement parfait d'une composition fixe : `R`, `gamma`, `cv`, `cp`, le rapport de pression critique et les deux coefficients de débit massique de tuyère précalculés. |
| `GasVolumeState` | Volume fini suivi par la **masse et l'énergie interne comme états indépendants**, avec masse volumique, température, pression et enthalpie massique dérivées. |
| `Orifice` | Écoulement compressible idéal à travers une restriction, avec un coefficient de débit et une fraction d'ouverture sans dimension dans `[0,1]`, signé dans les deux sens, sonique et sous-critique. |

`GasVolumeState` remplace délibérément la dérivation du cylindre fermé par l'angle et l'entropie initiale.
Comme la masse et l'énergie interne sont portées indépendamment, le même état peut absorber une masse transportée,
une enthalpie transportée et de la chaleur de paroi sans supposer un historique isentropique.

## Équations

La pression statique utilise `p = (gamma - 1) U / V`, exacte pour un gaz caloriquement parfait, et évite
un aller-retour de température distinct. La température est `T = U / (m cv)`.

Le débit massique suit les relations standard de tuyère isentropique. Avec `A` l'aire effective
(aire géométrique × coefficient de débit × ouverture), l'état statique amont `p_u, T_u` et le rapport de
pression `pr = p_d / p_u` :

- sonique, `pr <= (2/(gamma+1))^(gamma/(gamma-1))` :
  `mdot = A (p_u / sqrt(T_u)) sqrt(gamma/R) (2/(gamma+1))^((gamma+1)/(2(gamma-1)))`
- sous-critique : `mdot = A (p_u / sqrt(T_u)) sqrt(2 gamma / (R (gamma-1)) (pr^(2/gamma) - pr^((gamma+1)/gamma)))`

La veine porte l'enthalpie amont, `hdot = mdot cp T_u`, donc le sens de l'écoulement décide
quelle température d'extrémité est transportée. Un réservoir est passé comme une paire `(p, T)` ordinaire, donc
aucun volume fictif n'est requis pour une frontière fixe.

Référence pour les deux branches : [blocage sonique du débit massique NASA](https://www.grc.nasa.gov/www/k-12/BGP/mflchk.html).
C'est une référence pour les relations, pas une validation de cette implémentation.

## Notes numériques

La fonction de débit sous-critique est évaluée comme `pr^(2/gamma) * (1 - pr^((gamma-1)/gamma))`, le
second facteur étant calculé par `expm1`. La différence de manuel entre deux puissances presque égales s'annule
de façon catastrophique lorsque `pr` approche de un : à `pr = 1 - 1e-12` elle ne conserve qu'environ quatre chiffres, alors que
la forme `expm1` est exacte à la précision du rapport stocké. `Numeric.Expm1` et `Numeric.Log1p`
sont maintenant partagés avec la physique du cylindre fermé, plutôt que dupliqués.

Deux limites tiennent au modèle plutôt qu'à l'implémentation, et un solveur qui l'adopte
doit traiter les deux :

- La branche sous-critique a une **dérivée infinie au rapport de pression unitaire**. Un pas de Newton ne doit
  pas traverser ce point tout droit ; il faut l'encadrer ou l'amortir.
- Un rapport proche de un ne peut pas être représenté utilement en binary64. À `pr = 1 - 1e-15`, environ un seul
  chiffre de l'écart survit, quelle que soit l'écriture de la fonction.

Les conditions d'arrêt et statiques amont sont traitées comme égales. C'est l'approximation usuelle de
volume de contrôle quasi stationnaire, et elle n'est **pas** valable pour un écoulement de chambre à Mach élevé.

## Preuves

`tests/Power.Tests/GasChecks.cs` ajoute six contrôles, chacun écrit contre une forme fermée indépendante
plutôt que contre une sortie enregistrée de ce code :

1. **Propriétés et continuité du régime sonique** — `cv`, `cp` et le rapport critique contre leurs
   définitions pour `gamma` dans `{1.1, 1.3, 1.4, 5/3}` ; la branche sous-critique atteint le
   coefficient sonique exactement au rapport critique ; décroissance monotone de la fonction de débit jusqu'à zéro, contrôlée
   contre la forme naïve là où cette forme est fiable, et contre le développement au premier ordre
   là où elle ne l'est pas.
2. **Écoulement de tuyère** — 54 combinaisons de pression amont, de température amont et de rapport de pression
   contre les relations NASA écrites en entier, y compris le fait que l'écoulement sonique est indépendant de la
   pression aval et que l'enthalpie est portée à la température amont.
3. **Contrats** — antisymétrie exacte par échange des extrémités, débit nul pour un orifice fermé et
   à pressions égales, linéarité dans la fraction d'ouverture, et rejet des états non finis ou
   non positifs, des ouvertures hors de `[0,1]` et des paramètres de gaz ou d'orifice invalides.
4. **Vidange adiabatique d'une enceinte** — intégration RK4 d'une enceinte de 2 L depuis 20 bar et 900 K contre la
   solution isentropique analytique `rho(t) = (rho0^-k + k C t / V)^(-1/k)`, `k = (gamma-1)/2`, accordée à
   1e-9 en relatif sur la masse volumique et la température et à 1e-8 sur la pression, avec un contrôle de raffinement.
5. **Remplissage depuis un réservoir** — charge d'une enceinte de 0.5 L depuis un réservoir à 6 bar et 320 K : l'identité exacte
   `dU = cp T_supply dm` tant que l'écoulement est unidirectionnel, et la limite de l'enceinte évacuée
   `T -> gamma T_supply`, contrôlée depuis deux pressions de départ différentes.
6. **Réseau fermé à deux volumes** — 2 s d'échange entre un volume chaud de 1.5 L et un volume froid de 0.4 L :
   masse totale conservée à 1e-14 en relatif et énergie interne totale à 1e-12 en relatif, pressions
   qui s'égalisent, et équilibre confirmé comme mécanique plutôt que comme la température du mélange complet.

## Ce qui reste ouvert

Le [jalon de réseau gazeux du cœur](GAS_NETWORK.fr.md) couvre maintenant les nœuds à volume fixe,
les réservoirs, les restrictions, les liaisons thermiques, les bilans de masse et d'énergie, les canaux de sortie et
l'avancement transactionnel borné. Le JSON, les assets portables, la découverte des capacités et les exemples de rejeu
sont intégrés. L'[extension du cylindre mobile](MOVING_CYLINDER.fr.md) couple maintenant l'échange gazeux et le travail du
vilebrequin. Un [calage optionnel sur l'angle vilebrequin](VALVE_TIMING.fr.md) commande les restrictions, et
la [combustion prémélangée](PREMIXED_COMBUSTION.fr.md) ajoute le carburant, l'air, les produits et le bilan
d'énergie chimique. Les preuves Unity Editor sont un
livrable distinct. La méthode implicite par paires proposée n'a pas été adoptée : la méthode
explicite actuelle, son limiteur d'équilibre et ses limites de précision y sont documentés.
Les volumes connectés exigent des constantes de gaz et un gamma identiques ; la thermochimie détaillée des espèces reste ouverte.
