# Localisation

`OmniEurope.Blazor` fournit le marqueur public `AppStrings` et ses ressources dans les 24 langues officielles de l'Union européenne : le français en culture neutre (`AppStrings.resx`, sans suffixe), puis `en`, `bg`, `cs`, `da`, `de`, `el`, `es`, `et`, `fi`, `ga`, `hr`, `hu`, `it`, `lt`, `lv`, `mt`, `nl`, `pl`, `pt`, `ro`, `sk`, `sl` et `sv`, en cultures neutres (`pt` et non `pt-BR`) : une culture régionale comme `de-AT` retombe sur `de`. Chaque langue autre que le français est livrée en assembly satellite (`lib/net10.0/<langue>/OmniEurope.Blazor.resources.dll`), et `eng/Test-Package.ps1` échoue si le paquet en perd une. L'hôte active le contrat une seule fois :

```csharp
builder.Services.AddOmniEuropeBlazor();
```

Les composants utilisent `IStringLocalizer<AppStrings>`. Les noms de marque, identifiants, valeurs techniques et contenus fournis par le consommateur ne sont pas traduits automatiquement.

## Remplacer un texte du paquet

Un texte qui dépend du contexte métier se remplace de deux façons :

- par paramètre, quand le composant expose le texte (libellé, titre, texte d'action) : un paramètre `string?` dont la valeur `null` garde le texte localisé ([public-api-conventions.md](public-api-conventions.md)). Les textes de mécanique n'ont pas de paramètre par instance (pagination et filtres d'`OmniDataGrid`, boutons d'`OmniPager`) ;
- depuis les ressources de l'hôte, pour n'importe quel texte du paquet :

  ```csharp
  builder.Services.AddOmniEuropeBlazor();
  builder.Services.AddOmniEuropeTextOverrides<HostStrings>();
  ```

  `AddOmniEuropeTextOverrides<THostResource>` (à appeler après `AddOmniEuropeBlazor`) enregistre `OmniTextOverrideLocalizer` à la place du localiseur du paquet. Une clé de l'hôte nommée du préfixe (`Omni_` par défaut, second argument de la méthode) suivi de la clé du paquet l'emporte, dans chaque culture que l'hôte traduit : `Omni_ConnectionReconnectNow` remplace `ConnectionReconnectNow`. Une clé que l'hôte ne définit pas garde le texte du paquet, et un remplacement n'ajoute aucune clé que le paquet n'a pas.

## Textes accordés avec un nombre

Un texte dont la formulation dépend d'un nombre porte une forme par catégorie de pluriel de sa langue, dans le bloc `plural` de la syntaxe de message ICU :

```text
{0, plural, one {# fichier} other {# fichiers}} au plus
Možete odabrati najviše {0, plural, one {# datoteku} few {# datoteke} other {# datoteka}}.
```

- Le premier élément du bloc est l'indice de l'argument compté ; `#` écrit ce nombre dans la forme choisie. Les marqueurs `{n}` gardent leur sens .NET, format compris (`{0:N0}`), et un texte peut porter plusieurs blocs et des marqueurs ordinaires autour.
- La catégorie vient des règles de pluriel cardinal du CLDR d'Unicode pour la langue d'affichage (`CurrentUICulture`) : `one` et `other` dans la plupart des langues ; `few` en plus en tchèque, slovaque, croate, lituanien et roumain ; `few` et `many` en polonais ; `two` et `few` en slovène ; `zero` en letton ; `two`, `few` et `many` en irlandais et en maltais. Le français range 0 avec 1. Le portugais livré est celui du Portugal : seul 1 est `one`. Une langue que le paquet ne livre pas affiche le texte français et suit donc la règle française.
- `=N` nomme un nombre exact et passe avant la règle (`=0 {aucun fichier}`). Une catégorie absente retombe sur `other`, qui est obligatoire.
- Seuls les nombres entiers sont rangés ; un nombre à décimales prend `other`. Le reste de la syntaxe ICU (`select`, guillemets par apostrophe, blocs imbriqués, décalages) n'est pas lu.
- Un texte sans bloc reste une chaîne de format ordinaire. La forme « Libellé : {0} », qui n'a rien à accorder, reste employée là où elle se lit naturellement (tchèque, polonais, lituanien, letton surtout).
- Un remplacement de l'hôte (`Omni_UploadMaximumFiles`) peut porter ses propres blocs, ou aucun.

Sont accordés : les comptes de fichiers d'`OmniUpload`, les longueurs des messages de `OmniDataAnnotationsValidator`, les jours, mois et années d'`OmniRelativeTime`, les lignes d'`OmniLogViewer`, d'`OmniUnifiedDiff`, de l'import de tableau d'`OmniHtmlEditor` et des exports Markdown, les résultats d'`OmniAutocomplete` et le compte d'`OmniMultiSelect`. `PluralTextTests` vérifie la règle de chaque langue, que chaque bloc des 24 fichiers porte exactement les catégories de sa langue, et les nombres qui étaient faux (roumain 20, croate 1 et 21, letton 21 et 31).

Ces textes se lisent par les composants du paquet. Un code qui lirait lui-même une de ces clés par `IStringLocalizer<AppStrings>` avec des arguments recevrait une `FormatException` : le bloc n'est pas une chaîne de format .NET.

## Garanties

Une clé absente constitue une régression : les tests doivent vérifier `ResourceNotFound == false` dans les cultures prises en charge. La ressource française sans suffixe reste le repli déterministe de la bibliothèque. `LibraryTranslationTests` exige un fichier pour chacune des 23 langues autres que le français, et, pour chacune, exactement les clés de la ressource neutre, les mêmes arguments numérotés (chacun compté une fois, format et bloc `plural` ignorés ; les formes sont vérifiées par `PluralTextTests`) et aucune valeur vide là où le français en a une : une clé ajoutée sans ses traductions fait échouer la suite au lieu d'afficher du français dans une page allemande ou grecque.

## Vitrine

La vitrine est proposée en anglais (ressources neutres, langue par défaut, culture `en-IE`) et en français (`fr-FR`), au choix dans un sélecteur de l'en-tête (décision du propriétaire, PLAN-005). Le choix est gardé dans `localStorage` (`omnieurope.showcase.culture`) et la page se recharge dans la nouvelle culture, avec le fragment ICU EFIGS qui couvre les deux langues ; l'euro reste le symbole des montants. `ShowcaseText_EveryCulturePresentHasTheNeutralKeys` exige que la ressource française de `ShowcaseStrings` et de chaque famille de la galerie (`Resources/Gallery/<Famille>Strings.fr.resx`) ait exactement les clés de la ressource neutre. Les exemples de la galerie (`site/OmniEurope.Blazor.Showcase.Examples`) gardent leurs textes en anglais écrits en dur ; les textes du paquet suivent la langue choisie.

## Limites connues des traductions

Les 23 langues autres que le français ont été relues le 2026-09-29 par des agents, sans locuteur natif (`5b8827a`). Ce que la relecture a laissé en l'état, à reprendre avec un locuteur natif ou par une décision :

- **Formes de pluriel** : les formes ajoutées le 2026-09-30 (341 textes, voir plus haut) ont été écrites sans locuteur natif, comme le reste. À relire en priorité : l'irlandais (mutation du nom après le nombre : `2 chomhad`, `8 gcomhad`, `8 mbliana`) et le maltais (`2 jumejn`, `12-il jum`, singulier à partir de 20).
- **Lettres de l'indication de date** (`DateHintDay`, `DateHintMonth`, `DateHintYear`, recette R-84, 2026-10-28) : les lettres jour, mois, année de la saisie de date des grilles (`jj/mm/aaaa`, `TT.MM.JJJJ`, `gg/mm/aaaa`, `pp.kk.vvvv`...) sont les initiales usuelles de chaque langue, choisies sans locuteur natif.
- **Alerte et avertissement** : un seul mot en néerlandais (« Waarschuwingen ») et en suédois (« varning ») pour le composant Alertes et pour la sévérité avertissement.
- **Dossier** : « comhad » en irlandais et « fajl » en maltais désignent aussi un fichier informatique.
- **Irlandais** : « Deais » (tableau de bord) et « tiomantas » (commit git) n'ont pas été vérifiés sur téarma.ie.
- **Maltais** : les emprunts « Progress » et « Debugging » sont gardés.
- **Survol** : rendu par « Überfahren » en allemand et « prejdenie myšou » en slovaque, termes cohérents mais qui ne sont pas ceux de Microsoft.
- **Notifications** : « Notifikationer » en danois, là où Microsoft écrit « Meddelelser ».
- **En-tête et pied de carte** : le suédois et le finnois emploient les termes d'en-tête et de pied de page.
- **Info-bulle** : « Popisek » et « Tip » coexistent en tchèque, « Popis » et « Tip » en slovaque.
- **Paires de contraste** de la vitrine (`ContrastPair*`) : en roumain et en bulgare, les jetons « attention » et « échec » suivent la source française et diffèrent des noms de sévérité des alertes.
- **Bouton discret** : deux noms dans toutes les langues, comme dans la source (« Discret » dans la démonstration des boutons, « Fantôme » dans celle des états).
- **Grec** : les en-têtes des colonnes du sélecteur d'heure (ώ, λ, δ) restent abrégés, faute de largeur.
- **Letton** : « Atcelt atsaukšanu » (rétablir) dans les menus et « atkārtot » dans l'aide clavier de la carte mentale ; « pilnvara » pour un jeton d'accès, « marķieri » pour les jetons de thème.
- **Italien** : « caricamento » vaut pour le chargement et pour le téléversement.
- **Grille, disposition des colonnes** ([PLAN-019](plans/archive/serie-1/PLAN-019-preferences-en-base.md)) : les 8 textes ajoutés pour le choix, le déplacement et la réinitialisation des colonnes (`GridColumnChooser`, `GridColumnMoveLeft`, `GridColumnMoveRight`, `GridColumnShown`, `GridColumnHidden`, `GridColumnMoved`, `GridResetLayout`, `GridLayoutReset`) ont été traduits dans les 23 langues sans relecture par un locuteur natif.
- **Zone de danger des réglages** (PLAN-001, lot 7, 2026-10-12) : les 3 textes du paquet (`SettingsDangerZoneTitle`, `SettingsDangerActionText`, `SettingsDangerConfirmMessage`) et les 15 textes de sa démonstration dans la vitrine (`SettingsProject`, `SettingsDeleteProject`, `SettingsResetHistory`, `SettingsTransferProject` et leurs compléments) ont été traduits dans les 23 langues sans relecture par un locuteur natif ; le texte du bouton reprend le « Supprimer » déjà traduit de la carte mentale (`MindMapDelete`).
- **Modèle de mail et saisie libre** (PLAN-001, lot 8, 2026-10-13) : les 9 textes du paquet pour `OmniMailTemplateEditor` (`MailTemplateEditorLabel`, `MailTemplateEditorSubject`, `MailTemplateEditorVariables`, `MailTemplateEditorTemplate`, `MailTemplateEditorBody`, `MailTemplateEditorPreview`, `MailTemplateEditorPreviewSubject`, `MailTemplateEditorVariableHint`, `MailTemplateEditorSendTest`) et les 11 textes des démonstrations (`DemoInputsModel*`, `DemoEditorMail*`, dont le modèle de mail d'exemple) ont été traduits dans les 23 langues sans relecture par un locuteur natif.
- **Blocs des modèles de mails** (recette R1-75, 2026-10-16) : les 7 textes du paquet pour le groupe « Insérer un bloc » d'`OmniMailTemplateEditor` (`MailTemplateEditorBlocks`, `MailTemplateEditorBanner`, `MailTemplateEditorBannerInfo`, `MailTemplateEditorBannerSuccess`, `MailTemplateEditorBannerWarning`, `MailTemplateEditorBannerDanger`, `MailTemplateEditorButton`) et le texte de démonstration `DemoEditorMailPieces` ont été traduits dans les 23 langues sans relecture par un locuteur natif.
