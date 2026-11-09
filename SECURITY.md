# Security Policy

AHFMC: A Halo Fan-Made Collection includes an end-to-end encrypted chat. It is a small hobby project and has not been independently audited. Reports that help make it safer are welcome.

## Supported versions

Only the newest release gets security fixes. Please update before you report, using the launcher's built-in updater or the [Releases page](https://github.com/WhyWasThisNotAThing/AHFMC-A-Halo-Fan-Made-Collection/releases).

## How to report a vulnerability

Please do not post details of a security problem in a public issue, a pull request, the in-app chat or Discord.

1. **Preferred:** on the GitHub project, open the **Security** tab and choose **Report a vulnerability**. This sends the report privately.
2. **If that button is not there:** [open an issue](https://github.com/WhyWasThisNotAThing/AHFMC-A-Halo-Fan-Made-Collection/issues) that only says you have found a security problem, with no details, and ask for a private way to share them.

Please include:

- what is affected (the launcher, the chat, the updater, the server scripts) and the version, shown in the bottom right corner of the launcher;
- the steps to reproduce it, and what you expected to happen compared to what happened;
- what an attacker could do with it;
- a proof of concept, if you have one, that does not harm other people.

## What happens next

This is a one-person hobby project, so I can only promise best effort. I aim to reply within a week, tell you whether I can reproduce it, and fix real problems in a new release. I will credit you in the release notes if you want, and I will tell you if you should keep it private until the fix is out. There is no bug bounty.

## What is in scope

- The launcher and its in-app updater, including how it downloads and runs files.
- The chat client: sign-in, encryption, keys, messages, parties, reports and moderation.
- The server scripts in `GameChat/sql files/`, such as the database access rules (Row Level Security).
- Mistakes in the Privacy Policy that make it say something the code does not do.

## What is not in scope

- The Halo games and the community projects the launcher downloads. Report those to their makers.
- Problems in Supabase, Cloudflare, Discord, GitHub, Microsoft WebView2 or Windows themselves. Report those to the service.
- The Supabase project URL, the public (anon) key and the Turnstile site key in `GameChat/Config.cs`. They are meant to be public. Protection comes from the database access rules, so a report that shows data can be read or changed through them is in scope.
- Denial-of-service attacks, spam, and social engineering of people or accounts.
- A lost password or passphrase. By design there is no recovery.
- Problems that need a PC the attacker already controls.

## Testing rules

Please test only with accounts you created yourself, and do not read, change or delete other people's data or messages. Do not overload the chat server, and do not run automated scans against it. If you follow these rules and report in good faith, I will not take action against you. This promise does not cover Supabase, Cloudflare or other services, and they may have their own rules.

## Staying safe as a user

- Download the launcher only from the project's Releases page.
- Pick a strong password and a separate, strong encryption passphrase, and keep them safe.
- Use the fingerprint check in the chat to verify your contacts.
- Only add programs to My Apps that you trust.
