**NOTE:** I moved to Rust while developing this bot. See https://github.com/Sir-Photch/dab-rs


<img width="64" height="64" align="left" style="float: left;" alt="DAB-icon" src="logo.png"/>

# DAB - *Discord Announcement Bot*
Bot that lets users `/chime` with a given audio-file or url, that plays each time they connect to a channel on your server.


## Features
### Slash-commands
```
/chime set url    # sets chime of user to given url (that links to a audio-file)
/chime set file   # sets chime of user to given attachment
/chime clear      # clears chime of user, if present
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
    "ApiKey": "foo bar baz",
    "AudioBufferMs": 25
  },
  "BotParams": {
    "ChimeDurationMaxMs": 10000,
    "ChimeFilesizeMaxKb": 5000,
    "ChimePlaybackTimeoutMs": 10000
  }
}
```
- `"ApiKey":` string containing your unique discord-api-token
- `"AudioBufferMs":` unsigned integer containing the buffer-duration of discord-audio-stream. Must be greater than 1. For bigger values, short chimes may not be played.
- `"ChimeDurationMaxMs":` signed integer containing the maximum duration of a chime, in milliseconds. For no limit, set -1.
- `"ChimeFilesizeMaxKb":` signed integer containing the maximum filesize a user can upload, in kilobytes. For no limit, set -1.
- `"ChimePlaybackTimeoutMs":` signed integer containing the maximum duration after playing a chime will time out, in milliseconds. This is recommended to be set. For no limit, set -1

In general, the default settings will work out most of the time.

## Known issues
- playback stutters on weaker machines
- Chimes are queued when bot is in other channel than joinee, but not played until some other user joins into the same channel

## Contributing
Kudos to you for scrolling this far! If you want to contribute, see [CONTRIBUTING.md](./CONTRIBUTING.md).
