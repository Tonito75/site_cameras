# PortalCameras

Portail web personnel pour surveiller des caméras IP — consultation des flux en direct, historique photo, détection d'animaux par IA, et alertes Discord.

## Architecture

```
site_cameras/
├── back/          # API .NET 10 (ASP.NET Core Minimal API)
├── front/         # Interface React + TypeScript
├── docker-compose.yml       # Dev
└── docker-compose.prod.yml  # Production (NAS via CIFS)
```

Les deux services tournent dans des conteneurs Docker sur un réseau interne `cameras_internal`. En production, le frontend est exposé via un reverse proxy Caddy (`network_main`), et l'historique photo est monté depuis un NAS (CIFS).

---

## Backend — `back/`

**Stack :** .NET 10, ASP.NET Core Minimal API, YARP, Serilog, Scalar

### Authentification

Basée sur des **cookies HTTP-only** (ASP.NET Core Cookie Authentication). Un seul utilisateur, mot de passe configuré dans `appsettings.json`. Les tentatives de login sont loguées avec l'IP.

- `POST /api/login` — authentification (formulaire)
- `POST /api/logout`
- `GET /api/me` — vérifie si la session est active

### Endpoints caméras

Tous protégés par `RequireAuthorization`.

- `GET /api/cameras` — liste les caméras configurées (nom, IP, URL, groupe)
- `GET /api/cameras/{name}/ping` — ping ICMP de la caméra
- `GET /api/cameras/{name}/history?count=10&useAI=false` — images récentes depuis le dossier d'historique, avec filtrage optionnel par IA

### Endpoint images

- `GET /camera-history/{**path}` — sert les fichiers image depuis le dossier d'historique, avec protection contre le **path traversal**

### Reverse proxy (YARP)

Le backend embarque un reverse proxy YARP pour relayer les flux des caméras. Les routes sont définies dans `appsettings.json` (`ReverseProxy`) et toutes protégées par l'authentification.

### Services

| Service | Rôle |
|---|---|
| `PingService` | Ping ICMP d'une caméra par IP |
| `IOService` | Lecture du système de fichiers (liste de JPG par date) |
| `DateService` | Calcul des dossiers par date, formatage "il y a X" en français |
| `DetectThingsService` | Appel HTTP vers une API externe de détection d'animaux (multipart/form-data) |
| `DiscordService` | Envoi de notifications via webhook Discord |

### Configuration (`appsettings.json`)

```json
{
  "AllowedOrigins": "http://localhost:5173",
  "WebHookUrl": "https://discord.com/api/webhooks/...",
  "BaseHistoryFolder": "/chemin/vers/historique",
  "ApiDetectThingsUrl": "http://localhost:8000/detect-animal",
  "Authentication": { "Password": "..." },
  "Cameras": [
    { "Name": "Cam1", "Ip": "192.168.1.10", "Url": "...", "HistoryFolder": "cam1", "Group": "Jardin" }
  ],
  "ReverseProxy": { ... }
}
```

### Logging

Serilog — console + fichiers journaliers dans `logs/`.

### Documentation API

Scalar est exposé sur `/scalar/v1` (dev uniquement).

---

## Frontend — `front/`

**Stack :** React 19, TypeScript, Vite, MUI (Material UI v7), React Router v7

### Pages

| Page | Route | Description |
|---|---|---|
| `LoginPage` | `/login` | Formulaire de connexion |
| `HomePage` | `/` | Liste des caméras par groupe, statut en ligne (ping parallèle) |
| `AllStreamsPage` | `/streams` | Tous les flux caméra en iframes |
| `CameraHistoryPage` | `/camerahistory/:name` | Historique photo d'une caméra, avec option de filtre IA |

### Client API (`src/api/client.ts`)

Centralise les appels vers le backend. Lit l'URL de base depuis un fichier `config.json` (injecté en production via volume Docker), ce qui permet de configurer l'URL sans rebuild de l'image.

### Déploiement frontend

L'image Docker utilise **Nginx** pour servir le build statique. Un fichier `config.json` est monté en volume pour fournir l'URL du backend à runtime.

---

## Docker

### Dev

```bash
docker compose up --build
```

### Production

```bash
docker compose -f docker-compose.prod.yml up -d
```

En production, le volume `camera_history` est monté depuis un partage NAS via CIFS. Les credentials NAS et le chemin des settings sont passés par variables d'environnement (`.env`).

Variables attendues :

| Variable | Rôle |
|---|---|
| `DOCKER_SETTINGS_PATH` | Chemin vers les fichiers de config (`appsettings`, `config.json`) |
| `NAS_ADDR` | Adresse IP du NAS |
| `NAS_USER` / `NAS_PASSWORD` | Credentials CIFS |
