# Contributeurs automatisés

Tout contributeur automatisé commence par `CLAUDE.md`, puis lit `.claude/code-rules.md`, le plan actif indexé par `.claude/plan.md` et les documents liés à la surface modifiée.

Ordre d'autorité du dépôt :

1. instructions globales et demande utilisateur;
2. `CLAUDE.md` et `.claude/code-rules.md`;
3. plan canonique actif;
4. contrats spécialisés sous `docs/`;
5. preuves exécutables dans les tests, scripts `eng/` et workflows.

Un agent ne peut ni affaiblir une gate, ni inventer une preuve, ni remplacer un comportement par un stub. Toute affirmation de compatibilité, couverture ou sécurité doit être reliée à une vérification observable. Les décisions architecturales durables sont ajoutées à la documentation concernée; un ADR est créé uniquement lorsqu'une nouvelle décision explicite compare des alternatives et engage durablement le projet.
