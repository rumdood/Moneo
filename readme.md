# Moneo
A reminder system to ping me repeatedly to take my medication, built on Azure Functions. Currently using Durable Entities for state management.

***Not Functional At Present (3/10/2022)***
## Developer Requirements
* .NET 6 or higher
* Azure Functions Runtime

### Optional
* Docker Desktop (for running Azurite if you don't have it installed as a service)

## Deployment Requirements
* Azure Account

## Outstanding Decisions
* How will alerts/reminders be sent?
    * SMS?
    * Bot (e.g. Telegram/WhatsApp/Slack/Teams)?
    * Phone call (maybe for escalation)?