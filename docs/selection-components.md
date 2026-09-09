# Sélecteurs et entrées avancées

Ce lot fournit des contrôles typés reliés à `EditContext`, avec sémantique native et styles exclusivement statiques.

## Options et sélections

`OmniOption<TValue>` porte la valeur, le texte, l'état désactivé et le groupe éventuel. Ce modèle alimente :

- `OmniDropDown<TValue>` et `OmniMultiSelect<TValue>` ;
- `OmniListBox<TValue>` et `OmniCheckBoxList<TValue>` ;
- `OmniRadioButtonList<TValue>` et `OmniRadioButtonListItem<TValue>` ;
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

## Entrées spécialisées

- `OmniDatePicker` lie une valeur `DateOnly?`, utilise le contrôle de date natif et valide les bornes.
- `OmniDateTimePicker` lie une valeur `DateTime?` en heure locale, utilise le contrôle `datetime-local` natif, valide les bornes et ajoute les secondes avec `ShowSeconds`.
- `OmniSlider` expose orientation, minimum, maximum, pas et valeur ARIA.
- `OmniColorPicker` accepte exclusivement le format hexadécimal `#RRGGBB` sans générer de style inline.
- `OmniUpload` valide nombre, taille et types MIME avant d'appeler le délégué applicatif.

## Téléversement

Les propriétés `MaximumFiles`, `MaximumFileSize` et `AllowedContentTypes` filtrent l'interface à partir de métadonnées fournies par le client. Elles ne constituent jamais une validation de sécurité du contenu reçu.

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
