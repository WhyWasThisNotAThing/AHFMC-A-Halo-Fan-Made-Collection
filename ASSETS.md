# Assets

Everything needed to build and run AHFMC is included. The launcher builds and runs without any Halo files. The Classic layout looks best with the optional files below and still works without them.

## Included (original, made for this project)

| Path | Used for |
| --- | --- |
| `Assets\logo.png`, `Assets\logo.ico` | App icon and logo. Replace them with your own artwork if you like. |
| `Assets\countdown-beep.wav`, `Assets\countdown-go.wav` | Launch countdown sounds |
| `ClassicHalo\assets\ui\sfx-move.wav`, `sfx-select.wav`, `sfx-back.wav`, `sfx-fail.wav` | Menu sounds in every layout: Normal, Classic and Big Picture (original, generated for this project). Players can replace each one in Settings > Sound > Menu sound. |

## Optional, not included (you add them yourself)

The Classic, Hybrid and Big Picture layouts use these. Everything still starts without them.

| Put the file here | Used by | Without it |
| --- | --- | --- |
| `Assets\HaloGameLogo\<game>.png` (name each file after the game, for example `mcc.png`) | Game logos in the Classic preview panel and the Big Picture layouts | No logo shown |
| `ClassicHalo\assets\ui\sounds.mp3` | Menu click and move sounds in Classic and Big Picture | Generated beeps instead |
| `ClassicHalo\assets\ui\campaign.jpg`, `maps.jpg` | Big Picture artwork | Blank artwork |

## Your own background video and music

The launcher does not include a background video or music, and the build skips `bg-1080.mp4` and `bg.mp3` even if they are in the folder. You bring your own:

1. Open Settings > Launcher (the same options are in the Classic Halo CE and Big Picture settings). If you pick Classic Halo CE, Hybrid or Big Picture Hybrid without a video, the launcher tells you that you need to bring your own.
2. Press **Choose...** on **Background video** and pick an `.mp4`.
3. Optionally press **Choose...** on **Background music** and pick an `.mp3`. If your `.mp4` has its own sound, the launcher uses that and no music file is needed.

The files are copied to `%AppData%\AHFMC\ClassicHalo\assets\ui` on your PC and are never uploaded. **Remove** deletes the copy. Without a video, the Hybrid layouts show a plain background. Only pick files you made or have the right to use.
