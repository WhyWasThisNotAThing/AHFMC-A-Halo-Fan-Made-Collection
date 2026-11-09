# Privacy Policy

_Last updated: 9 November 2026_

This explains what "AHFMC: A Halo Fan-Made Collection" (the launcher) and the chat built into it collect, where that data goes, and how long it is kept. The aim is to collect as little as possible. In short: no ads, no selling your data, no analytics or tracking, and no account is needed to use the launcher itself.

## 1. What the chat collects

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

## 2. What the launcher does without an account

The launcher has no analytics and sends no usage data to us. It does talk to other services to do its job, and those services can see your IP address when it does:

- GitHub: to look for launcher updates, to show release notes (news), and to download community games and tools from their release pages.
- The makers' own download pages for some community games (for example Open Community Edition and Project Groundside).
- Discord, only if Discord Rich Presence is on: the launcher tells the Discord app on your PC that you use it, and which game you are playing. Turn it off in Settings > Launcher and the launcher does not connect to Discord at all.
- Websites you open in the built-in browser (for example Steam Workshop pages). Those sites have their own privacy policies.

## 3. What stays on your PC

Almost everything the launcher knows about you stays in a folder in your user profile (%AppData%\AHFMC) and is never uploaded:

- Your settings, where your games are installed, the apps you added to the launcher and their pictures (pictures you pick are copied there, not uploaded), and downloaded games.
- Play time: how long you have played each game and when you last played it. It is only shown to you in the launcher.
- Chat settings and which contacts you have verified. If you choose "Stay signed in", a sign-in token is saved, and your passphrase may be saved too, protected with Windows data protection. Turn the option off (or log out) to remove them.
- The built-in browser keeps its usual web data (cache and cookies) in the same folder.

## 4. How we use it

Only to run the launcher and the chat: sign you in, deliver messages, show parties and the lobby, apply limits against abuse, review reports, and keep banned people from coming back with another account. We do not sell your data, show ads or build profiles about you.

## 5. Who else sees it

- The online service that hosts the chat (Supabase) stores and processes the chat data above for us. It may also keep its own technical logs under its own policy.
- A human check (Cloudflare Turnstile) is used when you sign up or log in, and is provided by Cloudflare. It may look at your browser and connection to tell people from bots.
- Other chat users see your username, what you post publicly, and your messages if you sent them to them.
- The people who run the chat can read reports and appeal messages, and can see account status. They cannot read your direct or party messages, unless a message is attached to a report as evidence.
- Information may be given to the authorities if the law requires it or to prevent serious harm.
- GitHub, Discord and the other services listed above receive what is needed for the things they do for you, as described in this policy.

## 6. How long we keep it

- Messages, keys and your account are kept until you delete them or delete your account.
- Party finder posts expire on their own (within 24 hours at most) and are deleted by the service about a day after that.
- The chat service is set up to clear IP addresses and sign-in audit records from its logs about every hour. This is done on a best-effort basis and may not be possible in every case.
- Reports and moderation records are kept as long as they are needed to review them and to keep the chat safe.
- Device codes are kept with the account and removed when the account is deleted.

## 7. Deleting your data

You can delete all of your messages, or your whole account (account, keys, key backup, messages and parties), in Settings > Chat. Deleted data is removed from the chat service. Direct messages disappear for the other person too. Reports and short-lived technical logs may be kept a little longer.

Removing the launcher does not delete your data on your PC. To remove it, delete the folder %AppData%\AHFMC after uninstalling.

## 8. Security

Messages are encrypted before they leave your PC. The passphrase that protects your key backup is turned into a key with a slow, memory-hard method (Argon2) to make guessing harder. You can compare your fingerprint with a contact's to check that nobody is in the middle. No system is perfectly safe: keep your password and passphrase private and do not reuse them.

## 9. Your choices and rights

You can choose what to share, turn off notifications, previews and the popup over games, stop staying signed in, turn off Discord status, turn off update checks, and delete your data. Depending on where you live you may also have rights to ask what is held about you, to correct it, or to have it removed; use the contact below.

## 10. Children

The chat is not meant for children under 16 (or the minimum age where you live). Do not create an account if you are under that age. If you think a child has made an account, contact us and it will be removed.

## 11. Changes

This policy may change. The new version is shown with the app and the date above is updated.

## 12. Contact

Questions or requests: email whywasthisnotathing@gmail.com (the private way, best for anything personal or any request about your data), or open an issue at <https://github.com/WhyWasThisNotAThing/AHFMC-A-Halo-Fan-Made-Collection/issues>. Please do not post private details in a public issue; use email for those.
