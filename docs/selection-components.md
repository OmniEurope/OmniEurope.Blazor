# Sélecteurs et entrées avancées

Ce lot fournit des contrôles typés reliés à `EditContext`, avec sémantique native et styles exclusivement statiques.

## Options et sélections

`OmniRating` lie une valeur entière nullable par `Value`/`ValueChanged`/`ValueExpression`.
`Maximum` vaut cinq et doit être positif ; cliquer une étoile choisit son rang et remplit les
étoiles précédentes. `ReadOnly` conserve l'affichage sans modification, `Disabled` désactive
la saisie, et `Label` nomme le groupe et chaque choix. L'édition participe à `EditContext`.

`OmniOption<TValue>` porte la valeur, le texte, l'état désactivé et le groupe éventuel. Ce modèle alimente :

- `OmniDropDown<TValue>` et `OmniMultiSelect<TValue>` ;
- `OmniListBox<TValue>` et `OmniCheckBoxList<TValue>` ;
- `OmniRadioButtonList<TValue>` et `OmniRadioButtonListItem<TValue>` ; `Error` (facultatif) dessine sous les choix, dans le `fieldset`, la ligne d'erreur d'`OmniFormField` (glyphe décoratif, `role="alert"`, identifiant `{Id}-error`, ou `{Name}-error` sans identifiant), marque le groupe `aria-invalid="true"`, le fait décrire par cette ligne après l'`aria-describedby` passé par l'hôte et borde chaque bouton radio de la couleur de danger ;
- `OmniSelectBar<TValue>` et `OmniSelectBarItem<TValue>`.

`OmniMultiSelect<TValue>` expose deux formes par `Presentation`. `List`, la valeur par défaut, reste
une liste native toujours ouverte dont la hauteur est donnée par `VisibleRows`. `Compact` tient sur
une seule ligne, ouvre sa liste à la demande et résume la sélection : `Placeholder` quand rien n'est
choisi, le texte de l'option quand il n'y en a qu'une, le décompte au-delà. Les libellés de repli et
le bouton de désélection proviennent des ressources `MultiSelectEmpty`, `MultiSelectSelected` et
`MultiSelectClear`.

La forme `Compact` accepte en plus une recherche et deux templates. `Filterable` ajoute un champ qui
réduit la liste aux options dont le texte contient la saisie ; `FilterText` se lie dans les deux sens
pour que la page sache ce qui a été tapé, et la remise à `null` vide le champ. `OptionTemplate` dessine
une option à côté de sa case à cocher, `FooterTemplate` occupe le bas du panneau, hors de la zone
défilante : ensemble, ils donnent le sélecteur d'étiquettes qui propose de créer celle que la recherche
n'a pas trouvée. Le filtre porte toujours sur le texte de l'option, quoi que dessine le template, et
`MultiSelectNoMatch` est affiché lorsque la recherche ne laisse rien, un panneau vide se lisant comme
un contrôle qui n'a pas chargé ses options.

`Filterable` exige `Presentation="OmniMultiSelectPresentation.Compact"` et lève sinon : la forme
`List` est un `select multiple` natif, sans place pour un champ et adressant ses options par position,
donc un filtre silencieusement ignoré y serait le vrai piège.

Le panneau compact se ferme sur Échap (le focus revient au résumé) et, tant que `CloseOnOutsideClick`
reste à `true`, sa valeur par défaut, sur un appui ailleurs dans la page. Un appui dans un élément
marqué `data-omni-keep-open` ne compte jamais comme extérieur : une colonne de réglages qui modifie le
champ ouvert porte cet attribut et ne le referme pas. La fermeture passe par `omni-focus.js`, le seul
à voir un appui hors du composant ; l'écouteur de document n'existe que pendant que le panneau est
ouvert. Désactivé (`Disabled`), le résumé ne s'ouvre plus et sort de l'ordre de tabulation.

Une option désactivée (`OmniOption.Disabled`) se lit comme telle avant qu'on essaie de la choisir :
atténuée avec un curseur interdit dans les listes de choix, la forme compacte et les suggestions,
hachurée dans `OmniSelectBar`. Une barre entièrement désactivée porte `.omni-select-bar--disabled` et
`aria-disabled`, et garde son option choisie d'un accent pâli. Trop large pour sa place, la barre
défile sous un chevron de chaque côté qui cache encore des options, comme les onglets, sans barre de
défilement ; les chevrons sont hors de l'ordre de tabulation, chaque option restant atteignable.

```razor
<OmniMultiSelect TValue="Guid" Options="tags" @bind-Value="selectedTagIds"
                 Presentation="OmniMultiSelectPresentation.Compact"
                 Filterable="true" @bind-FilterText="search">
    <OptionTemplate Context="tag">
        <svg class="tag-swatch" viewBox="0 0 10 10" aria-hidden="true" focusable="false">
            <circle cx="5" cy="5" r="5" fill="@ColorOf(tag.Value)" />
        </svg>
        <span>@tag.Text</span>
    </OptionTemplate>
    <FooterTemplate>
        @if (CanCreate)
        {
            <OmniButton Variant="OmniButtonVariant.Ghost" OnClick="CreateAsync">+ @search</OmniButton>
        }
    </FooterTemplate>
</OmniMultiSelect>
```

`OmniAutocomplete<TValue>` reçoit une fonction asynchrone annulable, applique un délai de debounce et annonce le nombre de résultats dans une région live. Une option n'est engagée dans le modèle qu'après sélection explicite.

```razor
<OmniAutocomplete TValue="Guid"
                  Search="SearchPeopleAsync"
                  DebounceMilliseconds="250"
                  @bind-Value="personId" />
```

`HighlightMatches`, vrai par défaut, marque dans chaque suggestion les lettres qui correspondent à la
saisie (`mark.omni-autocomplete__match`), sans tenir compte de la casse ni des accents dans la culture
courante : « liege » marque « Liège ». Le texte lu par un lecteur d'écran reste celui de l'option.

`OptionIcon` dessine, avant le texte de chaque suggestion, une icône ou un drapeau qui dit ce qu'est
l'entrée ; il est décoratif (`aria-hidden`), le texte et son surlignage nommant toujours l'option.

## Menus : profil et contextuel

`OmniProfileMenu` repose sur l'élément natif `details`. Il se ferme sur Échap, une fois une entrée
choisie et, avec `CloseOnOutsideClick` (vrai par défaut), sur un appui ailleurs dans la page, avec la
même exception `data-omni-keep-open` que la sélection multiple.

Sans `Summary`, le déclencheur est l'avatar : un disque du gris de la palette qui porte `Initials` (quelques lettres, dans le texte de la page) ou, sans elles, le glyphe d'utilisateur. Il est décoratif, le déclencheur est nommé par `Label`, qui doit donc nommer le compte ; sa cible atteint 44 px par une zone transparente autour du disque, et le focus y dessine l'anneau sur le cercle. `Header` (facultatif) place l'identité en haut du menu ouvert, à côté d'un grand avatar : son premier élément se lit comme le nom, les suivants en détails atténués (rôle, organisation, lien vers le profil). L'en-tête est rendu hors de la liste `role="menu"`, qui ne contient que des entrées ; le panneau `omni-profile-menu__panel` porte alors la surface flottante. `OmniProfileMenuItem` gagne `Icon` (un disque décoratif avant le texte) et `Description` (une ligne atténuée sous le texte, lue avec lui) ; sans l'un ni l'autre, l'entrée rend son contenu seul, comme avant.

`OmniContextMenu` s'ouvre au pointeur sur un clic droit, sous son déclencheur à la touche Menu ou à
Maj+F10, et un second clic droit le déplace. `omni-focus.js` pose sa position par le CSSOM
(`--omni-menu-x`, `--omni-menu-y`, attribut `data-omni-placed`) et le ramène dans la fenêtre près
d'un bord. Rendu par le portail d'`OmniComponentsHost`, le menu est retrouvé par son identifiant
(`{Id}-menu`) et non par une référence d'élément ; chaque entrée du portail est indexée par son
propriétaire et suit les entrées du menu quand elles changent pendant qu'il est ouvert. Flèches,
Début et Fin parcourent les entrées `role="menuitem"`, Échap ferme et rend le focus au déclencheur ;
un appui ailleurs ferme le menu et laisse le focus là où il a été posé.

## Entrées spécialisées

- `OmniDatePicker` (`DateOnly?`), `OmniTimePicker` (`TimeOnly?`) et `OmniDateTimePicker` (`DateTime?`, heure locale) sont un champ texte et un bouton qui ouvre un panneau maison sur le calque des surfaces flottantes (jetons `--omni-overlay-*`), sous le champ. Le contrôle natif de date dessinait sa fenêtre lui-même, sans style possible ; il n'est plus utilisé.
- Saisie : la date suit l'ordre et les séparateurs de la date courte de la culture, sur deux chiffres (`21/09/2026` en français, `09/21/2026` en anglais américain) ; l'heure est toujours sur 24 heures (`HH:mm`, `HH:mm:ss` avec `ShowSeconds`). La lecture accepte aussi la forme ISO (`yyyy-MM-dd`, `yyyy-MM-ddTHH:mm`) et ce que l'analyseur de la culture comprend. Le texte indicatif vient du motif (`jj/mm/aaaa`, `hh:mm`), remplaçable par `Placeholder`.
- Calendrier : semaine commençant au premier jour de la culture (lundi en français), mois nommés dans sa langue, six semaines toujours, aujourd'hui entouré (`aria-current="date"`), jour choisi plein à l'accent (`aria-selected`), mois précédent et suivant. `role="grid"` nommé par le titre du mois, rangées `role="row"`, en-têtes `columnheader` au nom complet du jour. Un seul jour est dans l'ordre de tabulation ; les flèches déplacent d'un jour ou d'une semaine, Page précédente et suivante d'un mois (d'un an avec Maj), Début et Fin vont au début et à la fin de la semaine, Entrée et Espace choisissent. Pied : Aujourd'hui et Effacer ; choisir un jour, Aujourd'hui ou Effacer ferme le panneau et rend le focus au bouton.
- Heure : deux colonnes qui défilent (`listbox`), heures de 00 à 23 et minutes par `Step` (5 par défaut, de 1 à 30), trois avec les secondes ; un choix s'applique aussitôt, les flèches, Début et Fin parcourent une colonne, Tab passe à la suivante. Pied : Maintenant (l'heure de `TimeProvider`, arrondie au pas inférieur) et Valider, qui ferme et rend le focus au bouton. La date et heure met le calendrier et les colonnes côte à côte : un jour garde l'heure (minuit s'il n'y en a pas), une heure garde le jour (aujourd'hui s'il n'y en a pas).
- Bornes : `Minimum` et `Maximum` désactivent les jours, heures et minutes hors bornes et arrêtent le clavier à la borne ; une saisie hors bornes est refusée et marque le champ invalide, sans être ramenée à la borne. Dans la date et heure, un choix du panneau qui sortirait des bornes (un jour dont l'heure gardée dépasse) est ramené à la borne la plus proche.
- Fermeture : un appui hors du sélecteur ferme le panneau et laisse le focus où il a été posé ; Échap le ferme, rend le focus au bouton et ne remonte pas (un dialogue qui contient le sélecteur reste ouvert). Un seul panneau de sélecteur est ouvert à la fois dans la page. Ce câblage est dans `omni-focus.js` (`attachPicker`, `detachPicker`, `focusPickerItem`) ; il n'écrit aucun style, seul `scrollTop` des colonnes est posé pour centrer la valeur choisie.
- Densité : les cases du calendrier mesurent `--omni-cal-cell`, la marge du panneau `--omni-pop-pad`, le champ et son bouton suivent `--omni-control-height`, les éléments des colonnes `--omni-item-pad-y`. Les cases et les éléments des colonnes restent sous 44 px en densité compacte et confortable, comme la maquette les dessine ; le bouton du champ atteint 44 px de cible par une zone invisible.
- Écart assumé avec la maquette : dans la date seule, choisir un jour ferme le panneau (la maquette le laissait ouvert, faute de bouton Valider dans ce pied).
- `OmniSlider` expose orientation, minimum, maximum, pas et valeur ARIA.
- `OmniColorPicker` accepte exclusivement le format hexadécimal `#RRGGBB` sans générer de style inline.
- `OmniUpload` valide nombre, taille et types MIME avant d'appeler le délégué applicatif.

## Téléversement

Les propriétés `MaximumFiles`, `MaximumFileSize` et `AllowedContentTypes` filtrent l'interface à partir de métadonnées fournies par le client. Elles ne constituent jamais une validation de sécurité du contenu reçu.

Le champ est une zone de dépôt : le contrôle natif la couvre, invisible, si bien qu'un clic ouvre le sélecteur et qu'un fichier déposé n'importe où sur la zone y arrive sans script. La zone annonce ses limites (types, taille par fichier, nombre) et les relie au champ par `aria-describedby`.

Lié par `@bind-Files`, le champ tient une liste d'`OmniUploadFile` (nom, taille, type) : les fichiers que l'application a déjà, puis ceux que l'utilisateur ajoute. Chaque ligne a son icône, sa taille et un bouton de retrait qui lève `FileRemoved` avec l'entrée, puis `FilesChanged` avec la liste sans elle. Une sélection acceptée s'ajoute à la liste avec `Multiple`, la remplace sinon, et seulement après la réussite d'`Upload` quand ce délégué est fourni ; `MaximumFiles` compte alors la liste entière. Non lié, le champ montre la dernière sélection, sans retrait.

```razor
<OmniUpload Multiple="true" MaximumFiles="5" @bind-Files="attachments" FileRemoved="DeleteAsync" />

@code {
    private IReadOnlyList<OmniUploadFile> attachments = [new("rapport.pdf", 1_258_291, "application/pdf")];

    private Task DeleteAsync(OmniUploadFile file) => storage.DeleteAsync(file.Name);
}
```

Le délégué `Validate` reçoit un `OmniUploadRequest` avant `Upload`. L'hôte doit ouvrir chaque fichier avec `request.OpenReadStream(file)`, contrôler sa signature réelle, son format, sa taille effectivement lue et les règles métier, puis retourner un message public lorsqu'il refuse le lot. `OpenReadStream` applique la limite configurée et le jeton d'annulation. Le délégué `Upload` reçoit ensuite la même requête avec `CancellationToken` et `ReportProgress`. Le composant n'envoie rien seul et la validation doit être répétée à la frontière serveur qui persiste le contenu.

```razor
<OmniUpload Multiple="true"
            MaximumFiles="5"
            MaximumFileSize="10485760"
            AllowedContentTypes="allowedTypes"
            Upload="UploadAsync" />

@code {
    private readonly string[] allowedTypes = ["image/png", "image/jpeg"];

    private async Task UploadAsync(OmniUploadRequest request)
    {
        for (var index = 0; index < request.Files.Count; index++)
        {
            await using var stream = request.Files[index].OpenReadStream(10 * 1024 * 1024, request.CancellationToken);
            await using var destination = File.Create(GetDestinationPath());
            await stream.CopyToAsync(destination, request.CancellationToken);
            request.ReportProgress((index + 1d) / request.Files.Count * 100d);
        }
    }

    private static string GetDestinationPath() =>
        Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
}
```

L'exemple utilise un nom temporaire généré ; une application choisit son propre stockage durable. `IBrowserFile.ContentType` provient du client et ne remplace jamais une validation serveur du contenu réel, notamment par signature de fichier. Le nom fourni par le client ne doit pas être utilisé directement comme chemin de destination.
