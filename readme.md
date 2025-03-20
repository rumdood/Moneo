# Moneo
A chatbot To-Do/Task Manager.

A better name probably would have been "BadgerBot" because its primary job is to badger you until you remember to complete your tasks. No more swiping away the single reminder.

This started as something of a simple "remind me and badger me" script for a single task (take my medication). So a fancy CRON job done as an Azure Durable Function.

Over time I've come to really depend on this for numerous tasks and was frustrated by some fo the limitations of the Azure Durable Functions (specifically the challenges around having items scheduled several days/weeks/months into the future). Plus the code had just become somewhat unwieldy over time, so things have been cleaned up.

The application now runs as two separate ASP.NET Core applications - one for the chat functionality and one for the task management functions. I had looked at putting everything into a single application host but found that the refactoring was more than I wanted to do at the moment. Additionally, this opens the door to allowing the ChatApi to interact with a third party backend like Todoist or something along those lines.

### Manage tasks via chat

You can skip, complete, or even create tasks from within Telegram.

![image](https://github.com/rumdood/Moneo/assets/3585996/f1e795b1-2db9-4eb9-9f3d-a5622db8acc5)

The bot has a set of HTTP-based calls for CRUD with tasks as well as marking a given task as completed or skipped.

For my personal setup I have a Flic Button in my medicine cabinet that, when pressed, marks my medication task as completed for the current interval (it's twice a day). Other tasks I now manage entirely through the Telegram interface.

## Developer Requirements
- .NET 9 or higher

### Optional
- Docker Desktop

## Deployment Requirements
You can run the services as individual processes or in docker/docker-compose. The provided `docker-compose.yml` file is a sample of how to run them. You'd want to setup appropriate API keys, get a Telegram API key, etc.

I currently run this using docker using a reverse proxy to make the services appear as a single host. The Task Management API supports either Sqlite or Postgres as a data store. The Chat API is does not require a permanent data store.

## Chat Requirements
- Telegram account

At some point I'd like to support other chat platforms. If you'd like to support one, you can implement an additional `Moneo.Chat.IChatAdapter` and host for it. 
