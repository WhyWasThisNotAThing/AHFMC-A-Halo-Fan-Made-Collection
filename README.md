# AHFMC: A Halo Fan-Made Collection (all docs)

This README holds all the project documents in one place. The separate files (INSTALL.md, SECURITY.md and so on) are still in the project because GitHub and other files link to them by name.

## Contents

- [About](#readme) (`README.md`)
- [How to install](#doc-install) (`INSTALL.md`)
- [Building from source](#doc-building) (`BUILDING.md`)
- [Assets](#doc-assets) (`ASSETS.md`)
- [How to contribute](#doc-contributing) (`CONTRIBUTING.md`)
- [Code of Conduct](#doc-code-of-conduct) (`CODE_OF_CONDUCT.md`)
- [Security Policy](#doc-security) (`SECURITY.md`)
- [Privacy Policy](#doc-privacy) (`PRIVACY.md`)
- [Terms of Service](#doc-terms) (`TERMS.md`)
- [Third-party notices](#doc-third-party-notices) (`THIRD-PARTY-NOTICES.md`)


---

<a id="readme"></a>

## AHFMC: A Halo Fan-Made Collection

A free, fan-made launcher that puts your Halo games, the community Halo projects and your own apps in one place, with a built-in encrypted chat for finding people to play with.

> **Not affiliated with Microsoft, Xbox Game Studios, Halo Studios or 343 Industries.** Halo and all related names, logos and images belong to their owners. This launcher does not contain any Halo game. It starts games you already own, and downloads community projects from their makers.

### What it does

**Games**
- Starts the official Halo games you own (Halo: The Master Chief Collection, Halo 5: Forge, Halo Infinite, Halo: Campaign Evolved (2026), Halo Wars: Definitive Edition, Halo Wars 2, Halo: Spartan Assault and Halo: Spartan Strike). You can also point it at a game's `.exe` yourself.
- Downloads, updates and starts community projects: Reclaimer, Halo Reach Renaissance, Halo 2 Gravemind, Open Community Edition, Project Groundside and Project Raven.
- **My Apps**: add any program or game, give it your own name and a picture, and start it from the launcher.
- Optional 3, 2, 1, GO launch countdown with Halo sounds (or your own sounds), and an option to maximize the game window.
- Shows how long you have played each game, official or community, and when you last played it (this stays on your PC). It counts the time the launcher sees the game running.

**Look and feel**
- Five layouts, chosen in the welcome screen or in Settings > Launcher: Normal, Classic Halo CE, Hybrid, Big Picture and Big Picture Hybrid. The Hybrid layouts show a background video and music that you bring yourself.
- Works with a mouse, the keyboard arrow keys and a controller.
- Fullscreen or a normal resizable window. Everything scales with the window size.

**News**
- Release notes for the launcher and the community projects, with a button to open each one on GitHub. Each source can be turned off.

**Chat**
- Built into the launcher. You need an account to use it; the launcher itself works without one.
- End-to-end encrypted direct messages and parties, a public lobby, and a party finder.
- Windows notifications and an optional popup over games, an auto-lock, and a fingerprint check so you can verify your contacts.
- Report buttons and a moderation system. See the [Code of Conduct](#doc-code-of-conduct).

**Extras**
- Discord Rich Presence (optional), keep running in the system tray (optional), and in-app updates for the launcher.

### Install

Short version: download the installer (or the portable `.exe`) from the [Releases page](https://github.com/WhyWasThisNotAThing/AHFMC-A-Halo-Fan-Made-Collection/releases), run it, and follow the welcome screen.

Full steps, updating, uninstalling and fixes for common problems are in **[INSTALL.md](#doc-install)**.

**Requirements:** Windows 10 or 11, 64-bit, with the Microsoft Edge WebView2 Runtime (already on Windows 11 and most Windows 10 PCs). You do not need to install .NET. An internet connection is needed for downloads, news and chat.

### Your data and privacy

There is no tracking and there are no ads. Your settings, play time and downloaded games stay on your PC in `%AppData%\AHFMC`. Read the [Privacy Policy](#doc-privacy) and the [Terms of Service](#doc-terms) (the same text is shown in the app when you create a chat account).

### Building from source

You need Windows, the .NET 10 SDK and, for the installer, [Inno Setup 6](https://jrsoftware.org/isdl.php). To build and run, use `dotnet build` and `dotnet run`, or run `Builder_and_Installer\Build.bat`.

Everything about building, making a release and running your own chat server is in **[BUILDING.md](#doc-building)**.

### Contributing and support

- Want to add something (a setting, a game, a news source)? Read **[CONTRIBUTING.md](#doc-contributing)**.
- Found a bug or have an idea? [Open an issue](https://github.com/WhyWasThisNotAThing/AHFMC-A-Halo-Fan-Made-Collection/issues).
- Please follow the [Code of Conduct](#doc-code-of-conduct) everywhere in the project, including the in-app chat.
- Do not post passwords, passphrases or private messages in an issue.

### Credits

Made by [WhyWasThisNotAThing](https://github.com/WhyWasThisNotAThing). The community games belong to their makers: [Reclaimer](https://github.com/ProjectReclaimer/project-reclaimer-releases), [Halo Reach Renaissance](https://github.com/NETF1ix/reach-renaissance-releases), [Halo 2 Gravemind](https://github.com/BriRoz1/h2gravemind-releases/releases/), [Open Community Edition](https://opencommunityedition.com/) and Project Groundside ([join its Discord](https://discord.gg/nRusWYMdYu)). The full list is in the launcher under **Credits**, and in Settings > Credits and contact.

Contact: whywasthisnotathing@gmail.com

### Halo notice

**Halo: Combat Evolved** © Microsoft Corporation. AHFMC: A Halo Fan-Made Collection was created under Microsoft's "Game Content Usage Rules" using assets from Halo: Combat Evolved, and it is not endorsed by or affiliated with Microsoft. <https://www.xbox.com/en-us/developers/rules>

**Halo: Reach** © Microsoft Corporation. AHFMC: A Halo Fan-Made Collection was created under Microsoft's "Game Content Usage Rules" using assets from Halo: Reach, and it is not endorsed by or affiliated with Microsoft. <https://www.xbox.com/en-us/developers/rules>

### License

The source code of this project is licensed under the [GNU General Public License v3.0](LICENSE). Copyright (C) 2026 WhyWasThisNotAThing.

The license covers only the code and files written for this project. It does not cover Halo or anything that belongs to Microsoft, Xbox Game Studios or Halo Studios (names, logos, sounds, fonts, images, music and video). Those files are not in this repository: see [ASSETS.md](#doc-assets). The community games the launcher downloads have their own licenses.

Third-party software the launcher uses is listed, with its licenses, in [THIRD-PARTY-NOTICES.md](#doc-third-party-notices).

### Security

Please report security problems privately. See [SECURITY.md](#doc-security).

---

<a id="doc-install"></a>

## How to install

This guide is for players. If you want to build the launcher yourself, see [Building from source](#doc-building).

### Before you start

- Windows 10 or 11, 64-bit.
- The **Microsoft Edge WebView2 Runtime**. It is already on Windows 11 and on most up-to-date Windows 10 PCs. If the launcher opens with a blank window, install it from Microsoft ("WebView2 Runtime", Evergreen, x64).
- You do **not** need to install .NET.
- The official Halo games are not included. Install the ones you own through Steam or the Microsoft Store first.

### 1. Download

Open the [Releases page](https://github.com/WhyWasThisNotAThing/AHFMC-A-Halo-Fan-Made-Collection/releases) and download one of the files from the newest release:

| File | Choose it if |
| --- | --- |
| `AHFMC-Setup-<version>.exe` | You want a normal install with a Start menu shortcut. **Recommended.** |
| `AHFMC-Portable.exe` | You want a single file you can keep in a folder or on a USB drive. |

### 2. Install

#### Installer

1. Run `AHFMC-Setup-<version>.exe`.
2. The installer installs just for you by default, so it does not need administrator rights. On the first page you can choose to install for all users instead (Windows will then ask for permission).
3. Choose the folder, and tick **Create a desktop shortcut** if you want one.
4. Press **Install**, then **Finish**. The launcher starts.

#### Portable

1. Put `AHFMC-Portable.exe` in a folder you can write to, such as `Documents\Halo`. Do not put it in `Program Files`, because the launcher replaces its own file when it updates.
2. Double-click it.

#### If Windows says "Windows protected your PC"

This is normal for free hobby apps that are not signed with a paid certificate. It is not a virus warning. When the blue window pops up:

1. Click **More info**.
2. Click **Run anyway**.

You only need to do this once. Only do it if you downloaded the file from the project's Releases page.

Some antivirus programs also flag single-file programs like this one by mistake. If yours does, restore the file and add an exclusion for it.

### 3. First start

The welcome screen asks you to:

1. Pick a **layout** (you can change it any time in Settings > Launcher). The Hybrid layouts and Classic Halo CE show a background video and music that you choose yourself in Settings > Launcher.
2. Choose a few settings, such as fullscreen or windowed.
3. Press **Start**.

You can open this screen again from Settings > Launcher > First-time setup.

### 4. Add your games

- **Official Halo games:** if you own a game and it is installed through Steam or the Microsoft Store, its card starts it. If the launcher cannot find it, go to Settings > Official Games and press **Locate...** to pick the game's `.exe`. **Forget** removes your choice again.
- **Community games** (Reclaimer, Halo Reach Renaissance, Halo 2 Gravemind, Open Community Edition, Project Groundside, Project Raven): click the card. If the game is not installed, the launcher downloads the latest version for you. Settings > Games lets you **Locate...** your own copy, or **Forget / Uninstall** it.
- **Your own apps:** go to Settings > My Apps > **Add...**, pick the program, give it a name and, if you like, a picture. Right-click an app on the main screen to rename or remove it.

Play time: the launcher shows how long you have played each game it can see running, official and community, and when you last played it. It stays on your PC.

Tip: if a game's window opens small, turn on **Maximize game window** in Settings > Launcher.

### 5. Chat (optional)

Press **Chat** at the top of the launcher and create an account. You pick a username, a password and an **encryption passphrase**.

- Write the password and passphrase down. **There is no reset and no recovery.** If you lose them, the account and its messages are gone.
- Do not reuse them anywhere else.
- Read the [Terms](#doc-terms) and [Privacy Policy](#doc-privacy) first. They are also shown in the sign-up window.

### Updating

- **From inside the launcher:** Settings > Updates > **Check for update**. By default the launcher also checks every time it starts, and always asks before downloading anything.
- An installed copy runs the new installer for you. A portable copy replaces its own `.exe`. In both cases the launcher restarts by itself.
- **By hand:** download the newest file from the Releases page and run it. The installer updates the old install in place, and your settings are kept.

### Uninstalling

1. Close the launcher (if it is running in the tray, right-click the tray icon and choose **Exit launcher**).
2. Windows Settings > Apps > Installed apps > **AHFMC: A Halo Fan-Made Collection** > Uninstall. For the portable version, just delete the `.exe`.
3. Your settings, play time and downloaded games are kept on purpose, in case you reinstall. To remove everything, also delete the folder `%AppData%\AHFMC` (paste that into the File Explorer address bar).
4. If you had a chat account, delete it first in Settings > Chat > **Delete my account** to remove your data from the chat service.

### Troubleshooting

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

---

<a id="doc-building"></a>

## Building from source

This guide is for people who want to build AHFMC themselves: to try a change, to make their own copy, or to publish a release. If you only want to use the launcher, see [INSTALL.md](#doc-install). If you want to change the code, read [CONTRIBUTING.md](#doc-contributing) as well.

### What you need

- Windows 10 or 11, 64-bit.
- The **.NET 10 SDK**. The launcher is a WPF app, so it only builds on Windows.
- **Inno Setup 6**, only if you want to build the installer: <https://jrsoftware.org/isdl.php>.
- Visual Studio 2026 (with the ".NET desktop development" workload) or VS Code with the C# extension. Both are optional; the command line is enough.
- The **Microsoft Edge WebView2 Runtime** to run the result. It is already on Windows 11 and most Windows 10 PCs.

You do not need any Halo files. The launcher builds and runs without them. The optional extras are listed in [ASSETS.md](#doc-assets).

### Get the code

```bat
git clone https://github.com/WhyWasThisNotAThing/AHFMC-A-Halo-Fan-Made-Collection.git
cd AHFMC-A-Halo-Fan-Made-Collection
```

Check that an `Assets` folder is next to `AHFMC.csproj` and holds `logo.png`, `logo.ico`, `countdown-beep.wav` and `countdown-go.wav`. The build embeds them and fails without them. The `Assets\HaloGameLogo` folder is optional.

### Quick build and run

For everyday work, from the project folder:

```bat
dotnet build
dotnet run
```

Close any running copy of the launcher first, or Windows will lock the `.exe` and the build will fail.

### Build the single `.exe`

```bat
Builder_and_Installer\Build.bat
```

This stops any running copy, publishes a Release build and starts it. The result is one self-contained file, so the person using it does not need to install .NET:

```
bin\Release\net10.0-windows\win-x64\publish\AHFMC.exe
```

The scripts change to the project folder themselves, so you can run them from anywhere.

### Build a release (portable file and installer)

```bat
Builder_and_Installer\build-release.bat
```

It does the following:

1. Reads the version from `<Version>` in `AHFMC.csproj`.
2. Publishes the Release build, the same as `Build.bat`.
3. Copies the result to `release\AHFMC-Portable.exe`.
4. Looks for Inno Setup 6 (in `Program Files (x86)`, `Program Files` and `%LocalAppData%\Programs`) and builds `release\AHFMC-Setup-<version>.exe` from `Builder_and_Installer\AHFMC.iss`.

If Inno Setup is not found, only the portable file is made. Install Inno Setup and run the script again.

#### About the installer

- It installs just for the current user by default, so it needs no administrator rights. The first page lets the person choose all users instead.
- The desktop shortcut is unticked by default.
- It closes a running copy of the launcher before it installs.
- It accepts a `/RELAUNCH=1` option that starts the launcher again afterwards. The in-app updater uses it for installed copies.

### Publish an update

1. Change `<Version>` in `AHFMC.csproj` (for example `1.0.1`).
2. Run `Builder_and_Installer\build-release.bat`.
3. Create a GitHub release tagged `v<version>` (for example `v1.0.1`).
4. Upload both files from the `release` folder to it.

The in-app updater looks for release files whose names **start with** `AHFMC-Setup` (installed copies) or `AHFMC-Portable` (portable copies). Keep those names or updates will stop working.

Before you publish:

- [ ] Start the launcher with a fresh `config.json` and check that it works.
- [ ] Check that the `LICENSE` text and copyright lines in [THIRD-PARTY-NOTICES.md](#doc-third-party-notices) still match the packages you ship.
- [ ] If the chat collects or shows anything new, update `GameChat/LegalText.cs`, [PRIVACY.md](#doc-privacy) and [TERMS.md](#doc-terms) together, with the same "Last updated" date.
- [ ] Make sure no Halo files, background videos or music are included in the release (see [ASSETS.md](#doc-assets)).
- [ ] Make sure no passwords, keys or tokens are in the code. The Supabase "anon" key in `GameChat/Config.cs` is public by design; never add a service or secret key.

### Project layout

| Path | What is in it |
| --- | --- |
| `MainWindow.*` | The launcher window: games, settings, news, updater, setup and chat panel |
| `GameChat/` | Chat: sign-in, encryption, messages, parties, reports and the legal text |
| `GameChat/sql files/` | The chat server scripts for Supabase |
| `ClassicHalo/` | The Classic Halo CE menu and the Big Picture screen (web pages shown inside the launcher) |
| `Assets/` | Logo, countdown sounds and the optional `HaloGameLogo/` card pictures |
| `Builder_and_Installer/` | Build scripts and the Inno Setup installer script |

Everything in `ClassicHalo\` is packed **inside** the `.exe` and copied to `%AppData%\AHFMC\ClassicHalo` when the launcher starts. After you change a web file, rebuild and restart the launcher or you will keep seeing the old version. The build skips `bg-720.mp4`, `bg-1080.mp4` and `bg.mp3` even if they are in the folder.

### Running your own chat server

The chat uses a [Supabase](https://supabase.com) project. To use your own:

1. Create a Supabase project, open **SQL Editor** and run `GameChat/sql files/chat-all.sql`. It can be run more than once.
2. Run `GameChat/sql files/cleanup.sql` as well. It limits what the sign-in service keeps (such as IP addresses) and needs the `pg_cron` extension. Your database role may not be allowed to touch the `auth` schema; if so, that part cannot be done from the SQL editor.
3. Put your project's URL, public (anon) key and a Cloudflare Turnstile site key into `GameChat/Config.cs`.
4. To get the moderator tools, add your user id to the `public.admins` table.

If you run your own server, you are the one who runs the chat. Update the contact details in `GameChat/LegalText.cs`, [PRIVACY.md](#doc-privacy) and [TERMS.md](#doc-terms) to match, and say who the moderators are.

### Troubleshooting

| Problem | What to try |
| --- | --- |
| The build says a file is in use, or `AHFMC.exe` cannot be written | Close the launcher (check the system tray too), then build again. |
| The build cannot find `Assets\logo.png`, `logo.ico` or a `.wav` file | Add the missing files to the `Assets` folder (see above). |
| `build-release.bat` makes only the portable file | Inno Setup 6 was not found. Install it and run the script again. |
| `.NET SDK` or target framework errors | Install the .NET 10 SDK (`dotnet --version` should start with 10). |
| You changed a web file but see the old page | Rebuild and restart. The files are packed inside the `.exe`. |
| The launcher you built opens with a blank window | Install or repair the WebView2 Runtime. |
| Windows says "Windows protected your PC" when you run your build | The file is not signed with a paid certificate. This is normal for hobby builds. See [INSTALL.md](#doc-install). |

### License

The source code is under the [GNU General Public License v3.0](LICENSE). If you share a build, you must also share the source of your changes under the same license. The license does not cover Halo or anything that belongs to Microsoft, Xbox Game Studios or Halo Studios.

---

<a id="doc-assets"></a>

## Assets

Everything needed to build and run AHFMC is included. The launcher builds and runs without any Halo files. The Classic layout looks best with the optional files below and still works without them.

### Included (original, made for this project)

| Path | Used for |
| --- | --- |
| `Assets\logo.png`, `Assets\logo.ico` | App icon and logo. Replace them with your own artwork if you like. |
| `Assets\countdown-beep.wav`, `Assets\countdown-go.wav` | Launch countdown sounds |
| `ClassicHalo\assets\ui\sfx-move.wav`, `sfx-select.wav`, `sfx-back.wav`, `sfx-fail.wav` | Menu sounds in every layout: Normal, Classic and Big Picture (original, generated for this project). Players can replace each one in Settings > Sound > Menu sound. |

### Optional, not included (you add them yourself)

The Classic, Hybrid and Big Picture layouts use these. Everything still starts without them.

| Put the file here | Used by | Without it |
| --- | --- | --- |
| `Assets\HaloGameLogo\<game>.png` (name each file after the game, for example `mcc.png`) | Game logos in the Classic preview panel and the Big Picture layouts | No logo shown |
| `ClassicHalo\assets\ui\sounds.mp3` | Menu click and move sounds in Classic and Big Picture | Generated beeps instead |
| `ClassicHalo\assets\ui\campaign.jpg`, `maps.jpg` | Big Picture artwork | Blank artwork |

### Your own background video and music

The launcher does not include a background video or music, and the build skips `bg-1080.mp4` and `bg.mp3` even if they are in the folder. You bring your own:

1. Open Settings > Launcher (the same options are in the Classic Halo CE and Big Picture settings). If you pick Classic Halo CE, Hybrid or Big Picture Hybrid without a video, the launcher tells you that you need to bring your own.
2. Press **Choose...** on **Background video** and pick an `.mp4`.
3. Optionally press **Choose...** on **Background music** and pick an `.mp3`. If your `.mp4` has its own sound, the launcher uses that and no music file is needed.

The files are copied to `%AppData%\AHFMC\ClassicHalo\assets\ui` on your PC and are never uploaded. **Remove** deletes the copy. Without a video, the Hybrid layouts show a plain background. Only pick files you made or have the right to use.

---

<a id="doc-contributing"></a>

## How to contribute

Thanks for wanting to help. This guide shows how to set the project up, how it fits together, and step by step how to add the usual things: a setting, a Halo game, a community game, a news source or a chat feature.

Please read the [Code of Conduct](#doc-code-of-conduct) first. It applies to issues, pull requests and the in-app chat.

### Ways to help

- **Report a bug.** [Open an issue](https://github.com/WhyWasThisNotAThing/AHFMC-A-Halo-Fan-Made-Collection/issues) with your Windows version, the launcher version (bottom right corner), what you did and what happened. Never post passwords, passphrases or private messages.
- **Suggest an idea.** Open an issue first and say what problem it solves. For anything big, wait for a reply before you start coding, so your work is not wasted.
- **Improve the docs or the wording** in the app and in these files.
- **Send code.** Small, focused pull requests are easiest to review.

### Set up

You need:

- Windows 10 or 11, 64-bit
- The .NET 10 SDK
- Visual Studio 2026 (with the ".NET desktop development" workload) or VS Code with the C# extension
- Nothing else. The project builds without any Halo files. The optional extras are listed in [ASSETS.md](#doc-assets)

Build and run:

```bat
dotnet build
dotnet run
```

`Builder_and_Installer\Build.bat` publishes the single `.exe` and starts it. `Builder_and_Installer\build-release.bat` builds the portable file and the installer, and is for maintainers (see [Building from source](#doc-building)).

Useful to know while testing:

- The launcher keeps its data in `%AppData%\AHFMC`. To see the welcome screen again, close the launcher and rename `config.json` in that folder.
- The launcher does not ship a background video or music. To test the Hybrid layouts, pick your own `.mp4` and `.mp3` in Settings > Launcher > Background video / Background music. They are copied to `%AppData%\AHFMC\ClassicHalo\assets\ui`.
- Everything in `ClassicHalo\` is packed **inside** the `.exe` and copied to `%AppData%\AHFMC\ClassicHalo` when the launcher starts. After you change a web file, **rebuild and restart** the launcher or you will keep seeing the old version.
- Close any running copy before you build, or Windows will lock the `.exe`.

### How the project fits together

There are three screens, and they share one C# "brain":

| Screen | Made with | Files |
| --- | --- | --- |
| Normal and Hybrid | WPF | `MainWindow.xaml`, `MainWindow.xaml.cs`, `MainWindow.Extras.cs` |
| Classic Halo CE | A web page inside WebView2 | `ClassicHalo/index.html` (the page and its script are in one file) |
| Big Picture (and Big Picture Hybrid) | A web page inside WebView2 | `ClassicHalo/bigpicture/` |

The two web pages cannot do anything on their own. They send messages to C# and C# sends them the current state:

- **Page to C#:** the page calls `post({ type: "open", game: "mcc", option: 0 })`. The `Classic_WebMessage` method in `MainWindow.xaml.cs` has one `case` for every `type`. Settings use two generic messages, `setting` (row number and direction) and `settingPick` (row number and chosen item).
- **C# to page:** `PushClassicState()` sends one object with everything the pages need to draw themselves, including the whole settings list (`S.settings`). The pages read it as `S` (both Classic and Big Picture).

**The golden rule: a visible change usually has to be made in all three screens.** Games live in several places, and the recipes below list every one. If you only change one, the other layouts will be out of date. Settings are the exception: they are written once, in `BuildSettings()`.

Other files:

| Path | What is in it |
| --- | --- |
| `MainWindow.Setup.cs` | The welcome (first-time setup) screen |
| `MainWindow.Updater.cs` | Launcher self-update from GitHub releases |
| `MainWindow.Chat.cs` | Chat panel, notifications and tray |
| `GameChat/` | Chat: sign-in, encryption, messages, parties, reports, `LegalText.cs` |
| `GameChat/sql files/` | The chat server scripts (Supabase) |
| `Assets/` | Logo, countdown sounds, `HaloGameLogo/` card pictures |

### Recipes

> **Tip for everything below:** the easiest way to add something is to copy something that already exists. Search the whole project for the name of a similar item (for example `NewsReach`, `infinite` or `Gravemind`) and add the matching line next to every hit.

#### Add a setting

Example: an on/off setting called "Play start sound".

1. **Save it.** Add a property with a default to the `Config` class in `MainWindow.xaml.cs`:
   ```csharp
   public bool StartSound { get; set; } = true;
   ```
2. **Add the row.** In `BuildSettings()` in `MainWindow.Extras.cs`, add a row under the right `Header(...)`:
   ```csharp
   Add("Start sound",
       "Play a sound when the launcher starts.",
       () => _config.StartSound ? "Enabled" : "Disabled",
       d => { _config.StartSound = !_config.StartSound; SaveConfig(); PushClassicState(); });
   ```
   This one list is the only place settings are written. The Normal, Classic Halo CE and Big Picture menus all draw themselves from it, so they always show the same categories, the same rows and the same wording, and the keyboard, arrow keys and controller work the same in all three.

   The shape of the row is picked from the value text: `"Enabled"`/`"Disabled"` (or `"Show text"`/`"Hide text"`) makes a checkbox, a list of names makes a drop-down or arrows, and `btn: "Open..."` or `btns: new[] { "Locate...", "Forget" }` makes buttons. For two buttons, the first runs when `d > 0` and the second when `d < 0`.
3. **Welcome screen (optional).** If new players should see it, add a `SetupToggle(...)` line in `MainWindow.Setup.cs`.
4. Use the setting where it matters, for example in `App.xaml.cs` or the window code.
5. Only if a web page needs the value for something other than the settings menu (for example to change how it draws), add a field to the object in `PushClassicState()` and read it from `S`. Big Picture's blurry background does this (`blur`).

Chat settings are different. They are stored per account in `GameChat/LocalStore.cs` (`LocalSettings`), changed with `ChatSettingChanged()` and sent from the web pages with `setChat`.

#### Add an official Halo game

This is for a game people own on Steam or the Microsoft Store. The launcher only starts it.

1. `MainWindow.Extras.cs`: add `("id", "Display name")` to `OfficialList`. This gives it a row in Settings > Official Games.
2. `MainWindow.xaml.cs`: add the game to the `OfficialGames` dictionary with its ways to start. Steam games use `steam://run/<app id>`. Store games use an `ms-windows-store://` link.
   ```csharp
   ["newgame"] = new GameOpt[] { new("Steam", "steam://run/123456", "Halo: New Game"), new("Microsoft Store", "ms-windows-store://search/?query=Halo%20New%20Game", "Halo: New Game") },
   ```
3. Play time is saved under the game's display name, so use exactly the same name in `OfficialList`, in the `OfficialGames` entry and in the Normal layout `Tag`. In `MainWindow.xaml` also add a `PlayedXxx` text block under the title and set it in `UpdateOfficialPlayed()` (`MainWindow.Extras.cs`).
4. `MainWindow.xaml.cs`: add the card picture name to `LogoNames`, then put a matching PNG in `Assets\HaloGameLogo`.
5. **Normal layout:** copy an existing game block (such as Halo Infinite) in `MainWindow.xaml` and change the title, the app id and the `Tag` (`link|Game name`).
6. **Classic menu:** add the game and its options to the `GAMES` list at the top of the script in `ClassicHalo/index.html`.
7. **Big Picture:** add it to `OFFICIAL` at the top of `ClassicHalo/bigpicture/bigpicture.js`.

#### Add a community (fan) game

A community game is one the launcher downloads from GitHub or from its maker's site, keeps up to date, and can start. This is the biggest change, because the launcher has to install, update, locate, forget and uninstall it.

The reliable method: **pick the most similar existing game (Halo 2 Gravemind downloads from GitHub, Project Groundside from a direct link), search the project for its name, and mirror every hit.** Searching for `Gravemind` finds about 80 places in 7 files. They fall into these groups:

- **Saved data:** `Config` in `MainWindow.xaml.cs` needs `...Exe`, `...Version` and `...Manual` properties.
- **Where it comes from:** the repo and file name constants (`GravemindRepo`, `GravemindPrefix`, `GravemindExt`).
- **Install and update:** an `Install...Async`, a `Prepare...Async` (find the latest, ask before updating) and a `Play...Async`.
- **Locate, forget and uninstall:** `Locate...`, `Forget...`, plus the cases in `ForgetOrUninstall`, `IsRemovable` and the downloaded-games table (id to folder name).
- **Card and text:** a button in `MainWindow.xaml`, a click handler, an `Update...Text()` method, and the call that refreshes it.
- **Logo:** `LogoNames` and a PNG in `Assets\HaloGameLogo`.
- **Settings:** a "... location" row in `BuildSettings()`. The Classic and Big Picture menus read it from there.
- **Classic menu:** the `COMM` list in `ClassicHalo/index.html`, and the case for it in the `"fan"` branch of `Classic_WebMessage`.
- **Big Picture:** `BASE_FAN` in `bigpicture.js`.
- **State:** the game's text, `installed` and `removable` flags in `PushClassicState()`.

Run the game once with a fresh `config.json`, then test: install, play, update, locate your own copy, forget it, and uninstall. Credit the makers in the Credits pane (`MainWindow.xaml`) and in the README, and check that the project allows redistribution or linking to its downloads.

#### Add a news source

News shows the newest GitHub release of each project.

1. Add `(id, "Display name", repo)` to the `sources` array in `FetchNewsAsync()` (`MainWindow.xaml.cs`).
2. Add a `NewsXxx` property to `Config` and a case in `NewsEnabled(...)`.
3. Add the setting in `BuildSettings()` (Settings > News). All three menus show it.
4. Add a `SetupToggle(...)` in `MainWindow.Setup.cs`.

#### Change the chat or its server

- **Messages stay end-to-end encrypted.** Direct and party messages are encrypted on the user's PC before they are sent. Never add anything that sends or stores them as plain text. Only the public lobby and the party finder are public.
- **Keep the SQL re-runnable.** Everything in `chat-all.sql` can be run more than once: `create table if not exists`, `drop policy if exists`, `create or replace function`.
- **Turn on row level security** for every new table and write a policy for each thing a user may do. Nobody should be able to read or write rows that are not theirs.
- **Add a rate limit** to any table users can insert into (see the `rate_limit_sender` triggers).
- **Make deleting work.** If you add a table that holds a user's data, make sure the "delete my account" and "delete my messages" functions remove it too.
- **Collect as little as possible.** Every new piece of data has to be justified.
- **Update the legal text.** If you collect, store or share something new, update `GameChat/LegalText.cs` (what the app shows), `PRIVACY.md` and `TERMS.md` together, and change the "Last updated" date.

#### Add a picture, logo or sound

- Game card pictures go in `Assets\HaloGameLogo`. The file name (without the extension) must match the name in `LogoNames`.
- Countdown sounds are in `Assets` and are packed into the `.exe` (`Sounds\beep.wav` and `Sounds\go.wav`, set in `AHFMC.csproj`).
- The menu sounds (move, select, back) are `ClassicHalo\assets\ui\sfx-*.wav`. They play in every layout, including Normal (`PlayMenuSound()` in `MainWindow.Extras.cs`).
- Only use pictures and sounds you made, or that you have permission to share. Say where they came from in your pull request.

### Style

- Match the code around your change. C# uses 4 spaces, nullable reference types are on, and each `MainWindow.*.cs` file has one job.
- The code has no comments. Put your explanations in the pull request description, not in the code.
- **Words in the app:** short, plain sentences. Name the real buttons ("Locate: pick the game's .exe yourself. Forget: stop using your own copy.") and never say "Right" or "Left" for buttons, because they depend on the layout. Write "Direct messages", not "DMs". Use the **same wording in all three menus**.
- Keep the app working offline: a failed download or update check must never crash the launcher.

### Before you send a pull request

- [ ] It builds without new warnings.
- [ ] You started the launcher with a fresh `config.json` and it works.
- [ ] If you changed anything visible, you tried **Normal, Classic Halo CE and Big Picture**.
- [ ] If you changed a setting, you changed it in one menu and checked the others show the same value, and that it is still set after a restart.
- [ ] If you changed a web file, you ran `node --check` on it and rebuilt.
- [ ] You updated the README, INSTALL.md and the legal text if what you did changes how things work or what data is collected.
- [ ] You did not change `<Version>` in `AHFMC.csproj`. The maintainer does that when releasing.
- [ ] No passwords, keys, tokens or personal data are in your change. (The Supabase "anon" key in `GameChat/Config.cs` is public by design; do not add a service or secret key anywhere.)

### Sending the pull request

1. Fork the project and make a branch with a short name, such as `add-halo-wars-3` or `fix-settings-text`.
2. Make small commits with clear messages, such as "Add Halo Wars 3 to Official Games".
3. Push, then open a pull request. Say **what** you changed, **why**, and **how you tested it**. Add a screenshot for visible changes and link the issue it fixes.
4. Be ready for feedback. Reviews are about the code, never about you.

By opening a pull request you confirm that the work is yours (or that you have the right to share it) and that you are happy for it to become part of the project, under the [GNU General Public License v3.0](LICENSE).

### Questions

Not sure where to start? [Open an issue](https://github.com/WhyWasThisNotAThing/AHFMC-A-Halo-Fan-Made-Collection/issues) and ask. A half-formed question is fine.

---

<a id="doc-code-of-conduct"></a>

## Code of Conduct

"AHFMC: A Halo Fan-Made Collection" is a friendly fan project. This code of conduct says how we expect people to behave in it, so that everyone can enjoy the launcher, the chat and the community around it.

### Where it applies

- The in-app chat: the public lobby, parties, party finder posts, direct messages and the appeal chat.
- This project on GitHub: issues, pull requests, discussions and release comments.
- Anywhere else you represent the project in public.

### What we expect

- Be kind and respectful. Disagree with ideas, not with people.
- Welcome new players and new contributors, whatever their age, skill, background or platform.
- Keep it fair: no cheating, boosting or scamming, and follow the rules of the games you play.
- Respect privacy. Do not share other people's personal information, private messages or screenshots of them without permission.
- Report problems instead of escalating them.

### What is not allowed

- Harassment, threats, bullying, stalking or hate of any kind, including slurs and attacks on someone's identity.
- Sexual content involving minors. This is reported to the authorities.
- Illegal content, spam, scams, malware, phishing, or links meant to steal accounts or data.
- Pretending to be another person, or pretending to be part of the project or of Halo.
- Trying to break, overload or get around the chat, its limits or its security.
- Making a new account to get around a hold or a ban.
- Filing false or abusive reports.

Security problems: please do not post them publicly. Follow [SECURITY.md](#doc-security).

### How to report

- **In the chat:** use the report button on the message or post. Add a short reason, and attach the message as evidence if you can.
- **On GitHub:** [open an issue](https://github.com/WhyWasThisNotAThing/AHFMC-A-Halo-Fan-Made-Collection/issues) and ask for a private way to talk, so nothing private is posted in public.

Reports are read by the people who run the chat and the project. They are treated carefully and only shared with the people who need to see them to deal with them.

### What happens next

Depending on how serious it is and whether it is repeated, we may:

1. remove the content;
2. give a warning;
3. limit features or put an account **on hold**;
4. **ban** the account from the chat, and block the PC from being used for other chat accounts;
5. block the person from the project on GitHub.

If your account is on hold or banned, you will see the reason and can write to the moderators in the appeal chat. Decisions can be changed, but there is no promise that they will be. The [Terms of Service](#doc-terms) and [Privacy Policy](#doc-privacy) explain the rest.

### Changes

This code may be updated as the project grows. The newest version is the one in the project folder on GitHub.

---

<a id="doc-security"></a>

## Security Policy

AHFMC: A Halo Fan-Made Collection includes an end-to-end encrypted chat. It is a small hobby project and has not been independently audited. Reports that help make it safer are welcome.

### Supported versions

Only the newest release gets security fixes. Please update before you report, using the launcher's built-in updater or the [Releases page](https://github.com/WhyWasThisNotAThing/AHFMC-A-Halo-Fan-Made-Collection/releases).

### How to report a vulnerability

Please do not post details of a security problem in a public issue, a pull request, the in-app chat or Discord.

1. **Preferred:** on the GitHub project, open the **Security** tab and choose **Report a vulnerability**. This sends the report privately.
2. **If that button is not there:** [open an issue](https://github.com/WhyWasThisNotAThing/AHFMC-A-Halo-Fan-Made-Collection/issues) that only says you have found a security problem, with no details, and ask for a private way to share them.

Please include:

- what is affected (the launcher, the chat, the updater, the server scripts) and the version, shown in the bottom right corner of the launcher;
- the steps to reproduce it, and what you expected to happen compared to what happened;
- what an attacker could do with it;
- a proof of concept, if you have one, that does not harm other people.

### What happens next

This is a one-person hobby project, so I can only promise best effort. I aim to reply within a week, tell you whether I can reproduce it, and fix real problems in a new release. I will credit you in the release notes if you want, and I will tell you if you should keep it private until the fix is out. There is no bug bounty.

### What is in scope

- The launcher and its in-app updater, including how it downloads and runs files.
- The chat client: sign-in, encryption, keys, messages, parties, reports and moderation.
- The server scripts in `GameChat/sql files/`, such as the database access rules (Row Level Security).
- Mistakes in the Privacy Policy that make it say something the code does not do.

### What is not in scope

- The Halo games and the community projects the launcher downloads. Report those to their makers.
- Problems in Supabase, Cloudflare, Discord, GitHub, Microsoft WebView2 or Windows themselves. Report those to the service.
- The Supabase project URL, the public (anon) key and the Turnstile site key in `GameChat/Config.cs`. They are meant to be public. Protection comes from the database access rules, so a report that shows data can be read or changed through them is in scope.
- Denial-of-service attacks, spam, and social engineering of people or accounts.
- A lost password or passphrase. By design there is no recovery.
- Problems that need a PC the attacker already controls.

### Testing rules

Please test only with accounts you created yourself, and do not read, change or delete other people's data or messages. Do not overload the chat server, and do not run automated scans against it. If you follow these rules and report in good faith, I will not take action against you. This promise does not cover Supabase, Cloudflare or other services, and they may have their own rules.

### Staying safe as a user

- Download the launcher only from the project's Releases page.
- Pick a strong password and a separate, strong encryption passphrase, and keep them safe.
- Use the fingerprint check in the chat to verify your contacts.
- Only add programs to My Apps that you trust.

---

<a id="doc-privacy"></a>

## Privacy Policy

_Last updated: 9 November 2026_

This explains what "AHFMC: A Halo Fan-Made Collection" (the launcher) and the chat built into it collect, where that data goes, and how long it is kept. The aim is to collect as little as possible. In short: no ads, no selling your data, no analytics or tracking, and no account is needed to use the launcher itself.

### 1. What the chat collects

You only have this if you create a chat account.

- Account details: your username and a password (stored by the sign-up service as a hash, never in plain text). No email address and no real name are asked for.
- Your public encryption key, so people can send you encrypted messages.
- An encrypted backup of your private keys. It is locked with your passphrase on your PC before it is uploaded, so the server cannot open it. It lets you sign in on another PC.
- Your messages. Direct messages and party messages are end-to-end encrypted: the server stores and passes on encrypted data it cannot read. The public lobby and the party finder are not private, and what you post there can be read by others.
- Party finder posts. They are public and expire on their own (after 2 hours unless the poster picks another time, and never later than 24 hours).
- Basic technical data the online service handles to run, for example the time a message was sent and, for a short time, network details such as an IP address in the sign-in service's logs.
- Reports: if someone reports a message or post, the report is stored so it can be reviewed. It contains the reported username, the reporter's username, the reason, and any evidence the reporter chose to attach (this can include the text of a message).
- A device code: a one-way scrambled code made from your PC's Windows id (the real id is never sent). It is stored with your account, with the time it was last seen, so that someone who is banned, or whose chats are on hold, cannot just make or use another account on the same PC.
- Moderation records: if your account is put on hold or banned, that status, the reason, and anything you write in the appeal chat are stored.

### 2. What the launcher does without an account

The launcher has no analytics and sends no usage data to us. It does talk to other services to do its job, and those services can see your IP address when it does:

- GitHub: to look for launcher updates, to show release notes (news), and to download community games and tools from their release pages.
- The makers' own download pages for some community games (for example Open Community Edition and Project Groundside).
- Discord, only if Discord Rich Presence is on: the launcher tells the Discord app on your PC that you use it, and which game you are playing. Turn it off in Settings > Launcher and the launcher does not connect to Discord at all.
- Websites you open in the built-in browser (for example Steam Workshop pages). Those sites have their own privacy policies.

### 3. What stays on your PC

Almost everything the launcher knows about you stays in a folder in your user profile (%AppData%\AHFMC) and is never uploaded:

- Your settings, where your games are installed, the apps you added to the launcher and their pictures (pictures you pick are copied there, not uploaded), and downloaded games.
- Play time: how long you have played each game and when you last played it. It is only shown to you in the launcher.
- Chat settings and which contacts you have verified. If you choose "Stay signed in", a sign-in token is saved, and your passphrase may be saved too, protected with Windows data protection. Turn the option off (or log out) to remove them.
- The built-in browser keeps its usual web data (cache and cookies) in the same folder.

### 4. How we use it

Only to run the launcher and the chat: sign you in, deliver messages, show parties and the lobby, apply limits against abuse, review reports, and keep banned people from coming back with another account. We do not sell your data, show ads or build profiles about you.

### 5. Who else sees it

- The online service that hosts the chat (Supabase) stores and processes the chat data above for us. It may also keep its own technical logs under its own policy.
- A human check (Cloudflare Turnstile) is used when you sign up or log in, and is provided by Cloudflare. It may look at your browser and connection to tell people from bots.
- Other chat users see your username, what you post publicly, and your messages if you sent them to them.
- The people who run the chat can read reports and appeal messages, and can see account status. They cannot read your direct or party messages, unless a message is attached to a report as evidence.
- Information may be given to the authorities if the law requires it or to prevent serious harm.
- GitHub, Discord and the other services listed above receive what is needed for the things they do for you, as described in this policy.

### 6. How long we keep it

- Messages, keys and your account are kept until you delete them or delete your account.
- Party finder posts expire on their own (within 24 hours at most) and are deleted by the service about a day after that.
- The chat service is set up to clear IP addresses and sign-in audit records from its logs about every hour. This is done on a best-effort basis and may not be possible in every case.
- Reports and moderation records are kept as long as they are needed to review them and to keep the chat safe.
- Device codes are kept with the account and removed when the account is deleted.

### 7. Deleting your data

You can delete all of your messages, or your whole account (account, keys, key backup, messages and parties), in Settings > Chat. Deleted data is removed from the chat service. Direct messages disappear for the other person too. Reports and short-lived technical logs may be kept a little longer.

Removing the launcher does not delete your data on your PC. To remove it, delete the folder %AppData%\AHFMC after uninstalling.

### 8. Security

Messages are encrypted before they leave your PC. The passphrase that protects your key backup is turned into a key with a slow, memory-hard method (Argon2) to make guessing harder. You can compare your fingerprint with a contact's to check that nobody is in the middle. No system is perfectly safe: keep your password and passphrase private and do not reuse them.

### 9. Your choices and rights

You can choose what to share, turn off notifications, previews and the popup over games, stop staying signed in, turn off Discord status, turn off update checks, and delete your data. Depending on where you live you may also have rights to ask what is held about you, to correct it, or to have it removed; use the contact below.

### 10. Children

The chat is not meant for children under 16 (or the minimum age where you live). Do not create an account if you are under that age. If you think a child has made an account, contact us and it will be removed.

### 11. Changes

This policy may change. The new version is shown with the app and the date above is updated.

### 12. Contact

Questions or requests: email whywasthisnotathing@gmail.com (the private way, best for anything personal or any request about your data), or open an issue at <https://github.com/WhyWasThisNotAThing/AHFMC-A-Halo-Fan-Made-Collection/issues>. Please do not post private details in a public issue; use email for those.

---

<a id="doc-terms"></a>

## Terms of Service

_Last updated: 9 November 2026_

These terms cover the launcher "AHFMC: A Halo Fan-Made Collection" and the chat that is built into it. By using the launcher you accept the parts that apply to the launcher. By ticking the box when you create a chat account, you also accept the parts about the chat. If you do not agree, do not use the launcher or create an account.

### 1. What this is

The launcher and its chat are a free, fan-made project. They are not made by, endorsed by or connected to Microsoft, Xbox Game Studios, Halo Studios, 343 Industries or any other owner of Halo. Halo and related names, logos and images belong to their owners.

### 2. Who may use it

You must be at least 16 years old, or the minimum age in the place where you live if that is higher, to create a chat account. If you are under 18, make sure a parent or guardian is fine with you using it.

### 3. The launcher

The launcher is free. Its source code is released under the GNU General Public License v3 (the LICENSE file in the project), which says what you may do with the code. That license covers only the code written for this project, not Halo or its names, logos, sounds and images, which belong to their owners.

The launcher does not contain the Halo games. It starts games that you own and have installed (for example from Steam or the Microsoft Store), and it can download community games and tools from the pages of their makers. Those games and tools are made by other people, have their own rules and licenses, and are used at your own risk.

The launcher can look for a newer version of itself on GitHub. It always asks before it downloads or installs anything. The launcher can start other programs you add to it. Only add programs you trust.

### 4. Your chat account

- You pick a username (3-16 letters, numbers or underscores) and a password, plus an encryption passphrase.
- You are responsible for everything done with your account and for keeping your password and passphrase safe.
- There is no password reset and no account recovery. If you lose your password or passphrase, your account and messages cannot be recovered.
- One person, one account. Do not pretend to be someone else, and do not pretend to work on the project or for Halo.

### 5. How you may use the chat

You also agree to follow the Chat Rules shown when you create an account and the [Code of Conduct](#doc-code-of-conduct), which applies to the chat and to this project on GitHub.

You agree not to:

- harass, threaten, bully or hate on other people;
- share illegal content, or sexual content involving minors (this is reported to the authorities);
- send spam, scams, malware or links meant to steal accounts or data;
- try to break, overload or get around the chat, its limits or its security, or use bots or scripts to send messages;
- share other people's private information without their permission;
- make a new account to get around a hold or a ban;
- use the chat to break the law or the rules of any game or service.

Parties and the party finder are public places: anything you post there can be read by others. The public lobby is not private either.

### 6. Your content

You keep the rights to what you write. You allow the chat to store, deliver and show it to the people you send it to, which is needed for it to work. You are responsible for what you send.

### 7. Reports, holds and bans

Messages and posts can be reported. A report contains the reason you give and any evidence you choose to attach, such as the text of a message, and it is read by the people who run the chat. Please report in good faith: false or abusive reports can lead to action against your own account.

We may remove content, limit features, put an account on hold or ban it if it breaks these terms or puts others at risk, with or without warning. If your account is on hold or banned you will see the reason, and you can write to the moderators in the appeal chat. A decision is not guaranteed to change.

To stop people from simply starting over, a PC that is linked to an account that is on hold or banned may be blocked from signing up for or using other chat accounts. See the privacy policy for how this works.

You can delete your messages or your whole account yourself in Settings > Chat at any time.

### 8. Other people's services

The chat and the launcher rely on other services, including the online service that hosts the chat (Supabase), the human check used at sign-up (Cloudflare Turnstile), GitHub (updates, news and game downloads), Discord (the optional status) and Microsoft's WebView2 (the built-in browser). They have their own terms and privacy policies, and we are not responsible for them.

### 9. No warranty

The launcher and chat are provided "as is", without promises that they will always work, be secure, be available, or keep your messages. The chat can change, break or be shut down at any time, and the launcher may stop working with a game after that game is updated.

### 10. Limit of liability

As far as the law allows, the people who make this project are not responsible for losses or damage that come from using it, including lost messages or accounts, problems caused by games or programs you start from the launcher, or messages you receive from other users. Nothing here limits rights you have by law that cannot be limited.

### 11. Stopping

You can stop using the launcher and the chat whenever you like. To remove your chat data, delete your account in Settings > Chat. We may stop running the chat or the project at any time.

### 12. Changes

These terms may change. When they do, the new version will be shown with the app and the date above will be updated. If you keep using the launcher or the chat after a change, you accept the new terms.

### 13. Contact

Questions or reports: email whywasthisnotathing@gmail.com (the private way, best for anything personal or any request about your data), or open an issue at <https://github.com/WhyWasThisNotAThing/AHFMC-A-Halo-Fan-Made-Collection/issues>. Please do not post private details in a public issue; use email for those.

---

<a id="doc-third-party-notices"></a>

## Third-party notices

AHFMC: A Halo Fan-Made Collection is licensed under the [GNU General Public License v3.0](LICENSE). It uses the third-party software below, which keeps its own licenses. Their copyright notices and license text are reproduced here as those licenses require.

Before each release, check the `LICENSE` file inside each NuGet package and make sure the copyright lines below still match.

### Software included in the launcher

| Component | Used for | License |
| --- | --- | --- |
| [.NET runtime and libraries](https://github.com/dotnet/runtime) | The launcher is published self-contained, so the runtime is inside the `.exe` | MIT |
| [Newtonsoft.Json](https://www.newtonsoft.com/json) 13.0.3 | Reading and writing JSON | MIT |
| [Konscious.Security.Cryptography.Argon2](https://github.com/kmaragon/Konscious.Security.Cryptography) 1.3.x | Turning your chat passphrase into an encryption key (Argon2) | MIT |
| [DiscordRichPresence](https://github.com/Lachee/discord-rpc-csharp) 1.x | The optional Discord status | MIT |
| [Microsoft.Web.WebView2](https://www.nuget.org/packages/Microsoft.Web.WebView2) 1.0.2903.40 | The built-in browser for the Classic and Big Picture layouts | Microsoft's own license terms, in the `LICENSE.txt` inside the package |

The .NET runtime also ships its own `THIRD-PARTY-NOTICES.TXT` for the components inside it. Include that file with a release if you distribute the self-contained build.

#### MIT License

Applies to the .NET runtime, Newtonsoft.Json, Konscious.Security.Cryptography.Argon2 and DiscordRichPresence.

Copyright notices:

- .NET runtime and libraries: Copyright (c) .NET Foundation and Contributors
- Newtonsoft.Json: Copyright (c) 2007 James Newton-King
- Konscious.Security.Cryptography.Argon2: Copyright (c) 2017 Keef Aragon

```
Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
```

### Fonts, sounds and images

The fonts, music, video, sounds and images used by the Classic and Big Picture layouts are not part of this repository and are not covered by the GPL. See [ASSETS.md](#doc-assets). Whoever builds the launcher is responsible for the license of each file they add. Add each file's license or credit here when you ship it.

### Optional Halo files

The Classic and Big Picture layouts can use sounds and images that you add yourself, and the Hybrid layouts show a background video and music that you pick in Settings (see [ASSETS.md](#doc-assets)). They are not part of this project. Whoever adds them is responsible for having the right to use them, and should not include them in a public release unless they do.

### Halo

Halo and everything that belongs to it are owned by Microsoft, Xbox Game Studios and Halo Studios. This project is not made by, affiliated with or endorsed by them. Microsoft's [Game Content Usage Rules](https://www.xbox.com/en-us/developers/rules) require a notice for the Halo content used:

**Halo: Combat Evolved** © Microsoft Corporation. AHFMC: A Halo Fan-Made Collection was created under Microsoft's "Game Content Usage Rules" using assets from Halo: Combat Evolved, and it is not endorsed by or affiliated with Microsoft. <https://www.xbox.com/en-us/developers/rules>

**Halo: Reach** © Microsoft Corporation. AHFMC: A Halo Fan-Made Collection was created under Microsoft's "Game Content Usage Rules" using assets from Halo: Reach, and it is not endorsed by or affiliated with Microsoft. <https://www.xbox.com/en-us/developers/rules>

### Programs the launcher can download or start

The community Halo projects the launcher can download (Reclaimer, Halo Reach Renaissance, Halo 2 Gravemind, Open Community Edition and Project Groundside) and the games and programs you add yourself are separate programs by other people. They are not part of this project and have their own licenses and rules.

### Online services

The launcher and chat use Supabase (chat server), Cloudflare Turnstile (sign-up check), GitHub (updates, news and downloads), Discord (optional status) and the Microsoft Edge WebView2 Runtime. Their own terms apply to them.
