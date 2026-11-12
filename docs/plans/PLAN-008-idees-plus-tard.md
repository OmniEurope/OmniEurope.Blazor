<!-- SPDX-License-Identifier: EUPL-1.2 -->
# PLAN-008 : idées pour plus tard

> Statut : **réservé**. Numéro réservé aux idées que le propriétaire garde pour plus tard (règle globale du 2026-11-04), hors de la numérotation dense. Établi le 2026-11-12 ; l'ancien PLAN-008 de la deuxième série (demandes d'une application cliente du 2026-10-29) est devenu [PLAN-011](archive/serie-2/PLAN-011-demandes-application-cliente-2026-10-29.md).

Une idée de cette liste ne se traite que sur demande explicite du propriétaire ; elle passe alors dans un plan numéroté.

## Idées

- **Déploiement de la vitrine** (ex lot H2 du [PLAN-005](archive/serie-2/PLAN-005-galerie-depuis-demo-applicative.md), reporté le 2026-11-12) : Dockerfiles du front et de l'API, Compose de production, `.pipeline`, overlay `docs/code-rules.md` (règles d'hôte actives pour la vitrine), CI. Preuve attendue : contrôle de conformité du déploiement sans écart et pile Compose démarrée en local.
- **`OmniCard.Bordered`** (point W-9 du [PLAN-011](archive/serie-2/PLAN-011-demandes-application-cliente-2026-10-29.md), à décider) : un paramètre par carte pour retirer le liseré, en plus du jeton public `--omni-card-border-width` livré en 1.11.0, qui s'applique à toutes les cartes d'une portée (grilles et éditeurs compris).
