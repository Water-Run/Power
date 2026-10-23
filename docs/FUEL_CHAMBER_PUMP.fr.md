# Pompe carburant diphasique entraînée par arbre

[English](FUEL_CHAMBER_PUMP.md) · [简体中文](FUEL_CHAMBER_PUMP.zh-CN.md) · **Français** · [Русский](FUEL_CHAMBER_PUMP.ru.md) · [日本語](FUEL_CHAMBER_PUMP.ja.md) · [한국어](FUEL_CHAMBER_PUMP.ko.md) · [Deutsch](FUEL_CHAMBER_PUMP.de.md) · [Español](FUEL_CHAMBER_PUMP.es.md) · [Italiano](FUEL_CHAMBER_PUMP.it.md) · [Português](FUEL_CHAMBER_PUMP.pt-BR.md)

## Contrat physique

`fuel_chamber` couple un arbre réel à une chambre finie du domaine carburant. Des `fuel_check_valve` d'entrée et de sortie assurent remplissage et livraison par pression. Liquide, vapeur et énergie élastique partagent la même fermeture. Une chambre sous-alimentée peut vaporiser à l'aspiration ; la livraison suit l'inventaire et la pression calculés.

`node_a` est rotationnel et `node_b` est un nœud carburant avec un seul propriétaire géométrique. Déclarer `minimum_reference_volume`, `swept_volume` en `m3` ou `l`, et `phase` en `rad` ou `deg`. Le stockage du nœud correspond au volume initial à l'arrondi binary64 près. Le mécanisme sinusoïdal n'ajoute pas d'inertie de piston ou de came ; l'inertie d'arbre est explicite.

```text
V0(theta) = Vmin + Vs sin^2((theta - phase) / 2)
V = V0(theta) + C p
E = U_fluid + C p^2 / 2
dE = T dS + mu dM - p dV0
torque_pressure = -delta_E_adiabatic / delta_theta
```

## Travail et ports

Le sous-pas mécanique fermé conserve masse et entropie. L'énergie comprend fluide et paroi élastique ; le travail d'arbre porte donc sur le volume géométrique de référence. La même variation acceptée fournit le couple dans le solveur mécanique joint, engrenages, convertisseurs, hydraulique et intervalles spéculatifs d'embrayage compris. La détente pousse l'arbre ; `hydraulic_work` positif entre dans la chambre. Aucune source de pression extérieure supplémentaire n'est ajoutée.

`fuel_check_valve` utilise une conductance déclarée en `m3_s_pa` et une ouverture fractionnaire facultative. Le fluide donneur homogène passe seulement de A vers B lorsque A est à pression supérieure. C'est un clapet idéal sans masse et à pression d'ouverture nulle. Les contrats d'enthalpie conservative et de chaleur de paroi finie restent ceux du réseau carburant.

## Utilisation et preuves

Demander `equilibrium-fuel-pump` ou `electric-equilibrium-fuel-pump` par MCP, ou exécuter leur JSON par CLI. Lire masse, température, pression, vapeur et énergie avec livraison des clapets et vitesse d'arbre. Le `volume` du composant est géométrique ; le `volume` du nœud inclut la dilatation de paroi. `torque`, `hydraulic_power` et `hydraulic_work` exposent l'échange interne d'arbre.

Pression/travail du liquide, adiabates de vapeur idéale et inversion indépendante de l'entropie diphasique vérifient la fermeture. Le mouvement liquide fermé montre l'ordre deux ; remplissage/livraison avec changement de phase montre l'ordre un face à une référence RK4 inversée indépendamment. Sous-alimentation, apport liquide complet, moteur électrique, composition engrenages/hydraulique/embrayage, rejeu, rollback et allocations couvrent l'intégration. L'asset v35 stocke la géométrie et lit v1-v34. Unity Editor/Play/Player/IL2CPP réel reste non vérifié.

## Travail restant

Les paramètres restent `unverified`. Les limites de température, pression et volume représentable s'appliquent sur tout le cycle. Hypothèses : liquide incompressible, vapeur idéale pseudo-espèce, équilibre instantané à température commune et pression extérieure de carter nulle. Inertie d'entrée/NPSH, blocage sonique, nucléation retardée, inertie/hystérésis de clapet, mélanges réels et calibration mesurée restent ouverts.

[Modelica: swept volume](https://doc.modelica.org/Modelica%204.0.0/Resources/helpWSM/Modelica/Modelica.Fluid.Machines.SweptVolume.html) · [Cantera: reactor walls](https://cantera.org/dev/reference/reactors/interactions.html) · [VALIDATION.md](VALIDATION.fr.md)

La pompe du domaine carburant alimente désormais une aiguille physique et une décharge à titre figé dans un récepteur allumé à volume liquide fini. Les inventaires incluent changement de phase, travail de déplacement, énergie cinétique du jet et impulsion à la paroi immobile. [Décharge de carburant à titre figé et récepteurs finis](FUEL_DISCHARGE.fr.md)
