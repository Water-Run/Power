# Notes de reprise du développement moteur

[English](NEXT_ENGINE_STEP.md) · [简体中文](NEXT_ENGINE_STEP.zh-CN.md) · **Français** · [Русский](NEXT_ENGINE_STEP.ru.md) · [日本語](NEXT_ENGINE_STEP.ja.md) · [한국어](NEXT_ENGINE_STEP.ko.md) · [Deutsch](NEXT_ENGINE_STEP.de.md) · [Español](NEXT_ENGINE_STEP.es.md) · [Italiano](NEXT_ENGINE_STEP.it.md) · [Português](NEXT_ENGINE_STEP.pt-BR.md)

## Point de reprise

La pompe du domaine carburant alimente désormais une aiguille physique et une décharge à titre figé dans un récepteur allumé à volume liquide fini. Les inventaires incluent changement de phase, travail de déplacement, énergie cinétique du jet et impulsion à la paroi immobile.

[Pompe carburant diphasique entraînée par arbre](FUEL_CHAMBER_PUMP.fr.md)

## Prochaine étape

Résoudre inertie et pertes d'entrée, sensibilité à la relaxation de phase et au glissement, puis allumage, admission/échappement, pertes et ECU/TCU coordonnés. Chaque équation exige références indépendantes et raffinement ; calibration mesurée et acceptation Unity réelle restent séparées.

## Acceptation

Exiger références analytiques ou limites indépendantes, bilans complets masse/énergie et raffinement du pas adapté. Préserver retour arrière complet, annulation, branches, canaux stables, rejeu portable et anciens lecteurs. Continuer ensuite allumage, admission/échappement, pertes mécaniques, transmission et coordination ECU/TCU.

Les paramètres restent `unverified`. Respecter températures déclarées, volume gazeux positif et domaine vapeur diluée. Inertie locale d'entrée, nucléation à vitesse finie, glissement, spray résolu, comportement critique réel et calibration mesurée sont omis. La combustion prescrite ne valide pas l'équilibre chimique ni l'entropie réactionnelle. Poursuivre entrée, allumage, admission/échappement, pertes et coordination ECU/TCU.

[DEVELOPMENT_STATUS.fr.md](DEVELOPMENT_STATUS.fr.md) · [ROADMAP.fr.md](ROADMAP.fr.md) · [VALIDATION.fr.md](VALIDATION.fr.md)

[Décharge de carburant à titre figé et récepteurs finis](FUEL_DISCHARGE.fr.md)
