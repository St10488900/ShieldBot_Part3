# ShieldBot — Cybersecurity Awareness Chatbot (Part 3 / POE)

**Student:** Mfundo Mchunu  
**Module:** PROG6221 — Programming 2A

---

## Overview

ShieldBot is a WPF GUI-based cybersecurity chatbot built across three parts:

- **Part 1:** Console chatbot with keyword detection, ASCII art, voice greeting, sentiment analysis
- **Part 2:** WPF GUI with XAML, styles, chat cards, typing animation, memory, delegates
- **Part 3:** Task Assistant (SQLite CRUD), Cybersecurity Quiz, NLP intent detection, Activity Log

---

## Features

### Part 1 & 2 (still fully working)
- Voice greeting on launch (Audio/greeting.wav)
- ASCII art / brand shield in header
- Name collection and personalised responses
- 8+ cybersecurity keyword responses (passwords, phishing, malware, etc.)
- Sentiment detection (worried / frustrated / curious) with empathetic replies
- Follow-up memory ("tell me more" continues the topic)
- Typing animation on bot responses
- Quick topic sidebar buttons
- Delegates: FormatBotMessage, ValidateUserInput, LogConversationEntry

### Part 3 — New Features
- **Task Assistant:** Add, view, complete, delete cybersecurity tasks. All stored in `database.db` (SQLite via EF Core). Loads from DB on startup.
- **Reminder system:** Each task stores a reminder field. Type `Add task to enable 2FA` in chat to add via NLP.
- **Quiz:** 13 questions across phishing, passwords, safe browsing, social engineering, 2FA, malware, privacy, and data backup. One question at a time. Immediate feedback + explanation after each answer. Final score and message.
- **NLP Simulation:** Detects task, reminder, quiz, and log intents from varied phrasings using `string.Contains()`.
- **Activity Log:** Every significant action is logged with a timestamp to the database. Type `show activity log` to see the last 10 entries. "Show Full Log" button for all entries.

---

## Prerequisites

- Visual Studio 2022
- .NET 8.0
- NuGet packages (auto-restored on first build):
  - `Microsoft.EntityFrameworkCore.Sqlite` v8.0.0
  - `Microsoft.EntityFrameworkCore.Proxies` v8.0.0

---

## Setup Instructions

1. Clone or download the repository.
2. Open `CyberAwarenessBot.sln` in Visual Studio 2022.
3. Right-click the solution → **Restore NuGet Packages** (or build — it restores automatically).
4. Place `greeting.wav` in the `Audio/` folder (already included).
5. Press **F5** to run.

The SQLite database (`database.db`) is created automatically on first run in the app's output directory.

---

## NLP Examples (type in chat)

| You type | ShieldBot does |
|---|---|
| `Add task to enable two-factor authentication` | Creates task, asks about reminder |
| `Remind me to update my password in 3 days` | Creates task with reminder |
| `start quiz` | Switches to quiz panel |
| `show activity log` | Shows last 10 log entries in chat |
| `what have you done for me?` | Shows activity log |
| `I am worried about phishing` | Detects sentiment, gives phishing tip |
| `tell me more` | Continues current topic |

---

## Releases

| Tag | Contents |
|---|---|
| v1.0 | Part 1 — console chatbot, keyword detection, sentiment, ASCII art |
| v2.0 | Part 2 — WPF GUI, XAML styles, typing animation, delegates, memory |
| v3.0 | Part 3 — Tasks, Quiz, NLP, Activity Log, SQLite EF Core integration |

---

## YouTube Video

[Add your unlisted YouTube link here]

---

## GitHub Actions

CI workflow at `.github/workflows/ci.yml` — green tick confirms build passes.
