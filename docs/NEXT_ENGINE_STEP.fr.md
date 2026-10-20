# Notes de reprise du développement moteur

[English](NEXT_ENGINE_STEP.md) · [简体中文](NEXT_ENGINE_STEP.zh-CN.md) · **Français** · [Русский](NEXT_ENGINE_STEP.ru.md) · [日本語](NEXT_ENGINE_STEP.ja.md) · [한국어](NEXT_ENGINE_STEP.ko.md) · [Deutsch](NEXT_ENGINE_STEP.de.md) · [Español](NEXT_ENGINE_STEP.es.md) · [Italiano](NEXT_ENGINE_STEP.it.md) · [Português](NEXT_ENGINE_STEP.pt-BR.md)

## Point de reprise

Les récipients du domaine carburant échangent les mélanges liquide/vapeur par restrictions dissipatives conservatives et liens thermiques finis. Remplissage de pompe, arbre et décharge d'injecteur restent à raccorder.

[Réseau de transport de carburant en équilibre](EQUILIBRIUM_FUEL_NETWORK.fr.md)

## Prochaine étape

Raccorder les volumes carburant au remplissage de pompe entraînée par arbre et à la décharge d'injecteur physique, avec matière, quantité de mouvement et travail conservatifs. Résoudre inertie locale d'entrée et blocage sonique par références indépendantes. Continuer allumage, admission/échappement, pertes, actionnement de transmission et ECU/TCU sans assimiler ce réseau au groupe motopropulseur complet.

## Acceptation

Exiger références analytiques ou limites indépendantes, bilans complets masse/énergie et raffinement du pas adapté. Préserver retour arrière complet, annulation, branches, canaux stables, rejeu portable et anciens lecteurs. Continuer ensuite allumage, admission/échappement, pertes mécaniques, transmission et coordination ECU/TCU.

Les limites de matière et de déformation représentable de la fermeture restent applicables. Les paramètres restent `unverified`. Remplissage de pompe et travail d'arbre, raccordement d'injecteur, inertie locale, blocage sonique, retard de nucléation, glissement et mesures de mélanges réels restent à réaliser. Les rails d'injecteur alimentés par pompe gardent leur chemin liquide séparé en attendant ces couplages.

[DEVELOPMENT_STATUS.fr.md](DEVELOPMENT_STATUS.fr.md) · [ROADMAP.fr.md](ROADMAP.fr.md) · [VALIDATION.fr.md](VALIDATION.fr.md)
