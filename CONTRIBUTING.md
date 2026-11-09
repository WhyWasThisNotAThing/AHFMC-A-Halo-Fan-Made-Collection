# How to contribute

Thanks for wanting to help. This guide shows how to set the project up, how it fits together, and step by step how to add the usual things: a setting, a Halo game, a community game, a news source or a chat feature.

Please read the [Code of Conduct](CODE_OF_CONDUCT.md) first. It applies to issues, pull requests and the in-app chat.

## Ways to help

- **Report a bug.** [Open an issue](https://github.com/WhyWasThisNotAThing/AHFMC-A-Halo-Fan-Made-Collection/issues) with your Windows version, the launcher version (bottom right corner), what you did and what happened. Never post passwords, passphrases or private messages.
- **Suggest an idea.** Open an issue first and say what problem it solves. For anything big, wait for a reply before you start coding, so your work is not wasted.
- **Improve the docs or the wording** in the app and in these files.
- **Send code.** Small, focused pull requests are easiest to review.

## Set up

You need:

- Windows 10 or 11, 64-bit
- The .NET 10 SDK
- Visual Studio 2026 (with the ".NET desktop development" workload) or VS Code with the C# extension
- Nothing else. The project builds without any Halo files. The optional extras are listed in [ASSETS.md](ASSETS.md)

Build and run:

```bat
dotnet build
dotnet run
```

`Builder_and_Installer\Build.bat` publishes the single `.exe` and starts it. `Builder_and_Installer\build-release.bat` builds the portable file and the installer, and is for maintainers (see [Building from source](README.md#building-from-source)).

Useful to know while testing:

- The launcher keeps its data in `%AppData%\AHFMC`. To see the welcome screen again, close the launcher and rename `config.json` in that folder.
- The launcher does not ship a background video or music. To test the Hybrid layouts, pick your own `.mp4` and `.mp3` in Settings > Launcher > Background video / Background music. They are copied to `%AppData%\AHFMC\ClassicHalo\assets\ui`.
- Everything in `ClassicHalo\` is packed **inside** the `.exe` and copied to `%AppData%\AHFMC\ClassicHalo` when the launcher starts. After you change a web file, **rebuild and restart** the launcher or you will keep seeing the old version.
- Close any running copy before you build, or Windows will lock the `.exe`.

## How the project fits together

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

## Recipes

> **Tip for everything below:** the easiest way to add something is to copy something that already exists. Search the whole project for the name of a similar item (for example `NewsReach`, `infinite` or `Gravemind`) and add the matching line next to every hit.

### Add a setting

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

### Add an official Halo game

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

### Add a community (fan) game

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

### Add a news source

News shows the newest GitHub release of each project.

1. Add `(id, "Display name", repo)` to the `sources` array in `FetchNewsAsync()` (`MainWindow.xaml.cs`).
2. Add a `NewsXxx` property to `Config` and a case in `NewsEnabled(...)`.
3. Add the setting in `BuildSettings()` (Settings > News). All three menus show it.
4. Add a `SetupToggle(...)` in `MainWindow.Setup.cs`.

### Change the chat or its server

- **Messages stay end-to-end encrypted.** Direct and party messages are encrypted on the user's PC before they are sent. Never add anything that sends or stores them as plain text. Only the public lobby and the party finder are public.
- **Keep the SQL re-runnable.** Everything in `chat-all.sql` can be run more than once: `create table if not exists`, `drop policy if exists`, `create or replace function`.
- **Turn on row level security** for every new table and write a policy for each thing a user may do. Nobody should be able to read or write rows that are not theirs.
- **Add a rate limit** to any table users can insert into (see the `rate_limit_sender` triggers).
- **Make deleting work.** If you add a table that holds a user's data, make sure the "delete my account" and "delete my messages" functions remove it too.
- **Collect as little as possible.** Every new piece of data has to be justified.
- **Update the legal text.** If you collect, store or share something new, update `GameChat/LegalText.cs` (what the app shows), `PRIVACY.md` and `TERMS.md` together, and change the "Last updated" date.

### Add a picture, logo or sound

- Game card pictures go in `Assets\HaloGameLogo`. The file name (without the extension) must match the name in `LogoNames`.
- Countdown sounds are in `Assets` and are packed into the `.exe` (`Sounds\beep.wav` and `Sounds\go.wav`, set in `AHFMC.csproj`).
- The menu sounds (move, select, back) are `ClassicHalo\assets\ui\sfx-*.wav`. They play in every layout, including Normal (`PlayMenuSound()` in `MainWindow.Extras.cs`).
- Only use pictures and sounds you made, or that you have permission to share. Say where they came from in your pull request.

## Style

- Match the code around your change. C# uses 4 spaces, nullable reference types are on, and each `MainWindow.*.cs` file has one job.
- The code has no comments. Put your explanations in the pull request description, not in the code.
- **Words in the app:** short, plain sentences. Name the real buttons ("Locate: pick the game's .exe yourself. Forget: stop using your own copy.") and never say "Right" or "Left" for buttons, because they depend on the layout. Write "Direct messages", not "DMs". Use the **same wording in all three menus**.
- Keep the app working offline: a failed download or update check must never crash the launcher.

## Before you send a pull request

- [ ] It builds without new warnings.
- [ ] You started the launcher with a fresh `config.json` and it works.
- [ ] If you changed anything visible, you tried **Normal, Classic Halo CE and Big Picture**.
- [ ] If you changed a setting, you changed it in one menu and checked the others show the same value, and that it is still set after a restart.
- [ ] If you changed a web file, you ran `node --check` on it and rebuilt.
- [ ] You updated the README, INSTALL.md and the legal text if what you did changes how things work or what data is collected.
- [ ] You did not change `<Version>` in `AHFMC.csproj`. The maintainer does that when releasing.
- [ ] No passwords, keys, tokens or personal data are in your change. (The Supabase "anon" key in `GameChat/Config.cs` is public by design; do not add a service or secret key anywhere.)

## Sending the pull request

1. Fork the project and make a branch with a short name, such as `add-halo-wars-3` or `fix-settings-text`.
2. Make small commits with clear messages, such as "Add Halo Wars 3 to Official Games".
3. Push, then open a pull request. Say **what** you changed, **why**, and **how you tested it**. Add a screenshot for visible changes and link the issue it fixes.
4. Be ready for feedback. Reviews are about the code, never about you.

By opening a pull request you confirm that the work is yours (or that you have the right to share it) and that you are happy for it to become part of the project, under the [GNU General Public License v3.0](LICENSE).

## Questions

Not sure where to start? [Open an issue](https://github.com/WhyWasThisNotAThing/AHFMC-A-Halo-Fan-Made-Collection/issues) and ask. A half-formed question is fine.
