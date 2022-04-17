<p align="right">
  <img src="logo.png" alt="DAB-Logo" width=150/>
</p>

# DAB - *Discord Announcement Bot*
Bot that lets users `/set-chime` with a given audio-file, that plays each time they connect to a channel on your server.


## Features
### Slash-commands
```
/set-chime <audio-file> # sets chime of user to given attachment
/clear-chime            # clears chime of user, if present
```
### Behaviour
If some user connects to a channel, the bot will join that channel and play the chime of the user, if configured.
After playing, the bot will leave.

When multiple users connect at the same time, their chimes will be queued and played in *first come, first serve*-order.

## How to get started
Clone this repo and build it for your platform, or download the current release.
Consider [dependencies needed to run](./DEPENDENCIES.md).

On first startup, the bot will exit stating that a default-config has been created. You need to add your Discord-API-Key in `config.json`.

## Configuration
The auto-generated `config.json` in the working directory of the bot contains various parameters that can be adjusted.
Currently, the config consists of:
```json
{
  "DiscordParams": {
    "ApiKey": "foo bar baz"
  },
  "BotParams": {
    "ChimeDurationMaxMs": 10000,
    "ChimeFilesizeMaxKb": 5000,
    "ChimePlaybackTimeoutMs": 10000
  }
}
```
- `"ApiKey":` string containing your unique discord-api-token
- `"ChimeDurationMaxMs":` signed integer containing the maximum duration of a chime, in milliseconds. For no limit, set -1.
- `"ChimeFilesizeMaxKb":` signed integer containing the maximum filesize a user can upload, in kilobytes. For no limit, set -1.
- `"ChimePlaybackTimeoutMs":` signed integer containing the maximum duration after playing a chime will time out, in milliseconds. This is recommended to be set. For no limit, set -1

In general, the default settings will work out most of the time.

## Known issues
- playback stutters on weaker machines
- playback quality
- very short chimes won't play

## Contributing
Kudos to you for scrolling this far! If you want to contribute, see [CONTRIBUTING.md](./CONTRIBUTING.md).
