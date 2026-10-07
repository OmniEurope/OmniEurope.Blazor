<!-- SPDX-License-Identifier: EUPL-1.2 -->
# PLAN-016 : Neutralité du dépôt public

> Statut : **en cours**. Établi le 2026-10-07 sur décision du propriétaire (« réécrire tout, supprimer toutes mentions »). Remplace la décision en attente « historique public » de PLAN-009.

## Objectif

Le dépôt et le paquet sont publics. Rien de ce qui est publié ne nomme une application cliente, un outil
interne de développement ou d'assistance, ni ne contient de donnée personnelle (nom d'utilisateur, chemin
local, nom de serveur). Le README est en anglais. Les dépendances tierces nommées pour leur licence
(`NOTICE.md`, `docs/third-party-licenses/`) restent nommées : leur attribution est une obligation.

## Lots

### Lot 1 - Contenu courant

Remplacer chaque mention dans les fichiers suivis : sources, feuilles de style, scripts, commentaires de
documentation XML, textes et données de démonstration de la vitrine, tests, documentation, journal des
modifications et plans (archives comprises). Retirer du suivi le lanceur local et la configuration du
contrôle des règles du kit, qui restent sur le poste hors suivi.

Contrôle : la recherche des termes interdits sur `git ls-files` ne rend rien ; build Release sans
avertissement, suite complète verte, vitrine vérifiée dans le navigateur.

### Lot 2 - Garde

Un test qui échoue si un terme interdit ou un chemin local apparaît dans un fichier suivi. Les termes ne
sont pas écrits en clair dans le dépôt : le test compare l'empreinte SHA-256 de chaque mot.

Contrôle : le test échoue sur un fichier suivi qui contient un terme interdit, et passe sur l'arbre nettoyé.

### Lot 3 - Historique

Réécrire tout l'historique (contenu, chemins, messages de commit) et déplacer les étiquettes de version
sur les commits réécrits ; sauvegarde locale de l'historique d'origine avant réécriture.

Contrôle : aucun terme interdit dans aucun objet, message ni chemin des branches et étiquettes réécrites ;
l'arbre du dernier commit est identique à celui du lot 2.

### Lot 4 - Publication

Pousser en force `main`, `develop` et les étiquettes, sur accord explicite du propriétaire.

Contrôle : `git ls-remote` montre les nouveaux identifiants ; la recherche des termes sur un clone frais
ne rend rien.

## Hors du dépôt

Ce que la réécriture ne peut pas atteindre et qui relève du propriétaire : les paquets NuGet déjà publiés
(à déprécier ou délister sur nuget.org), les notes des versions GitHub, les demandes de fusion et les
anciens commits encore servis par GitHub par leur identifiant (demande de purge au support GitHub).
