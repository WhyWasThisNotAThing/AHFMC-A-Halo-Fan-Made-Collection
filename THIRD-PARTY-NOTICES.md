# Third-party notices

AHFMC: A Halo Fan-Made Collection is licensed under the [GNU General Public License v3.0](LICENSE). It uses the third-party software below, which keeps its own licenses. Their copyright notices and license text are reproduced here as those licenses require.

Before each release, check the `LICENSE` file inside each NuGet package and make sure the copyright lines below still match.

## Software included in the launcher

| Component | Used for | License |
| --- | --- | --- |
| [.NET runtime and libraries](https://github.com/dotnet/runtime) | The launcher is published self-contained, so the runtime is inside the `.exe` | MIT |
| [Newtonsoft.Json](https://www.newtonsoft.com/json) 13.0.3 | Reading and writing JSON | MIT |
| [Konscious.Security.Cryptography.Argon2](https://github.com/kmaragon/Konscious.Security.Cryptography) 1.3.x | Turning your chat passphrase into an encryption key (Argon2) | MIT |
| [DiscordRichPresence](https://github.com/Lachee/discord-rpc-csharp) 1.x | The optional Discord status | MIT |
| [Microsoft.Web.WebView2](https://www.nuget.org/packages/Microsoft.Web.WebView2) 1.0.2903.40 | The built-in browser for the Classic and Big Picture layouts | Microsoft's own license terms, in the `LICENSE.txt` inside the package |

The .NET runtime also ships its own `THIRD-PARTY-NOTICES.TXT` for the components inside it. Include that file with a release if you distribute the self-contained build.

### MIT License

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

## Fonts, sounds and images

The fonts, music, video, sounds and images used by the Classic and Big Picture layouts are not part of this repository and are not covered by the GPL. See [ASSETS.md](ASSETS.md). Whoever builds the launcher is responsible for the license of each file they add. Add each file's license or credit here when you ship it.

## Optional Halo files

The Classic and Big Picture layouts can use sounds and images that you add yourself, and the Hybrid layouts show a background video and music that you pick in Settings (see [ASSETS.md](ASSETS.md)). They are not part of this project. Whoever adds them is responsible for having the right to use them, and should not include them in a public release unless they do.

## Halo

Halo and everything that belongs to it are owned by Microsoft, Xbox Game Studios and Halo Studios. This project is not made by, affiliated with or endorsed by them. Microsoft's [Game Content Usage Rules](https://www.xbox.com/en-us/developers/rules) require a notice for the Halo content used:

**Halo: Combat Evolved** © Microsoft Corporation. AHFMC: A Halo Fan-Made Collection was created under Microsoft's "Game Content Usage Rules" using assets from Halo: Combat Evolved, and it is not endorsed by or affiliated with Microsoft. <https://www.xbox.com/en-us/developers/rules>

**Halo: Reach** © Microsoft Corporation. AHFMC: A Halo Fan-Made Collection was created under Microsoft's "Game Content Usage Rules" using assets from Halo: Reach, and it is not endorsed by or affiliated with Microsoft. <https://www.xbox.com/en-us/developers/rules>

## Programs the launcher can download or start

The community Halo projects the launcher can download (Halo 3 Reclaimer, Halo Reach Renaissance, Halo 2 Gravemind, Open Community Edition and Project Groundside) and the games and programs you add yourself are separate programs by other people. They are not part of this project and have their own licenses and rules.

## Online services

The launcher and chat use Supabase (chat server), Cloudflare Turnstile (sign-up check), GitHub (updates, news and downloads), Discord (optional status) and the Microsoft Edge WebView2 Runtime. Their own terms apply to them.
