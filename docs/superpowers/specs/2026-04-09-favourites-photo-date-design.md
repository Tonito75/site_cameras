# Design — Tri des favoris par date photo

**Date :** 2026-04-09

## Contexte

La page des favoris affiche les photos triées par `AddedAt` (date d'ajout du favori). L'objectif est de trier par la date réelle de la photo, extraite du nom de fichier.

Format du nom de fichier : `Villy_00_20260220000120.jpg`
→ les 14 chiffres avant l'extension = `yyyyMMddHHmmss` = 2026-02-20 00:01:20

## Modèle

Ajout d'un champ `DateTime PhotoDate` à `Favourite`.

```csharp
public DateTime PhotoDate { get; set; }
```

Pour les favoris existants en base, la date sera parsée depuis `RelativePath`. Si le parse échoue, fallback sur `AddedAt`.

## Parsing du nom de fichier

Méthode statique `ParsePhotoDate(string relativePath, DateTime fallback)` dans un helper (ex. `FavouriteHelper`).

- Extrait le nom de fichier depuis le chemin
- Cherche 14 chiffres consécutifs avant `.jpg` via regex : `(\d{14})\.\w+$`
- Parse avec `DateTime.ParseExact(..., "yyyyMMddHHmmss", CultureInfo.InvariantCulture)`
- Retourne `fallback` en cas d'échec

## Migration EF

Passage de `EnsureCreated()` à `db.Database.Migrate()` dans `Program.cs`.

Migration `AddPhotoDateToFavourites` :
1. Ajoute colonne `PhotoDate` nullable (`datetime2`)
2. Backfill via `migrationBuilder.Sql(...)` : pour chaque ligne, tente d'extraire les 14 chiffres du chemin et met à jour `PhotoDate` (fallback sur `AddedAt`)
3. Passe `PhotoDate` en `NOT NULL`

## Endpoints modifiés

- `GET /api/favourites` : `OrderByDescending(f => f.PhotoDate)`
- `GET /api/cameras/{name}/favourites` : `OrderByDescending(f => f.PhotoDate)`
- `POST /api/favourites` : calcule `PhotoDate` via le helper au moment de créer le favori

## Frontend

Aucun changement nécessaire. La date affichée restera `addedAt` (date d'ajout), le tri est géré côté serveur.
