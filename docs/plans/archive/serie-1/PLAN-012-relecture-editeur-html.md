<!-- SPDX-License-Identifier: EUPL-1.2 -->
# PLAN-012 : Relecture dans OmniHtmlEditor (orthographe, grammaire)

> Statut : **terminé** le 2026-10-03, archivé ; établi le même jour à la demande du propriétaire (lot D2 du PLAN-001 d'une application cliente, ADR-024
> d'une application cliente). OE reçoit une accroche générique et gratuite ; aucun moteur ni dictionnaire n'entre dans le paquet.

## Objectif

Un hôte branche sur `OmniHtmlEditor` un relecteur de son choix (correcteur orthographique Hunspell, liste de mots,
vérification grammaticale) par une extension. L'éditeur découpe le texte de la face visuelle en blocs avec leur langue
(attribut `lang`), demande les anomalies après une pause de frappe, les souligne sans modifier le contenu (CSS Custom
Highlight API) et, au clic droit sur un passage souligné, propose les corrections du relecteur, « Ignorer », « Tout
ignorer » et « Ajouter au dictionnaire » quand le relecteur les prend en charge. Désactivé tant qu'aucune extension ne
fournit de relecteur (API additive).

## Lots

### Lot 1 - API et texte de la surface
- [x] `OmniHtmlEditorProofreader` (abstrait : `CheckAsync`, `SuggestAsync`, `IgnoreAllAsync`, `AddToDictionaryAsync`),
  `OmniHtmlEditorProofreadingText`, `OmniHtmlEditorProofreadingIssue`, `OmniHtmlEditorProofreadingKind` ;
  `OmniHtmlEditorExtension.Proofreader`.
- [x] Script : blocs de texte (balisage en ligne traversé, éléments non éditables et texte proposé exclus), langue la plus
  proche, demande différée après la frappe, résultats gardés par bloc inchangé, soulignement par surligneurs nommés.
Contrôle : tests bUnit (relecteur appelé avec les blocs, anomalies rendues au script, exception d'un relecteur sans effet).

### Lot 2 - Menu et actions
- [x] Clic droit sur un passage souligné : corrections proposées (cinq au plus), « Aucune suggestion », « Ignorer »,
  « Tout ignorer », « Ajouter au dictionnaire », puis le menu des extensions ; correction appliquée comme une frappe
  (historique, `ValueChanged`).
- [x] Textes dans les 24 langues.
Contrôle : tests bUnit du menu ; contenu inchangé tant que rien n'est choisi.

### Lot 3 - Vitrine, documentation, preuve navigateur
- [x] Démonstration dans la vitrine (relecteur d'exemple, les deux genres d'anomalie).
- [x] `docs/editor-components.md`, `CHANGELOG.md`, référence d'API publique.
- [x] Vérification dans le navigateur : faute soulignée, correction appliquée, « Ignorer » retire le soulignement.
Contrôle : suite OE verte avec compteur ; capture de la vitrine.

> Fait le 2026-10-03 : suite OE 4099/4099 (dont `HtmlEditorProofreadingTests`, 8 tests : option du script, blocs et
> langues transmis, relecteur en échec sans effet, face source sans relecture, menu à cinq corrections au plus, actions
> « Ignorer », « Tout ignorer », « Ajouter au dictionnaire », passage mal formé sans menu, ordre avant les commandes) ;
> référence d'API publique : 39 signatures ajoutées, aucune retirée. Vitrine vérifiée dans le navigateur (1400 x 900) :
> « ortografe » souligné en rouge et « répété répété » en bleu, menu « orthographe / Ignorer / Tout ignorer / Ajouter au
> dictionnaire » puis les commandes de l'extension, correction appliquée et soulignement retiré, Ctrl+Z la défait et le
> mot est de nouveau souligné, « Ignorer » et « Tout ignorer » retirent le soulignement sans changer le texte ; console
> sans erreur. Nouvelle version du paquet : à la publication du propriétaire (une application cliente lit OE par `ProjectReference`).
