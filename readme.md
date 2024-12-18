# Moneo
A reminder system to ping me repeatedly (badger me) to take my medication, built on Azure Functions. Currently using Durable Entities for state management.

This started as something of a simple "remind me and badger me" script for a single task (take my medication). So a fancy CRON job done as an Azure Durable Function.

Then I wanted a similar thing for checking my blood pressure and other tasks (remind me when it's due and then badger me until I say I've completed it), and so I complicated it to a ridiculous level, while trying to stay entirely within Azure Durable Functions/Entities because I thought it was kind of funny to do so (though it presents some challenges to do so).

### Manage tasks via chat

You can skip, complete, or even create tasks from within Telegram.

![image](https://github.com/rumdood/Moneo/assets/3585996/f1e795b1-2db9-4eb9-9f3d-a5622db8acc5)

The bot has a set of HTTP-based triggers for CRUD with tasks as well as marking a given task as completed or skipped.

For my personal setup I have a Flic Button in my medicine cabinet that, when pressed, marks my medication task as completed for the current interval (it's twice a day). Other tasks I now manage entirely through the Telegram interface.

## Developer Requirements
* .NET 6 or higher (Azure Functions for managing tasks)
* .NET 8 or higher (Azure Functions for interacting with Telegram, local hosting for Telegram interaction)
* Azure Functions Runtime

### Optional
* Docker Desktop (for running Azurite if you don't have it installed as a service)

## Deployment Requirements
* Azure Account

## Chat Requirements
* While I had originally planned to make which channel the bot works in be configurable, right now I've leaned into using Telegram because I have that on all my devices, meaning that my reminders and badgers can follow me from device to device rather than sitting unseen on my phone.
* At some point in the future I will probably look at redoing this using Microsoft's Azure Bot Framework, which will give it greater flexibility in channel, but at this point this project is kind of ridiculous on purpose.
djdjdjd