# Préférences de l'utilisateur

Une préférence est un réglage qu'un utilisateur choisit et qu'il retrouve d'une visite à l'autre :
l'apparence (mode, thème, palette, police, densité, fond animé, tailles du texte et des contrôles,
largeur du contenu), la disposition et l'état d'une grille, ou toute valeur de l'application. Depuis la
`1.8.0`, le paquet les tient toutes par un seul service, `OmniPreferences`, qui garde chaque valeur dans
le `localStorage` du navigateur et, quand l'application en fournit une, dans sa base, par utilisateur
connecté. La base est prioritaire : une valeur plus récente en base remplace celle du navigateur et
l'affichage se met à jour seul. Sans base, tout reste dans le navigateur, par la même API.

Le paquet n'a pas de base. Il définit le contrat `IOmniPreferenceStore` ; l'application l'implémente sur
sa propre base et l'enregistre.

## Enregistrement

`AddOmniEuropeBlazor` enregistre `OmniPreferences` et `OmniAppearanceState` (scoped : un circuit en
Blazor Server, l'application en WebAssembly), chacun seulement si l'hôte n'a pas déjà le sien. Sans autre
appel, les préférences vivent dans le navigateur seul.

`AddOmniEuropePreferences<TStore>` branche la base : `TStore` (une classe qui implémente
`IOmniPreferenceStore`) est enregistré scoped et remplace un store enregistré avant. L'appel enregistre
aussi `OmniPreferences`, si bien qu'il se place avant ou après `AddOmniEuropeBlazor`. Son second argument
règle la synchronisation :

```csharp
builder.Services.AddOmniEuropeBlazor();
builder.Services.AddOmniEuropePreferences<EfPreferenceStore>(options =>
    options.SyncInterval = TimeSpan.FromMinutes(5));
```

`OmniPreferencesOptions.SyncInterval` vaut une minute par défaut. `Timeout.InfiniteTimeSpan` coupe la
synchronisation périodique ; toute autre valeur doit être positive, sinon la construction du service lève
`ArgumentOutOfRangeException`. Les dates des valeurs viennent du `TimeProvider` de l'hôte quand il en
enregistre un, de l'horloge système sinon, toujours en UTC.

## API

- `GetAsync<T>(key)` : la valeur, ou `default` quand la préférence n'existe pas, a été supprimée ou ne se
  relit plus comme `T` (sa forme a changé) ; l'appelant garde alors ses propres défauts.
- `SetAsync<T>(key, value)` : écrit la valeur, sérialisée en JSON (conventions web de `System.Text.Json`).
  Une valeur égale à la valeur du cache n'écrit rien ; tant que le cache n'est pas ouvert (prérendu, échec de
  `GetScopeAsync`), chaque appel lève `Changed` et écrit dans la base quand il y en a une.
- `RemoveAsync(key)` : supprime la préférence, qui se relit ensuite comme `default`.
- `SyncAsync()` : synchronise tout de suite, comme le fait la synchronisation automatique.
- `Changed` : levé quand une préférence prend une nouvelle valeur, avec un `OmniPreferenceChangedEventArgs` :
  la clé (`Key`), la valeur JSON (`Value`, null pour une suppression) et l'origine (`Origin`,
  `OmniPreferenceChangeOrigin` : `Local`, écrit par ce circuit ; `Store`, valeur plus récente lue dans la
  base ; `OtherTab`, écrit par un autre onglet du même navigateur, levé aussi sans changement de valeur,
  voir « Autres onglets »).
  L'événement peut être levé hors du fil du rendu (synchronisation périodique) : un composant passe par
  `InvokeAsync`. Un gestionnaire qui lève est journalisé et n'empêche pas les autres.

Une clé ne peut être ni nulle ni blanche. Les clés qui commencent par `omni.` sont celles du paquet ;
l'application préfixe les siennes de son propre nom.

## Lecture, écriture et synchronisation

Le navigateur sert de cache : une entrée `localStorage` par préférence, sous
`omni:pref:<portée>:<clé>`, qui porte la valeur JSON, sa date et un drapeau « pas encore confirmée par la
base » ; la date de la dernière synchronisation est sous `omni:pref-sync:<portée>`. Quand le navigateur
refuse son stockage (données de site bloquées, quota atteint), les entrées vivent en mémoire le temps de
la page, sans erreur.

- **Lecture : le cache d'abord.** `GetAsync` rend la valeur du cache tout de suite. La première lecture
  d'une clé dans le circuit interroge aussi la base en arrière-plan ; si elle tient une valeur plus
  récente, le cache est remplacé et `Changed` est levé (`Store`). Quand le cache n'a rien (nouveau poste,
  navigateur vidé), la base est lue avant de répondre, pour que le premier affichage soit déjà juste, et
  sa valeur est mise en cache.
- **Écriture : le cache, puis la base.** `SetAsync` et `RemoveAsync` écrivent dans le cache, marqué non
  confirmé, lèvent `Changed` (`Local`), puis écrivent dans la base. Une fois la base d'accord, l'entrée
  est confirmée. Un échec de la base (une exception) est journalisé ; la valeur reste dans le navigateur,
  non confirmée.
- **Rejeu des valeurs non confirmées.** À chaque synchronisation, chaque entrée non confirmée est
  réécrite dans la base, sauf si la base tient pour cette clé une valeur aussi récente ou plus récente :
  c'est alors la valeur de la base qui remplace l'entrée (à date égale, la base gagne).
- **La base gagne quand elle est plus récente.** La synchronisation demande à la base tout ce qui a
  changé depuis la dernière (`GetChangedSinceAsync`) et remplace chaque valeur du cache plus ancienne, en
  levant `Changed` (`Store`) quand la valeur change. La marque de la dernière synchronisation est la date
  la plus récente renvoyée par la base, jamais l'horloge du poste : un changement daté par un autre poste
  n'est pas sauté parce que les deux horloges diffèrent.
- **Synchronisation automatique**, avec une base seulement : toutes les `SyncInterval`, et chaque fois que
  l'onglet redevient visible (`visibilitychange`). Une synchronisation déjà en cours n'est pas doublée.
- **Autres onglets.** Un autre onglet du même navigateur qui écrit une préférence de la même portée est
  suivi par l'événement `storage` du navigateur : `Changed` est levé (`OtherTab`), sans appel à la base.
  Il l'est pour toute écriture de l'autre onglet sous la portée, même quand la valeur ne change pas
  (entrée confirmée par la base, valeur déjà connue) : un gestionnaire compare lui-même s'il y tient.
  Cela vaut aussi sans base.
- **Suppressions (pierres tombales).** Une suppression n'efface pas l'entrée : elle l'écrit avec une
  valeur nulle et sa date, dans le cache et dans la base, pour que les autres postes la reçoivent comme
  tout changement. La base garde donc ces lignes à valeur nulle et les renvoie dans
  `GetChangedSinceAsync`.
- **Partition par utilisateur.** Le préfixe du cache contient la portée que donne la base
  (`GetScopeAsync`), si bien que deux utilisateurs d'un même navigateur ne lisent jamais les valeurs
  l'un de l'autre dans ce cache. Sans base, la portée est `local`. La garantie vaut pour le cache des
  préférences seulement : l'instantané du premier affichage (`OmniThemeScope.SnapshotKey`, rejoué par
  `omni-boot.js` avant le rendu, taille du texte et des contrôles comprises) n'est pas partitionné. Il
  garde le dernier aspect dessiné dans ce navigateur, quel que soit l'utilisateur : le suivant voit cet
  aspect au premier affichage, puis le sien dès que la page est rendue. Un hôte qui veut l'éviter suffixe
  `SnapshotKey` par utilisateur. La portée est redemandée à la base à chaque lecture, écriture et
  synchronisation : si elle a changé (déconnexion puis connexion d'un autre compte, même sans appel à
  `ResetUserAsync`), le service change de partition au premier usage suivant, comme le fait
  `ResetUserAsync`. Les valeurs non confirmées de l'ancien utilisateur restent dans sa partition, elles ne
  sont jamais poussées dans la base du nouveau. Une base occupée par un autre appel n'est pas attendue par une lecture ou une écriture
  (le cache répond sans délai) ; la synchronisation et l'écriture vers la base revérifient alors la portée.

Une valeur illisible (JSON abîmé, forme changée) se relit comme `default` ; une entrée de cache illisible
est traitée comme absente, la valeur de la base ou le défaut de l'appelant prenant le relais.

### Prérendu

Pendant le prérendu, JavaScript n'est pas joignable : avec une base, lectures et écritures vont
directement à la base et le cache est laissé de côté ; sans base, elles ne lisent rien et ne gardent
rien. Le cache s'ouvre au premier appel du rendu interactif. Un hôte qui a une base rend donc
l'apparence de l'utilisateur dès le premier octet (attributs de l'`OmniThemeScope` ; les jetons suivent
avec le script, l'instantané d'`omni-boot.js` couvrant le premier affichage), et une grille à
`StateKey` lit son état dès le prérendu. Sans base, l'état d'une grille est restauré au rendu interactif.
Cette lecture fait attendre le prérendu : un `OmniThemeScope` qui suit l'état ne rend son contenu
qu'une fois la préférence lue, et le prérendu attend la réponse de la base aussi longtemps qu'elle tarde
(le paquet ne pose pas de délai). Une base lente retarde donc la première réponse de la page ; l'hôte
qui ne le veut pas borne le temps de `GetAsync` dans son `IOmniPreferenceStore` (une base qui échoue ou
est annulée laisse l'apparence par défaut) ou lie un paramètre d'apparence de son `OmniThemeScope`.

## Le contrat `IOmniPreferenceStore`

L'implémentation est résolue dans la portée de l'utilisateur et ne répond que pour l'utilisateur
connecté : le paquet ne lui passe jamais d'identité, elle la trouve elle-même.

| Membre | Ce qu'il fait |
|---|---|
| `GetAsync(key)` | Une préférence de l'utilisateur, une pierre tombale (`Value` nulle) si elle a été supprimée, ou `null` si la base ne l'a jamais tenue. |
| `GetChangedSinceAsync(since)` | Toutes les préférences de l'utilisateur modifiées à `since` ou après (`UpdatedAt >= since`), pierres tombales comprises, dans n'importe quel ordre ; `null` demande tout. |
| `SaveAsync(preference)` | Écrit la préférence en remplaçant celle de même clé, avec la date reçue (`preference.UpdatedAt`, ne pas la remplacer par l'heure du serveur : c'est elle que comparent les postes). Une pierre tombale se garde avec sa date. |
| `GetScopeAsync()` | La partition du cache propre à l'utilisateur (voir plus bas). |

Une exception d'un membre n'interrompt rien : une lecture qui échoue laisse répondre le cache, une
écriture qui échoue est rejouée à la synchronisation suivante, un `GetScopeAsync` qui échoue fait
travailler le service avec la base seule, sans cache, jusqu'à ce qu'il réussisse (il est redemandé à
l'appel suivant). Seul `SyncAsync` appelé par l'application rend l'exception d'un
`GetChangedSinceAsync` en échec ; la synchronisation automatique la journalise.

### `GetScopeAsync`

`GetScopeAsync` est appelée à chaque lecture, écriture et synchronisation : elle doit être peu coûteuse
(lire l'état d'authentification, sans accès disque ni réseau).

La portée est écrite en clair dans les clés du `localStorage`. Elle ne doit donc pas être une donnée
personnelle (adresse électronique, nom, identifiant brut) mais une valeur opaque, par exemple une
empreinte SHA-256 de l'identifiant de l'utilisateur. Elle ne doit être ni blanche ni contenir de
deux-points (sinon `InvalidOperationException`), et le même utilisateur doit toujours recevoir la même
valeur.

Tant qu'aucun utilisateur n'est connecté, le magasin rend `OmniPreferences.NoUserScope` (la chaîne vide) : les
préférences se lisent à leurs valeurs par défaut, sans avertissement, et le magasin n'est appelé pour rien
d'autre ; une valeur écrite entre-temps reste dans la mémoire du circuit, ni dans le navigateur ni en base. La
portée est redemandée à la lecture ou à l'écriture suivante.

### Changement d'utilisateur sans recharger la page

Après une connexion, une déconnexion ou un changement de compte, l'hôte appelle
`OmniPreferences.ResetUserAsync()` pour que le nouvel utilisateur voie ses préférences tout de suite (sans
cet appel, le service s'en aperçoit au premier usage suivant, voir la partition par utilisateur plus haut). La portée est redemandée au magasin, le cache du navigateur de l'ancien
utilisateur est laissé tel quel (il est cloisonné par utilisateur), les valeurs écrites sans utilisateur sont
abandonnées, puis chaque préférence lue ou écrite dans le circuit est relue pour le nouvel utilisateur et
levée par `Changed` avec l'origine `OmniPreferenceChangeOrigin.UserChanged` (valeur nulle quand il n'en a
pas). `OmniAppearanceState`, les grilles à `StateKey` et tout écouteur de l'hôte qui traite les origines autres
que `Local` affichent alors les préférences du nouvel utilisateur, ou leurs valeurs par défaut. Une
synchronisation ou une écriture en cours se termine d'abord, pour l'ancien utilisateur.

```csharp
// Dans le gestionnaire de l'état d'authentification de l'hôte.
authentication.AuthenticationStateChanged += async _ => await preferences.ResetUserAsync();
```

### Table conseillée

| Colonne | Type | Rôle |
|---|---|---|
| `Id` | entier auto-incrémenté | Clé primaire technique. |
| `UserId` | texte (450) | L'utilisateur, tel que l'application l'identifie. |
| `Key` | texte (200) | La clé de la préférence (`omni.appearance`, `omni.grid.commandes`). |
| `Value` | texte long, nullable | La valeur JSON ; nulle pour une pierre tombale. |
| `UpdatedAt` | date avec fuseau (UTC) | La date reçue de `SaveAsync`. |

Index unique sur (`UserId`, `Key`), index sur (`UserId`, `UpdatedAt`) pour `GetChangedSinceAsync`.

### Exemple EF Core (Blazor Server)

L'entité et sa configuration :

```csharp
public sealed class UserPreference
{
    public long Id { get; set; }
    public required string UserId { get; set; }
    public required string Key { get; set; }
    public string? Value { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

// Dans AppDbContext :
public DbSet<UserPreference> UserPreferences => Set<UserPreference>();

protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    modelBuilder.Entity<UserPreference>(entity =>
    {
        entity.ToTable("UserPreferences");
        entity.Property(p => p.UserId).HasMaxLength(450);
        entity.Property(p => p.Key).HasMaxLength(200);
        entity.HasIndex(p => new { p.UserId, p.Key }).IsUnique();
        entity.HasIndex(p => new { p.UserId, p.UpdatedAt });
    });
}
```

Le store, qui trouve l'utilisateur par `AuthenticationStateProvider` :

```csharp
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;
using OmniEurope.Blazor.Components;

public sealed class EfPreferenceStore(
    IDbContextFactory<AppDbContext> contexts,
    AuthenticationStateProvider authentication) : IOmniPreferenceStore
{
    public async Task<OmniPreference?> GetAsync(string key, CancellationToken cancellationToken = default)
    {
        if (await UserIdAsync() is not { } userId)
        {
            return null;
        }

        await using var db = await contexts.CreateDbContextAsync(cancellationToken);
        return await db.UserPreferences
            .AsNoTracking()
            .Where(p => p.UserId == userId && p.Key == key)
            .Select(p => new OmniPreference(p.Key, p.Value, p.UpdatedAt))
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<OmniPreference>> GetChangedSinceAsync(DateTimeOffset? since, CancellationToken cancellationToken = default)
    {
        if (await UserIdAsync() is not { } userId)
        {
            return [];
        }

        await using var db = await contexts.CreateDbContextAsync(cancellationToken);
        var query = db.UserPreferences.AsNoTracking().Where(p => p.UserId == userId);
        if (since is { } from)
        {
            query = query.Where(p => p.UpdatedAt >= from);
        }

        return await query
            .Select(p => new OmniPreference(p.Key, p.Value, p.UpdatedAt))
            .ToListAsync(cancellationToken);
    }

    public async Task SaveAsync(OmniPreference preference, CancellationToken cancellationToken = default)
    {
        if (await UserIdAsync() is not { } userId)
        {
            // Visiteur anonyme : la valeur reste dans son navigateur.
            return;
        }

        await using var db = await contexts.CreateDbContextAsync(cancellationToken);
        var row = await db.UserPreferences
            .SingleOrDefaultAsync(p => p.UserId == userId && p.Key == preference.Key, cancellationToken);
        if (row is null)
        {
            row = new UserPreference { UserId = userId, Key = preference.Key };
            db.UserPreferences.Add(row);
        }

        row.Value = preference.Value;
        row.UpdatedAt = preference.UpdatedAt;
        await db.SaveChangesAsync(cancellationToken);
    }

    public async ValueTask<string> GetScopeAsync(CancellationToken cancellationToken = default)
    {
        if (await UserIdAsync() is not { } userId)
        {
            return "anonymous";
        }

        // Empreinte opaque : jamais l'identifiant lui-même dans le navigateur.
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes("omni-preferences|" + userId));
        return Convert.ToHexStringLower(hash);
    }

    private async Task<string?> UserIdAsync()
    {
        var state = await authentication.GetAuthenticationStateAsync();
        return state.User.FindFirstValue(ClaimTypes.NameIdentifier);
    }
}
```

Et son enregistrement :

```csharp
builder.Services.AddDbContextFactory<AppDbContext>(options => options.UseSqlServer(connectionString));
builder.Services.AddOmniEuropeBlazor();
builder.Services.AddOmniEuropePreferences<EfPreferenceStore>();
```

Remarques sur l'exemple :

- `IDbContextFactory` plutôt qu'un `DbContext` injecté : un circuit Blazor Server vit longtemps et le
  service appelle le store en arrière-plan (synchronisation périodique, relecture d'une clé) pendant que
  l'utilisateur écrit ; un `DbContext` ne supporte pas deux opérations simultanées.
- `AuthenticationStateProvider` répond pendant le prérendu comme dans le circuit. `IHttpContextAccessor`
  ne convient qu'au rendu statique et au prérendu : dans un circuit interactif, `HttpContext` n'est pas
  celui de l'utilisateur courant et peut être nul.
- Deux onglets qui créent la même clé au même instant peuvent heurter l'index unique ; l'exception est
  journalisée par `OmniPreferences` et l'écriture rejouée à la synchronisation suivante, qui trouve alors
  la ligne.
- Un visiteur anonyme partage la portée `anonymous` de ce navigateur, sans rien en base.
- SQLite : le fournisseur EF Core ne sait pas comparer ni trier les `DateTimeOffset` en SQL ; y stocker
  `UpdatedAt` par un convertisseur de valeur (`DateTimeOffsetToBinaryConverter`, par exemple).

### WebAssembly et Interactive Auto

En WebAssembly, le service tourne dans le navigateur : le store ne peut pas joindre la base. Il appelle
une API HTTP de l'application (quatre points d'entrée qui correspondent aux quatre membres), et c'est le
serveur qui identifie l'utilisateur par son authentification (cookie, jeton), jamais par un identifiant
envoyé par le client. Le serveur peut aussi calculer la portée et la renvoyer. En Interactive Auto, le
projet serveur enregistre le store EF Core, le projet client le store HTTP, chacun par
`AddOmniEuropePreferences`. En WebAssembly, les dates viennent de l'horloge du navigateur.

### Base de démonstration de la vitrine

La vitrine statique n'a ni serveur ni base. Ouverte avec `?store=demo` (et `&sync=<secondes>` pour la
synchronisation périodique, coupée sinon), elle enregistre un store de démonstration
(`site/OmniEurope.Blazor.Showcase/Preferences/DemoPreferenceStore.cs`) qui garde ses lignes dans le
`sessionStorage` de l'onglet, hors du cache `localStorage`, pour que la sonde `preferences` éprouve le
chemin de la base dans un vrai navigateur. Ce n'est pas un modèle de store de production : il n'a ni
utilisateur ni base, et sa portée est fixe (`demo`). Sans le paramètre, la vitrine n'enregistre aucun
store.

## Clés utilisées par le paquet

| Clé | Contenu |
|---|---|
| `omni.appearance` (`OmniAppearanceState.PreferenceKey`) | L'apparence tenue par `OmniAppearanceState`, voir [foundation-components.md](foundation-components.md), « Apparence portée par le paquet ». |
| `omni.grid.<StateKey>` | L'état d'une grille à `StateKey` (filtres, tris, largeurs, ordre et visibilité des colonnes), voir [data-components.md](data-components.md), « Enregistrement de l'état ». |

## Limites

- Pas de détection de conflit entre deux modifications simultanées (décision du propriétaire du
  2026-10-11) : la valeur la plus récente, d'après sa date, l'emporte entière, même si les deux postes
  avaient changé des réglages différents de la même clé (l'apparence est une seule préférence). Les dates
  sont celles des postes qui écrivent (le serveur en Blazor Server, le navigateur en WebAssembly) : une
  horloge très en avance peut faire gagner une valeur plus ancienne.
- Le cache suit la base au rythme de `SyncInterval`, au retour sur l'onglet et à la première lecture de
  chaque clé : un changement fait sur un autre poste n'arrive pas instantanément.
- Le paquet ne borne ni le nombre ni la taille des préférences, et ne purge pas les pierres tombales :
  c'est à la base de l'application d'en décider.

## Migration vers la 1.8.0

La `1.8.0` est une rupture assumée en version mineure (décision du propriétaire du 2026-10-11, voir
[versioning.md](versioning.md)).

### État des grilles : `IOmniDataGridStateStore` retiré

`IOmniDataGridStateStore`, `OmniLocalStorageDataGridStateStore` et le paramètre
`OmniDataGrid.StateStore` sont retirés. `StateKey` suffit : l'état passe par `OmniPreferences`, sous la
clé `omni.grid.<StateKey>`, dans la base si l'application enregistre un `IOmniPreferenceStore`, dans le
navigateur sinon.

- Retirer `StateStore="…"` du balisage : un paramètre inconnu ne compile plus (`OE0001`).
- L'ancien magasin par défaut (`OmniLocalStorageDataGridStateStore`) est repris automatiquement : une
  grille qui ne trouve pas sa préférence mais trouve dans `localStorage`, sous la `StateKey` seule, une
  entrée de ce magasin (un objet portant `Filters`, `Sorts` et `ColumnWidths`) la reprend une fois comme
  préférence, puis supprime l'entrée. Toute autre valeur rangée sous cette clé appartient à
  l'application et n'est pas touchée. Cette reprise n'a lieu que sans base (portée `local`) : la clé nue
  est commune à tous les utilisateurs du navigateur, et avec un `IOmniPreferenceStore` elle pourrait
  porter la grille d'un autre utilisateur, que la reprise écrirait dans la base du premier venu. Avec une
  base, l'entrée est laissée en place ; l'application qui veut la reprendre le fait elle-même, pour
  l'utilisateur qu'elle sait propriétaire de ce navigateur. Un stockage refusé par le navigateur (mode
  privé, stockage désactivé) ne reprend rien et n'empêche pas la grille de s'afficher.
- La visibilité restaurée est bornée à ce que le lecteur peut défaire : sans `AllowColumnHide`, ou pour
  une colonne `Hideable="false"`, la visibilité déclarée l'emporte sur l'état enregistré (qui est gardé
  et revaut dès que l'hôte permet de masquer), et un état qui masquerait toutes les colonnes garde la
  première colonne déclarée visible dans l'ordre affiché.
- Un `IOmniDataGridStateStore` propre à l'application n'est pas repris automatiquement. Ses données se
  recopient côté application dans son `IOmniPreferenceStore`, sous la clé `omni.grid.<StateKey>` : la
  chaîne JSON que la grille confiait à `SaveAsync` devient la `Value` telle quelle (sans numéro de
  version, elle se relit comme la version 1, sans perte : ordre et visibilité restent ceux déclarés),
  avec la date de la copie.

### Apparence : `OmniAppearanceState` remplace l'état d'apparence de l'application

Une application qui tenait son propre service d'apparence (cookie ou `localStorage`) et le liait aux
composants peut le retirer :

1. Délier les paramètres d'apparence pour que les composants suivent `OmniAppearanceState` :
   `OmniThemeScope` sans `Appearance`, `Density`, `Preset`, `Palette`, `Font`, `BackdropMotion` ;
   `OmniAppearanceWindow` et `OmniAppearanceSettings` sans aucun des dix-huit paramètres d'apparence
   (les neuf valeurs et leurs `…Changed`), l'hôte ne gardant que l'ouverture ; `OmniAppMenu` sans
   `Appearance`, `AppearanceChanged` ni `Preset`. Le choix se fait au premier rendu, composant par
   composant, et vaut pour tous ses paramètres : un composant qui en écrit un seul est contrôlé par
   l'hôte pour tous.
2. Retirer le code qui posait la taille du texte et des contrôles sur la racine du document : un
   `OmniThemeScope` qui suit l'état pose `data-oe-text-size` et `data-oe-control-size` lui-même.
3. Reprendre, si on le souhaite, les valeurs de l'ancien stockage : tant que
   `OmniPreferences.GetAsync<JsonElement?>(OmniAppearanceState.PreferenceKey)` rend `null`, l'application
   écrit les anciennes valeurs par les méthodes `Set…Async` d'`OmniAppearanceState`, puis efface son
   ancien cookie ou son entrée. Le paquet ne lit pas l'ancien stockage d'une application.

Un composant dont l'hôte lie explicitement les paramètres continue de fonctionner exactement comme
avant : il ne lit ni n'écrit l'état. `OmniAppMenu.AppearanceChanged` n'est plus `[EditorRequired]` :
l'omettre ne produit plus d'avertissement, et sans `Appearance` ni `Preset` le menu suit l'état.

### `OmniThemeScope` sans paramètre d'apparence : il suit désormais l'état enregistré

C'est une rupture de comportement. Un `OmniThemeScope` posé sans aucun de ses six paramètres
d'apparence (`Appearance`, `Density`, `Preset`, `Palette`, `Font`, `BackdropMotion`) dessinait jusqu'ici
les défauts ; il suit maintenant `OmniAppearanceState` : il prend l'apparence enregistrée, pose la taille
du texte et des contrôles sur la racine du document, et ne rend rien, contenu compris, tant que la
préférence n'est pas lue. Sans base, le prérendu ne lit rien et rend aussitôt les défauts, puis le
rendu interactif lit le cache du navigateur avant de dessiner le scope ; avec une base, le prérendu attend
sa réponse (voir « Prérendu »).

Pour garder l'ancien comportement, lier n'importe quel paramètre d'apparence, même à sa valeur par
défaut (`<OmniThemeScope Appearance="OmniAppearance.System">`) : le scope est alors contrôlé par l'hôte,
rend son contenu dès le premier rendu et ne lit ni n'écrit l'état.

### Largeur du contenu

Le réglage « Largeur du contenu » de la fenêtre d'apparence agit sur l'`OmniMain` le plus externe (celui
qui n'est pas imbriqué dans un autre) : « Pleine largeur » le dessine en `OmniLayoutWidth.FullWidth`,
« Centré » garde son `ContentWidth`, la colonne de l'hôte. Le défaut de `ContentWidth` étant `FullWidth`, une
application qui veut que « Centré » centre réellement pose `ContentWidth="OmniLayoutWidth.Wide"` (90 rem)
ou `ContentWidth="OmniLayoutWidth.Content"` (72 rem) sur son élément principal. Une application qui
appliquait elle-même `FullWidthChanged` à son `OmniMain` peut retirer ce câblage.
