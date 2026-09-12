[![Paypal](https://img.shields.io/badge/Paypal-Donate-success?style=for-the-badge&logo=paypal)](https://www.paypal.com/donate/?business=QT2Y72ABMYJNG&no_recurring=0&currency_code=AUD) 
[![Discord](https://img.shields.io/discord/674782527139086350?color=7289DA&label=Discord&style=for-the-badge&logo=discord)](https://discord.gg/atjrUen5fJ)
[![DockerHub](https://img.shields.io/badge/Docker-Hub-%23099cec?style=for-the-badge&logo=docker)](https://hub.docker.com/r/optimusperky/requestrr)
[![DockerHub](https://img.shields.io/badge/GitHub-Upstream-lightgrey?style=for-the-badge&logo=github)](https://github.com/thomst08/requestrr/)


Requestrr 
=================

![logo](https://i.imgur.com/0UzLYvw.png)

> ### This fork
> Forked from [thomst08/requestrr](https://github.com/thomst08/requestrr) with the following added:
>
> - **.NET 6 → .NET 10 upgrade** (DSharpPlus 4.5.3 runs unchanged, no library migration needed)
> - **Album-level requesting for the Lidarr/music module** — previously only whole-artist requests were possible. Now you can browse or search an artist's albums and request a specific one, or paste a MusicBrainz release-group link directly when Lidarr's own search can't find something (a real, confirmed gap in Lidarr's search coverage for some compilation series)
> - **Explicit "Various Artists" handling** — typing "Various Artists" or "VA" as the artist name skips search entirely and prompts for an album name, since requesting the whole VA catalog as one artist is nonsensical
> - **Bug fix**: fresh artist requests were never actually being set to `monitored: true` in Lidarr regardless of the `MonitorNewRequests` setting, because `addOptions.monitor` was never sent on artist creation
> - **Bug fix**: Lidarr's own "NOT NULL constraint failed: Albums.Images" error (triggered when adding an album with no cover art — a genuine Lidarr-side bug, reproduced directly against its API) now shows a clear explanation instead of a generic error
>
> **Published to Docker Hub as [`optimusperky/requestrr`](https://hub.docker.com/r/optimusperky/requestrr)** — see [Docker Set-up & Start](#docker-set-up--start) below.

Requestrr is a chatbot used to simplify using services like Sonarr/Radarr/Lidarr/Overseerr/Ombi via the use of chat!  

### Features

- Ability to request content via Discord using slash commands, buttons and more!
- Users can get notified when their requests complete
- Sonarr (V2-V4) & Radarr (V2-V5) integration with support for multiple instance via Overseerr (only for 4k/1080p)
- Lidarr (V1-V2) intergration
- Overseerr integration with support for per user permissions/quotas and issue submission
- Ombi (V3/V4) integration with support for per user roles/quotas and issue submission
- Fully configurable via a web portal

<br />

Installation & Configuration
==================

The web-portal configuration flow (Discord bot token, Sonarr/Radarr/Lidarr connections, categories) is unchanged from upstream — refer to their Wiki for that part:
https://github.com/thomst08/requestrr/wiki

<br />

Docker Set-up & Start
==================

Quickest option — pull the published image directly, same as upstream but pointed at this fork's tag:

```bash
docker run -d \
  --name requestrr \
  -p 4545:4545 \
  -v /path/to/config:/root/config \
  -e TZ=Europe/London \
  --restart=unless-stopped \
  optimusperky/requestrr:latest
```

Then access the web portal at `http://youraddress:4545/` to create your admin account and configure everything. Once the bot is configured and invited to your Discord server, type **/help** to see all available commands.

To update to a newer `latest`, pull and recreate — your existing config volume (bot token, Sonarr/Radarr/Lidarr connections, everything) carries over untouched:

```bash
docker pull optimusperky/requestrr:latest
docker stop requestrr && docker rm requestrr
docker run -d --name requestrr --restart=unless-stopped -p 4545:4545 \
  -v /path/to/config:/root/config -e TZ=Europe/London optimusperky/requestrr:latest
```

### Building from source instead

Only needed if you're making your own changes on top of this fork:

```bash
git clone https://github.com/Optimus-Perky/requestrr.git
cd requestrr/Requestrr.WebApi
docker build -f dockerfile -t requestrr-fork:live .

docker run -d \
  --name requestrr \
  -p 4545:4545 \
  -v /path/to/config:/root/config \
  -e TZ=Europe/London \
  --restart=unless-stopped \
  requestrr-fork:live
```

Rebuild and recreate the same way after pulling new commits to pick up changes.

<br />

Environment Variables
==================

Requestrr supports the following environment variables to help you customize your deployment:

#### `REQUESTRR_PORT`

* **Description**: Sets the port the application listens on **inside** the container.
* **Default**: `4545`
* **Example**: `-e REQUESTRR_PORT=5000`

#### `REQUESTRR_BASEURL`

* **Description**: Defines a base URL path for Requestrr. Useful when deploying behind a reverse proxy with a subpath (e.g. `/requestrr`).
* **Default**: `/`
* **Example**: `-e REQUESTRR_BASEURL=/requestrr`

#### Example Docker Command with Environment Variables

```bash
docker run -d \
  --name requestrr \
  -p 5000:5000 \
  -v /path/to/config:/root/config \
  -e REQUESTRR_PORT=5000 \
  -e REQUESTRR_BASEURL=/requestrr \
  -e TZ=Europe/London \
  --restart=unless-stopped \
  optimusperky/requestrr:latest
```

> ⚠️ **Note**: When setting `REQUESTRR_BASEURL`, make sure it matches your reverse proxy config if you're serving Requestrr under a subpath.

<br />

Thank you list
==============

Thank you goes out to the following people:
- [@darkalfx]( https://github.com/darkalfx ) - Creator of Requestrr, without this person, Requestrr would not exist.
- [@thomst08]( https://github.com/thomst08 ) - Maintainer of the fork this repo builds on.
