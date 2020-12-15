# VideoSplitter

[![GitHub issues](https://img.shields.io/github/issues/Gokujo/VideoSplitter)](https://github.com/Gokujo/VideoSplitter/issues) [![GitHub forks](https://img.shields.io/github/forks/Gokujo/VideoSplitter)](https://github.com/Gokujo/VideoSplitter/network) [![GitHub stars](https://img.shields.io/github/stars/Gokujo/VideoSplitter)](https://github.com/Gokujo/VideoSplitter/stargazers)

Simple video splitter for WhatsApp, Telegram and etc. You just need to set up the max. file size and this app will split video(s) fast and automatically.

It's a console app, so you have to type all params in a console. Don't worry those are just a few.

# Requirements

- [FFMpeg](https://www.gyan.dev/ffmpeg/builds/) for Windows
- [Microsoft .NET Framework 4.6.1](https://www.microsoft.com/de-de/download/details.aspx?id=49982)

# Possible settings

- Path to FFMpeg bin dir (folder)
- Path to video source dir (folder)
- Path to video output dir (folder)
- Path to temp files dir (folder) - for now it's no use
- Max. filesize in MB
- Search for especially video extension (mp4, avi, flv, wmv, mkv - only supported)

# How It Works

You have to define max. filesize and the script will split video source into parts. On gathering of media info it checks if the video have to be splitted or not. If the filesize is bigger that estimated and predefined filesize the script starts to checking in how much part it should be splitted. In this case - original size / estimated size.
FFMpeg saves video "cut" to output dir with automatically generated part num with equal video length.


# Screenshot
![Screenshot 1](./ScreenShots/1.png) ![Screenshot 2](./ScreenShots/2.png) ![Screenshot 3](./ScreenShots/3.png) ![Screenshot 4](./ScreenShots/4.png)

# Have Fun
