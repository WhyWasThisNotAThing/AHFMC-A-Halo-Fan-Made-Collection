# How to install

This guide is for players. If you want to build the launcher yourself, see [Building from source](README.md#building-from-source).

## Before you start

- Windows 10 or 11, 64-bit.
- The **Microsoft Edge WebView2 Runtime**. It is already on Windows 11 and on most up-to-date Windows 10 PCs. If the launcher opens with a blank window, install it from Microsoft ("WebView2 Runtime", Evergreen, x64).
- You do **not** need to install .NET.
- The official Halo games are not included. Install the ones you own through Steam or the Microsoft Store first.

## 1. Download

Open the [Releases page](https://github.com/WhyWasThisNotAThing/AHFMC-A-Halo-Fan-Made-Collection/releases) and download one of the files from the newest release:

| File | Choose it if |
| --- | --- |
| `AHFMC-Setup-<version>.exe` | You want a normal install with a Start menu shortcut. **Recommended.** |
| `AHFMC-Portable.exe` | You want a single file you can keep in a folder or on a USB drive. |

## 2. Install

### Installer

1. Run `AHFMC-Setup-<version>.exe`.
2. The installer installs just for you by default, so it does not need administrator rights. On the first page you can choose to install for all users instead (Windows will then ask for permission).
3. Choose the folder, and tick **Create a desktop shortcut** if you want one.
4. Press **Install**, then **Finish**. The launcher starts.

### Portable

1. Put `AHFMC-Portable.exe` in a folder you can write to, such as `Documents\Halo`. Do not put it in `Program Files`, because the launcher replaces its own file when it updates.
2. Double-click it.

### If Windows says "Windows protected your PC"

This is normal for free hobby apps that are not signed with a paid certificate. It is not a virus warning. When the blue window pops up:

1. Click **More info**.
2. Click **Run anyway**.

You only need to do this once. Only do it if you downloaded the file from the project's Releases page.

Some antivirus programs also flag single-file programs like this one by mistake. If yours does, restore the file and add an exclusion for it.

## 3. First start

The welcome screen asks you to:

1. Pick a **layout** (you can change it any time in Settings > Launcher). The Hybrid layouts and Classic Halo CE show a background video and music that you choose yourself in Settings > Launcher.
2. Choose a few settings, such as fullscreen or windowed.
3. Press **Start**.

You can open this screen again from Settings > Launcher > First-time setup.

## 4. Add your games

- **Official Halo games:** if you own a game and it is installed through Steam or the Microsoft Store, its card starts it. If the launcher cannot find it, go to Settings > Official Games and press **Locate...** to pick the game's `.exe`. **Forget** removes your choice again.
- **Community games** (Halo 3 Reclaimer, Halo Reach Renaissance, Halo 2 Gravemind, Open Community Edition, Project Groundside): click the card. If the game is not installed, the launcher downloads the latest version for you. Settings > Games lets you **Locate...** your own copy, or **Forget / Uninstall** it.
- **Your own apps:** go to Settings > My Apps > **Add...**, pick the program, give it a name and, if you like, a picture. Right-click an app on the main screen to rename or remove it.

Play time: the launcher shows how long you have played each game it can see running, official and community, and when you last played it. It stays on your PC.

Tip: if a game's window opens small, turn on **Maximize game window** in Settings > Launcher.

## 5. Chat (optional)

Press **Chat** at the top of the launcher and create an account. You pick a username, a password and an **encryption passphrase**.

- Write the password and passphrase down. **There is no reset and no recovery.** If you lose them, the account and its messages are gone.
- Do not reuse them anywhere else.
- Read the [Terms](TERMS.md) and [Privacy Policy](PRIVACY.md) first. They are also shown in the sign-up window.

## Updating

- **From inside the launcher:** Settings > Updates > **Check for update**. By default the launcher also checks every time it starts, and always asks before downloading anything.
- An installed copy runs the new installer for you. A portable copy replaces its own `.exe`. In both cases the launcher restarts by itself.
- **By hand:** download the newest file from the Releases page and run it. The installer updates the old install in place, and your settings are kept.

## Uninstalling

1. Close the launcher (if it is running in the tray, right-click the tray icon and choose **Exit launcher**).
2. Windows Settings > Apps > Installed apps > **AHFMC: A Halo Fan-Made Collection** > Uninstall. For the portable version, just delete the `.exe`.
3. Your settings, play time and downloaded games are kept on purpose, in case you reinstall. To remove everything, also delete the folder `%AppData%\AHFMC` (paste that into the File Explorer address bar).
4. If you had a chat account, delete it first in Settings > Chat > **Delete my account** to remove your data from the chat service.

## Troubleshooting

| Problem | What to try |
| --- | --- |
| Blank or black window | Install or repair the WebView2 Runtime, then start the launcher again. |
| No background video or music in Classic or Hybrid layout | The launcher does not include them. Pick your own `.mp4` and `.mp3` in Settings > Launcher > **Background video** and **Background music**. Then check Settings > Sound (volumes not at 0%) and make sure the window is not minimized. |
| A game will not start | Settings > Games (or Official Games) > **Locate...** and pick the game's real `.exe`. Make sure the game starts on its own first. |
| A community game download fails | Check your internet connection and any firewall or antivirus, then try again. |
| The launcher cannot see a Steam game | Make sure Steam has finished installing the game (it must show as fully installed), then restart the launcher. |
| Chat sign-up does not show the human check | The check uses WebView2. Repair it, and make sure your network does not block Cloudflare. |
| "This device is linked to an account that is on hold or banned" | The chat blocks other accounts on a PC linked to a held or banned account. Use the appeal chat on the held account. |
| Forgot the chat password or passphrase | It cannot be recovered. Create a new account. |
| Settings are messed up | Close the launcher, rename `%AppData%\AHFMC\config.json` to `config.json.old`, and start it again. You will see the welcome screen and lose your added apps and play time. |

Still stuck? [Open an issue](https://github.com/WhyWasThisNotAThing/AHFMC-A-Halo-Fan-Made-Collection/issues) and say your Windows version, the launcher version (shown in the bottom right corner) and what happened. Never post passwords, passphrases or private messages.

You can also email the maker at whywasthisnotathing@gmail.com (or use Settings > Credits and contact > Contact me). Do not send passwords, passphrases or private messages.
