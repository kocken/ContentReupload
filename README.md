# ContentReupload
An application that automatically creates daily, monthly & yearly highlight gaming compilation videos and uploads them to YouTube. 

The app is multi-threaded and can manage multiple YouTube channels at once, using one thread per channel. 

The program uses SQL Server Express for DB storage, which is accessed by the app with Entity Framework.

This app was created in form as a hobby project. It was created to run and automatically upload videos for the following YouTube channels:
[Twitch Gold](https://www.youtube.com/channel/UC4Ucibr5L2O9HkaygjIvyrA)
[Daily CSGO](https://www.youtube.com/channel/UCCdPO27psRquTjAY2EHiz6w)
[Daily Dota](https://www.youtube.com/channel/UC96mL-T6mAD2xR8tZi7iNug)

## Simplified process
1. [RedditLibrary] Gathers [twitch.tv](https://www.twitch.tv) gaming clip submissions from the channel's configured [subreddit](https://www.reddit.com).
2. [TwitchLibrary] Downloads the twitch clips using the [clipr.xyz](https://clipr.xyz) website, using Selenium and WebClient doing so, as there's no official API to download Twitch clips.
3. [VideoLibrary] Uses the locally downloaded clips and compiles them to a video using [FFmpeg](https://ffmpeg.org) with the Windows Command Prompt.
4. [YouTubeLibrary] Uploads the video compilation to YouTube using different Google nugets.

In order for the app to properly function and upload the compiled videos to YouTube, the user needs to set up their Google account to allow API access to YouTube. Read the [Google documentation](https://developers.google.com/api-client-library/dotnet/guide/aaa_overview) for instructions. Edit client_secrets.json with the id & secret.
