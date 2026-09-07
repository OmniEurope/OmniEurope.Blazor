# Capacités par famille

Les familles ci-dessous regroupent les composants publics de la bibliothèque et décrivent leurs limites actuelles.

- Actions : boutons simples, bascules, boutons scindés et actions de menu.
- Charts : moteur SVG, axes, domaines partagés, séries projetées et empilements à baselines cumulées.
- Data : listes, pagination, arbre et grille virtualisable avec projection locale ou chargement distant annulable par blocs.
- Editor : édition source HTML, aperçu sanitizé, historique et outils personnalisés.
- Feedback : alertes à deux variantes de remplissage et emplacement d'icône, progression, skeletons et indicateurs d'état.
- Forms : champs texte, formulaires typés, validation et contrôles de saisie.
- Foundation : primitives de texte, titres et types fondamentaux.
- Layout : conteneurs, cartes, panneaux, barres et structure de page.
- Navigation : fil d'Ariane, menus à groupes imbriqués qui déplient la branche portant la page courante, onglets, étapes et profil, avec icônes optionnelles et rendu icône seule du menu latéral.
- Overlays : hôte, dialogues empilés avec ouverture attendue par `OpenDialogAsync`, notifications bornées, tooltip local et menu contextuel porté.
- Scheduling : timeline et scheduler jour/semaine/mois en `DateTimeOffset`, avec fuseau explicite.
- Selection : dropdown simple/multiple, autocomplete annulable, listes de choix, date, slider, couleur et upload.

Les noms Omni décrivent des capacités, pas des implémentations.

## Limites fonctionnelles actuelles

- La DataGrid virtualise réellement le viewport avec un défilement continu, une hauteur de tableau paramétrable en longueur CSS et un chargement distant par blocs ; elle refuse explicitement de virtualiser en présence de groupes ou de lignes de détail. Voir [data-components.md](data-components.md).
- Les séries et axes SVG partagent désormais leurs domaines et projections; la couverture visuelle navigateur reste à étendre aux combinaisons de séries et de tailles de viewport.
- L'éditeur HTML assainit sa valeur, affiche un aperçu et conserve un historique ; ses commandes portent encore sur tout le document et ne gèrent ni sélection, ni caret, ni composition IME, ni collage spécialisé.
- Le catalogue illustre un sous-ensemble de la surface. Le registre `110/110` ne remplace pas des scénarios comportementaux, navigateur et accessibilité pour chaque cible.
