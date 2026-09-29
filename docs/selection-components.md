# Sélecteurs et entrées avancées

Ce lot fournit des contrôles typés reliés à `EditContext`, avec sémantique native et styles exclusivement statiques.

## Options et sélections

`OmniRating` lie une valeur entière nullable par `Value`/`ValueChanged`/`ValueExpression`.
`Maximum` vaut cinq et doit être positif ; cliquer une étoile choisit son rang et remplit les
étoiles précédentes. La notation est un groupe radio natif (`role="radiogroup"`, un `input type="radio"`
masqué derrière chaque étoile) : un seul arrêt de tabulation, les flèches déplacent et cochent sans
script, `aria-invalid` suit la validation. `ReadOnly` rend une seule image (`role="img"`) qui annonce la
valeur (« Note : 3 sur 5 »), hors de l'ordre de tabulation, et dessine ses étoiles sans les estomper ;
`Disabled` garde les boutons radio, désactivés et estompés, et l'emporte sur `ReadOnly`. `Label`
(`string?`) nomme le groupe, « Note » localisé par défaut, ou le libellé d'un `OmniFormField` englobant
(`aria-labelledby`). L'édition participe à `EditContext`.

`OmniOption<TValue>` porte la valeur, le texte, l'état désactivé et le groupe éventuel. Ce modèle alimente :

- `OmniDropDown<TValue>` (seule source des choix, par `Options` ; `Filterable` ajoute une recherche, `Label` le nomme, rendu en `aria-label` seulement s'il est posé) et `OmniMultiSelect<TValue>` ;
- `OmniListBox<TValue, TSelection>` et `OmniCheckBoxList<TValue>` (`Error` marque la liste `aria-invalid` et la fait décrire par l'erreur) ;
- `OmniRadioButtonList<TValue>`, qui dessine lui-même chaque bouton radio ; `Error` (facultatif) dessine sous les choix, dans le `fieldset`, la ligne d'erreur d'`OmniFormField` (glyphe décoratif, dans une région live polie, identifiant `{Id}-error`, ou `{Name}-error` sans identifiant), marque le groupe `aria-invalid="true"` (aussi quand l'`EditContext` porte une erreur), le fait décrire par cette ligne après `AriaDescribedBy` et borde chaque bouton radio de la couleur de danger ; les options désactivées sont marquées comme telles ;
- `OmniSelectBar<TValue>`, qui dessine lui-même chaque option.

`OmniListBox` est une liste native toujours ouverte (`select` à `size`) dont la hauteur est donnée
par `VisibleRows`. Elle choisit une option ; avec `Multiple="true"`, elle devient un `select multiple`
et en choisit plusieurs (Ctrl ou Maj avec un clic). `TSelection` est déduit de `@bind-Value` : une
valeur (`TValue`, ou sa forme nullable) pour une seule option, une collection (`IReadOnlyList<TValue>`,
un tableau) avec `Multiple` ; une liaison qui ne correspond pas au mode lève `InvalidOperationException`.
Les options désactivées ne sont jamais retenues.

`OmniMultiSelect<TValue>` tient sur une seule ligne, ouvre sa liste de cases à cocher à la demande et
résume la sélection : `Placeholder` quand rien n'est choisi, le texte de l'option quand il n'y en a
qu'une, le décompte au-delà. Les libellés de repli et le bouton de désélection proviennent des
ressources `MultiSelectEmpty`, `MultiSelectSelected` et `MultiSelectClear`.

Il accepte en plus une recherche et deux templates. `Filterable` ajoute un champ qui réduit la liste
aux options dont le texte contient la saisie ; `FilterText` se lie dans les deux sens pour que la
page sache ce qui a été tapé, et la remise à `null` vide le champ ; `FilterPlaceholder` remplace le
libellé et le texte indicatif du champ de recherche. `OptionTemplate` dessine
une option à côté de sa case à cocher, `FooterContent` occupe le bas du panneau, hors de la zone
défilante : ensemble, ils donnent le sélecteur d'étiquettes qui propose de créer celle que la recherche
n'a pas trouvée. Le filtre porte toujours sur le texte de l'option, quoi que dessine le template, et
`MultiSelectNoMatch` est affiché lorsque la recherche ne laisse rien, un panneau vide se lisant comme
un contrôle qui n'a pas chargé ses options.

Le panneau se ferme sur Échap (le focus revient au résumé) et, tant que `CloseOnOutsideClick`
reste à `true`, sa valeur par défaut, sur un appui ailleurs dans la page. Un appui dans un élément
marqué `data-omni-keep-open` ne compte jamais comme extérieur : une colonne de réglages qui modifie le
champ ouvert porte cet attribut et ne le referme pas. La fermeture passe par `omni-focus.js`, le seul
à voir un appui hors du composant ; l'écouteur de document n'existe que pendant que le panneau est
ouvert. Désactivé (`Disabled`), le résumé ne s'ouvre plus et sort de l'ordre de tabulation.

Une option désactivée (`OmniOption.Disabled`) se lit comme telle avant qu'on essaie de la choisir :
atténuée avec un curseur interdit dans les listes de choix, la sélection multiple et les suggestions,
hachurée dans `OmniSelectBar`. Une barre entièrement désactivée porte `.omni-select-bar--disabled` et
`aria-disabled`, et garde son option choisie d'un accent pâli. Trop large pour sa place, la barre
défile sous un chevron de chaque côté qui cache encore des options, comme les onglets, sans barre de
défilement ; les chevrons sont hors de l'ordre de tabulation, chaque option restant atteignable.

```razor
<OmniMultiSelect TValue="Guid" Options="tags" @bind-Value="selectedTagIds"
                 Filterable="true" @bind-FilterText="search">
    <OptionTemplate Context="tag">
        <svg class="tag-swatch" viewBox="0 0 10 10" aria-hidden="true" focusable="false">
            <circle cx="5" cy="5" r="5" fill="@ColorOf(tag.Value)" />
        </svg>
        <span>@tag.Text</span>
    </OptionTemplate>
    <FooterContent>
        @if (CanCreate)
        {
            <OmniButton Variant="OmniButtonVariant.Ghost" OnClick="CreateAsync">+ @search</OmniButton>
        }
    </FooterContent>
</OmniMultiSelect>
```

`OmniAutocomplete<TValue>` reçoit une fonction asynchrone annulable, applique un délai (`Debounce`, un `TimeSpan`, 250 ms par défaut) et annonce le nombre de résultats dans une région live. Une option n'est engagée dans le modèle qu'après sélection explicite. Le clavier est celui d'une liste déroulante combinée : flèches, Début et Fin parcourent les suggestions (`aria-activedescendant`, options en `li role="option"`), Entrée choisit, Échap ferme. Un échec de recherche lève `OnSearchError` avec l'exception et affiche `SearchErrorMessage` (texte localisé par défaut).

```razor
<OmniAutocomplete TValue="Guid"
                  Search="SearchPeopleAsync"
                  Debounce="TimeSpan.FromMilliseconds(300)"
                  @bind-Value="personId" />
```

`HighlightMatches`, vrai par défaut, marque dans chaque suggestion les lettres qui correspondent à la
saisie (`mark.omni-autocomplete__match`), sans tenir compte de la casse ni des accents dans la culture
courante : « liege » marque « Liège ». Le texte lu par un lecteur d'écran reste celui de l'option.

`OptionIconTemplate` dessine, avant le texte de chaque suggestion, une icône ou un drapeau qui dit ce qu'est
l'entrée ; il est décoratif (`aria-hidden`), le texte et son surlignage nommant toujours l'option.

## Menus : profil et contextuel

Tous les menus du paquet prennent les mêmes entrées, `OmniMenuItem` (`Icon`, `Href`, `Disabled`, `Tone`, `OnClick` en `EventCallback<MouseEventArgs>`, contenu enfant obligatoire), et le même moteur (`omni-focus.js`, voir [accessibility-contract.md](contracts/accessibility-contract.md)) : rendus dans le portail d'`OmniComponentsHost`, flèches, Début et Fin, Échap qui rend le focus au déclencheur, Tab qui ferme. Une entrée destructrice prend `Tone="OmniTone.Danger"`. Un menu à déclencheur (`OmniOverflowMenu`, `OmniSplitButton`, `OmniProfileMenu`) se pilote par `Open` (`bool?`, `null` = état propre) et `OpenChanged` ; `Label` nomme le déclencheur, `MenuLabel` la liste.

`OmniProfileMenu` est un bouton (`omni-profile-menu__trigger`, qui porte `Id`) qui ouvre son menu dans le portail. Il se ferme sur Échap, une fois une entrée
choisie et, avec `CloseOnOutsideClick` (vrai par défaut), sur un appui ailleurs dans la page, avec la
même exception `data-omni-keep-open` que la sélection multiple. `Disabled` désactive le bouton.

Sans `Summary`, le déclencheur est l'avatar : un disque du gris de la palette qui porte `Initials` (quelques lettres, dans le texte de la page) ou, sans elles, le glyphe d'utilisateur. Il est décoratif, le déclencheur est nommé par `Label`, qui doit donc nommer le compte ; sa cible atteint 44 px par une zone transparente autour du disque, et le focus y dessine l'anneau sur le cercle. `Header` (facultatif) place l'identité en haut du menu ouvert, à côté d'un grand avatar : son premier élément se lit comme le nom, les suivants en détails atténués (rôle, organisation, lien vers le profil). L'en-tête est rendu hors de la liste `role="menu"`, qui ne contient que des entrées ; le panneau `omni-profile-menu__popup--header` porte alors la surface flottante. Chaque entrée est un `OmniMenuItem` ; une ligne secondaire va dans son contenu enfant.

`OmniContextMenu` s'ouvre au pointeur sur un clic droit (lié ou non : `Open` est un `bool?`, `null` laissant le menu tenir son état ; `Disabled` le désactive), sous son déclencheur à la touche Menu ou à
Maj+F10, et un second clic droit le déplace. `omni-focus.js` pose sa position par le CSSOM
(`--omni-menu-x`, `--omni-menu-y`, attribut `data-omni-placed`) et le ramène dans la fenêtre près
d'un bord. Rendu par le portail d'`OmniComponentsHost`, le menu est retrouvé par son identifiant
(`{Id}-menu`) et non par une référence d'élément ; chaque entrée du portail est indexée par son
propriétaire et suit les entrées du menu quand elles changent pendant qu'il est ouvert. Flèches,
Début et Fin parcourent les entrées `role="menuitem"`, Échap ferme et rend le focus au déclencheur ;
un appui ailleurs ferme le menu et laisse le focus là où il a été posé.

`OmniOverflowMenu` est le menu « ⋮ » d'une ligne, d'une carte ou d'un en-tête : trois points seuls, sans
cadre (bouton `Ghost`, nommé par `Label`, « Plus d'actions » par défaut), qui ouvrent une liste verticale
d'`OmniMenuItem` (`Icon` dans une colonne fixe, puis le libellé ; `Disabled`, `Tone`), nommée par
`MenuLabel`. `Id` est posé sur le déclencheur. Le
déclencheur ouvre et ferme le menu sur son propre clic (`aria-haspopup="menu"`, `aria-expanded`) ; un
appui sur lui ne compte pas comme un appui extérieur, qui rouvrait le menu. Le menu est rendu par le
portail d'`OmniComponentsHost` (aucune zone défilante ni `container-type` de la page ne le coupe ou ne
devient le bloc contenant de sa position fixe), posé sous le déclencheur, bord de fin contre bord de fin,
et ramené dans la fenêtre : décalé sur le côté près d'un bord, ouvert au-dessus faute de place en bas,
recalé au défilement et au redimensionnement. Flèches, Début et Fin parcourent les entrées ; Échap
ferme et rend le focus au déclencheur ; Tab ferme et continue depuis le déclencheur ; flèche bas ou haut
sur le déclencheur ouvre sur la première ou la dernière entrée. Une entrée choisie ferme d'abord le menu,
le focus rendu au déclencheur, puis lance son action : un dialogue ouvert par l'action le trouve là et
l'y rend à sa fermeture. Dans une ligne de grille, le déclencheur prend la hauteur des badges comme les
autres boutons à icône seule ; les entrées ne sont pas des boutons et gardent leur géométrie de menu.

## Cartes à choisir : `OmniSelectableCard` et `OmniSelectableCardGroup`

`OmniSelectableCard` est un choix qui mérite plus qu'un bouton radio : `Icon`, `Title` (obligatoire),
`Description` en ligne atténuée, `ChildContent` en texte complémentaire (du texte et des badges, rien
d'interactif : la carte est un bouton). `Multiple="false"` (par défaut) en fait un choix parmi plusieurs
(`role="radio"`) ; seule, elle se lie par `Value`/`ValueChanged` (`bool`), et `ValueChanged` reçoit toujours
`true`, même sur la carte déjà choisie, pour que l'hôte puisse enchaîner (passer à l'étape suivante).
`Multiple="true"` en fait une option (`role="checkbox"`) qui bascule. Le choix se lit à l'encadré et à la
teinte d'accent seuls, sans coche : le cadre garde 2 px dans tous les états, le texte ne bouge jamais.
Le survol penche le cadre et le fond vers l'accent, en clair comme en sombre. `Disabled` garde l'état
(une option imposée par une autre), laisse la carte focalisable et l'annonce indisponible
(`aria-disabled`).

`OmniSelectableCardGroup<TValue, TSelection>` lie la sélection de plusieurs cartes par `@bind-Value` : les cartes viennent d'`Options` (le texte devient le titre), ou sont écrites dans le groupe avec leur `Choice`, ou les deux (options d'abord). Choix unique par défaut (`role="radiogroup"`, `TSelection` étant une valeur) : une seule carte est dans l'ordre de tabulation (la choisie, sinon la première disponible) et les flèches, Début et Fin déplacent le choix et le focus, comme un groupe radio natif. `Multiple="true"` en fait un groupe de cases (`role="group"`, `TSelection` une collection). `Label` nomme le groupe (à défaut, le libellé d'un `OmniFormField` englobant par `aria-labelledby`) ; `Disabled` désactive toutes les cartes. Dans un groupe, `Value`, `ValueChanged` et `Multiple` de chaque carte sont ceux du groupe.

## Entrées spécialisées

- `OmniDatePicker` (`DateOnly?`), `OmniTimePicker` (`TimeOnly?`) et `OmniDateTimePicker` (`DateTime?`, heure locale) sont un champ texte et un bouton qui ouvre un panneau maison sur le calque des surfaces flottantes (jetons `--omni-overlay-*`), sous le champ. Le contrôle natif de date dessinait sa fenêtre lui-même, sans style possible ; il n'est plus utilisé.
- Saisie : la date suit l'ordre et les séparateurs de la date courte de la culture, sur deux chiffres (`21/09/2026` en français, `09/21/2026` en anglais américain) ; l'heure est toujours sur 24 heures (`HH:mm` ; `HH:mm:ss` avec `ShowSeconds`, que seul `OmniDateTimePicker` propose). La lecture accepte aussi la forme ISO (`yyyy-MM-dd`, `yyyy-MM-ddTHH:mm`) et ce que l'analyseur de la culture comprend. Le texte indicatif vient du motif (`jj/mm/aaaa`, `hh:mm`), remplaçable par `Placeholder`.
- Calendrier : semaine commençant au premier jour de la culture (lundi en français), mois nommés dans sa langue, six semaines toujours, aujourd'hui entouré (`aria-current="date"`), jour choisi plein à l'accent (`aria-selected`), mois précédent et suivant. `role="grid"` nommé par le titre du mois, rangées `role="row"`, en-têtes `columnheader` au nom complet du jour. Un seul jour est dans l'ordre de tabulation ; les flèches déplacent d'un jour ou d'une semaine, Page précédente et suivante d'un mois (d'un an avec Maj), Début et Fin vont au début et à la fin de la semaine, Entrée et Espace choisissent. Pied : Aujourd'hui et Effacer ; choisir un jour, Aujourd'hui ou Effacer ferme le panneau et rend le focus au bouton.
- Heure : deux colonnes qui défilent (`listbox`), heures de 00 à 23 et minutes par `Step` (un `TimeSpan` de minutes entières, 5 minutes par défaut, de 1 à 30), et une troisième colonne de secondes pour un `OmniDateTimePicker` à `ShowSeconds` ; un choix s'applique aussitôt, les flèches, Début et Fin parcourent une colonne, Tab passe à la suivante. Pied : Maintenant (l'heure du `TimeProvider` enregistré par l'hôte, l'horloge système sinon, arrondie au pas inférieur) et Valider, qui ferme et rend le focus au bouton. La date et heure met le calendrier et les colonnes côte à côte : un jour garde l'heure (minuit s'il n'y en a pas), une heure garde le jour (aujourd'hui s'il n'y en a pas).
- Bornes : `Minimum` et `Maximum` désactivent les jours, heures et minutes hors bornes et arrêtent le clavier à la borne ; une saisie hors bornes est refusée et marque le champ invalide, sans être ramenée à la borne. Dans la date et heure, un choix du panneau qui sortirait des bornes (un jour dont l'heure gardée dépasse) est ramené à la borne la plus proche.
- Fermeture : un appui hors du sélecteur ferme le panneau et laisse le focus où il a été posé ; Échap le ferme, rend le focus au bouton et ne remonte pas (un dialogue qui contient le sélecteur reste ouvert). Un seul panneau de sélecteur est ouvert à la fois dans la page. Ce câblage est dans `omni-focus.js` (`attachPicker`, `detachPicker`, `focusPickerItem`) ; il n'écrit aucun style, seul `scrollTop` des colonnes est posé pour centrer la valeur choisie.
- Densité : les cases du calendrier mesurent `--omni-cal-cell`, la marge du panneau `--omni-pop-pad`, le champ et son bouton suivent `--omni-control-height`, les éléments des colonnes `--omni-item-pad-y`. Les cases et les éléments des colonnes restent sous 44 px en densité compacte et confortable, comme la maquette les dessine ; le bouton du champ atteint 44 px de cible par une zone invisible.
- Écart assumé avec la maquette : dans la date seule, choisir un jour ferme le panneau (la maquette le laissait ouvert, faute de bouton Valider dans ce pied).
- `OmniSlider` expose orientation, minimum, maximum, pas, valeur ARIA et `aria-invalid`. `ValueChanged` suit chaque pas du
  glissement ; `OnValueCommit` est levé une seule fois au relâchement, avec la valeur finale (recherche
  d'une position dans un média), et sans lui aucun gestionnaire `change` n'est posé.
- `OmniColorPicker` accepte exclusivement le format hexadécimal `#RRGGBB` sans générer de style inline.
- `OmniUpload` valide nombre, taille et types MIME avant d'appeler le délégué applicatif ; `Id` est posé sur le champ fichier.

## Téléversement

Les propriétés `MaximumFiles`, `MaximumFileSize` et `AllowedContentTypes` filtrent l'interface à partir de métadonnées fournies par le client. Elles ne constituent jamais une validation de sécurité du contenu reçu.

`Accept` fixe l'attribut `accept` du champ fichier (`.csv,text/csv`) : il ne filtre que la fenêtre de sélection du système, les fichiers restant contrôlés par `AllowedContentTypes`. Sans lui, l'attribut est tiré de `AllowedContentTypes`, comme avant.

Le champ est une zone de dépôt : le contrôle natif la couvre, invisible, si bien qu'un clic ouvre le sélecteur et qu'un fichier déposé n'importe où sur la zone y arrive sans script. La zone annonce ses limites (types, taille par fichier, nombre) et les relie au champ par `aria-describedby`.

Lié par `@bind-Files`, le champ tient une liste d'`OmniUploadFile` (nom, taille, type) : les fichiers que l'application a déjà, puis ceux que l'utilisateur ajoute. Chaque ligne a son icône, sa taille et un bouton de retrait qui lève `FileRemoved` avec l'entrée, puis `FilesChanged` avec la liste sans elle. Une sélection acceptée s'ajoute à la liste avec `Multiple`, la remplace sinon, et seulement après la réussite d'`Upload` quand ce délégué est fourni ; `MaximumFiles` compte alors la liste entière. Non lié, le champ montre la dernière sélection, sans retrait.

```razor
<OmniUpload Multiple="true" MaximumFiles="5" @bind-Files="attachments" FileRemoved="DeleteAsync" />

@code {
    private IReadOnlyList<OmniUploadFile> attachments = [new("rapport.pdf", 1_258_291, "application/pdf")];

    private Task DeleteAsync(OmniUploadFile file) => storage.DeleteAsync(file.Name);
}
```

Le délégué `Upload` reçoit un `OmniUploadRequest`. L'hôte y ouvre chaque fichier avec `request.OpenReadStream(file)`, contrôle sa signature réelle, son format, sa taille effectivement lue et les règles métier, et refuse le lot par `request.Reject("message public")` : le champ affiche alors ce message comme erreur, sans proposer de nouvel essai ni ajouter les fichiers à sa liste. `OpenReadStream` applique la limite configurée et le jeton d'annulation ; la même requête porte `CancellationToken` et `ReportProgress`. Le composant n'envoie rien seul et la validation doit être répétée à la frontière serveur qui persiste le contenu.

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
        if (request.Files.Any(file => file.Size == 0))
        {
            request.Reject("Un fichier est vide.");
            return;
        }

        for (var index = 0; index < request.Files.Count; index++)
        {
            await using var stream = request.OpenReadStream(request.Files[index]);
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
