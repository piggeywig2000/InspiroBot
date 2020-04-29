# InspiroBot
A simple bot for KevinVG207's server, used for generating ''''inspirational'''' quotes and abusing certain members

Head over to the [releases](https://github.com/piggeywig2000/InspiroBot/releases) page to download it.

## Features

* Generate an inspirational quote using the `inspire` command
* Generate 10 inspirational quotes using the `bulkinspire` command
* Reload the configuration file using the `reloadconfig` command. Only the bot's owner can use this
* Apply certain roles to certain members (I call them sticky roles) automatically. When a certain role(s) is removed from a certain member(s), the bot automatically adds it back

## Configuration

To easily get channel/member IDs, enable the toggle at Discord settings -> Appearance -> Developer Mode

`Token`: Your bot's token. To get the bot's token, go to the [Discord Developer Portal](https://discordapp.com/developers/applications/), set up a new application, make it a bot, then copy the token.

`ChannelId`: The ID of the channel that you want the bot to be used in. The bot will only respond to commands in this channel.

`OwnerId`: The ID of the owner of this bot (that's you!). Used for certain commands that only the bot's owner can run.

`CommandPrefix`: The prefix for the commands. Fairly simple.

`UserToStickyRoles`: A dictionary of member ID to list of role IDs, which will be automatically re-applied. Should follow the JSON dictionary format.
Example: To auto-apply role with ID 353984776799125536 to member 214060918219341824 , and roles with IDs 353984776799125536 and 611961013620834314 to member 317621741352648714, the configuration would be
```json
"UserToStickyRoles":
{
  "214060918219341824":[353984776799125536],
  "317621741352648714":[353984776799125536, 611961013620834314]
}
```
(the newlines and spaces are optional, but it makes it more readable)
