# Conventions d'interface

- Les actions hors grille associent un libellé localisé et une icône Omni décorative lorsque la convention d'action l'exige.
- Boutons : trois tailles réellement distinctes, `Small` (hauteur de contrôle moins 0,5 rem, petit corps), `Medium` (hauteur de contrôle, celle du champ voisin) et `Large` (plus 0,5 rem, grand corps), communes à `OmniButton`, `OmniToggleButton` et `OmniSplitButton`. Un bouton qui ne porte qu'une `OmniIcon` est carré à sa taille ; la feuille de style le reconnaît à l'icône seul élément de son contenu, un libellé posé à côté d'elle va donc dans un élément (`<span>`), un texte nu passant inaperçu et privant le bouton de sa marge. Désactivé, un bouton s'éteint et s'assombrit ; occupé (`Busy`), un voile sombre respire par-dessus, sans changer sa taille ni son contenu.
- Dialogues : les boutons se tiennent au bout du pied, l'action d'abord, puis « Annuler » en `OmniButtonVariant.Danger`, chacun avec une icône et un libellé court. `OmniOverlayService.ConfirmAsync` rend cette convention d'office ; un pied écrit à la main (`OmniDialog.Footer`, `OmniDialogRequest.Footer`) la suit dans le même ordre.
- Réglages : un `OmniSwitch`, `OmniCheckBox`, `OmniNullableSwitch` ou `OmniNullableCheckBox` posé à côté du nom d'un `OmniSettingsTile` est étiqueté par ce nom, et un clic n'importe où sur la tuile le bascule, une seule fois, par l'activation native du libellé.
- Les cibles interactives auditées mesurent au moins 44 par 44 px, sans imposer cette taille au glyphe visible.
- Les noms accessibles, états, erreurs et pluriels proviennent des ressources ou d'un paramètre public explicite.
- Le clavier, le focus, les rôles ARIA et les annonces live font partie du contrat fonctionnel.
- Les styles sont statiques; aucun attribut `style`, gestionnaire JavaScript HTML ou `unsafe-eval` n'est autorisé.
- Les composants utilisent exclusivement les primitives OmniEurope. Aucune dépendance, copie, traduction ou adaptation de code, CSS, JavaScript, tests, commentaires ou assets d'une autre bibliothèque de composants n'est permise.

La preuve visuelle et interactive vient des sondes navigateur décrites dans `docs/testing.md`; le catalogue reste une illustration partielle et ne constitue pas une preuve exhaustive.
