# Notes de reprise du développement moteur

[English](NEXT_ENGINE_STEP.md) · [简体中文](NEXT_ENGINE_STEP.zh-CN.md) · **Français** · [Русский](NEXT_ENGINE_STEP.ru.md) · [日本語](NEXT_ENGINE_STEP.ja.md) · [한국어](NEXT_ENGINE_STEP.ko.md) · [Deutsch](NEXT_ENGINE_STEP.de.md) · [Español](NEXT_ENGINE_STEP.es.md) · [Italiano](NEXT_ENGINE_STEP.it.md) · [Português](NEXT_ENGINE_STEP.pt-BR.md)

## Point de reprise

L’alimentation comprend réservoirs finis, retours conservatifs, espace gazeux géométrique et ventilation explicite. Pompe, rampe et gaz échangent le travail interne ; livraison liquide, évaporation et réaction prescrite restent distinctes. Reprendre depuis les contrats et preuves actuels.

[Géométrie du réservoir et espace gazeux fini](TANK_HEADSPACE.fr.md)

## Prochaine étape

Cibler ensuite l’équilibre de phase dépendant de la pression et la cavitation avec propriétés matérielles explicites. Garder les mesures OEM manquantes et les paramètres de recherche non vérifiés. Remplissage/régulation pompe/vanne mesurés, actionnement magnétique/électronique et pulvérisation résolue restent à développer.

## Acceptation

Exiger références analytiques ou limites indépendantes, bilans complets masse/énergie et raffinement du pas adapté. Préserver retour arrière complet, annulation, branches, canaux stables, rejeu portable et anciens lecteurs. Continuer ensuite allumage, admission/échappement, pertes mécaniques, transmission et coordination ECU/TCU.

Réservoir rigide mélangé, liquide incompressible et gaz idéal. Ballottement/forme hydrostatique, équilibre des phases, cavitation, cartes pompe/vanne mesurées, calibration OEM et Unity Editor/Play/Player/IL2CPP réel restent ouverts. Paramètres `unverified`.

[DEVELOPMENT_STATUS.fr.md](DEVELOPMENT_STATUS.fr.md) · [ROADMAP.fr.md](ROADMAP.fr.md) · [VALIDATION.fr.md](VALIDATION.fr.md)
