# Lesson 1 — Multi-turn Conversation with the Responses API

Multi-turn conversation with the Responses API using full input history.

## Run

```bash
dotnet run --project Lesson-1
```

## Environment variables

| Variable | Required | Default |
|---|---|---|
| `AI_API_KEY` | yes | — |
| `RESPONSES_API_ENDPOINT` | no | `https://api.openai.com/v1/responses` |
| `AI_MODEL` | no | `o4-mini` |

## What it does

1. Sends a question: `"What is 25 * 48?"`
2. Sends a follow-up: `"Divide that by 4."` with the previous exchange as context
3. Prints both answers with reasoning token counts
