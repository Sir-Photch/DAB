// This file is used by Code Analysis to maintain SuppressMessage
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given
// a specific target and scoped to a namespace, type, member, etc.

using System.Diagnostics.CodeAnalysis;

#if DEBUG
[assembly: SuppressMessage("Performance", "HAA0101:Array allocation for params parameter", Justification = "<Ausstehend>", Scope = "member", Target = "~M:DAB.Discord.DiscordAnnouncementService.OnClientReadyAsync~System.Threading.Tasks.Task")]
[assembly: SuppressMessage("Performance", "HAA0101:Array allocation for params parameter", Justification = "<Ausstehend>", Scope = "member", Target = "~M:DAB.Data.Sinks.FileSystemSink.#ctor(System.String)")]
[assembly: SuppressMessage("Performance", "HAA0102:Non-overridden virtual method call on value type", Justification = "<Ausstehend>", Scope = "member", Target = "~M:DAB.Discord.Commands.SlashCommandFactory.ApplySlashCommandDetails(Discord.SlashCommandBuilder,DAB.Discord.Enums.SlashCommandType)~Discord.SlashCommandBuilder")]
[assembly: SuppressMessage("Performance", "HAA0302:Display class allocation to capture closure", Justification = "<Ausstehend>", Scope = "member", Target = "~M:DAB.Discord.Commands.SlashCommandFactory.FromString(System.String)~System.Nullable{DAB.Discord.Enums.SlashCommandType}")]
[assembly: SuppressMessage("Performance", "HAA0301:Closure Allocation Source", Justification = "<Ausstehend>", Scope = "member", Target = "~M:DAB.Discord.Commands.SlashCommandFactory.FromString(System.String)~System.Nullable{DAB.Discord.Enums.SlashCommandType}")]
[assembly: SuppressMessage("Performance", "HAA0302:Display class allocation to capture closure", Justification = "<Ausstehend>", Scope = "member", Target = "~M:DAB.Discord.Commands.SlashCommandFactory.OverwriteCommandsAsync(Discord.WebSocket.DiscordSocketClient)~System.Threading.Tasks.Task")]
[assembly: SuppressMessage("Performance", "HAA0301:Closure Allocation Source", Justification = "<Ausstehend>", Scope = "member", Target = "~M:DAB.Discord.Commands.SlashCommandFactory.OverwriteCommandsAsync(Discord.WebSocket.DiscordSocketClient)~System.Threading.Tasks.Task")]
[assembly: SuppressMessage("Performance", "HAA0302:Display class allocation to capture closure", Justification = "<Ausstehend>", Scope = "member", Target = "~M:DAB.Discord.HandlerModules.AnnouncementHandlerModule.SetChimeAsync~System.Threading.Tasks.Task")]
[assembly: SuppressMessage("Performance", "HAA0601:Value type to reference type conversion causing boxing allocation", Justification = "<Ausstehend>", Scope = "member", Target = "~M:DAB.Discord.HandlerModules.AnnouncementHandlerModule.SetChimeAsync~System.Threading.Tasks.Task")]
[assembly: SuppressMessage("Performance", "HAA0101:Array allocation for params parameter", Justification = "<Ausstehend>", Scope = "member", Target = "~M:DAB.Discord.HandlerModules.AnnouncementHandlerModule.SetChimeAsync~System.Threading.Tasks.Task")]
[assembly: SuppressMessage("Performance", "HAA0301:Closure Allocation Source", Justification = "<Ausstehend>", Scope = "member", Target = "~M:DAB.Discord.HandlerModules.AnnouncementHandlerModule.SetChimeAsync~System.Threading.Tasks.Task")]
[assembly: SuppressMessage("Performance", "HAA0101:Array allocation for params parameter", Justification = "<Ausstehend>", Scope = "member", Target = "~M:DAB.Discord.HandlerModules.AnnouncementHandlerModule.LoadAudioFileAsync(Discord.IAttachment,Discord.ISlashCommandInteraction)~System.Threading.Tasks.Task")]
[assembly: SuppressMessage("Performance", "HAA0302:Display class allocation to capture closure", Justification = "<Ausstehend>", Scope = "member", Target = "~M:DAB.Discord.DiscordAnnouncementService.OnSlashCommandExecutedAsync(Discord.WebSocket.SocketSlashCommand)~System.Threading.Tasks.Task")]
[assembly: SuppressMessage("Performance", "HAA0301:Closure Allocation Source", Justification = "<Ausstehend>", Scope = "member", Target = "~M:DAB.Discord.DiscordAnnouncementService.OnSlashCommandExecutedAsync(Discord.WebSocket.SocketSlashCommand)~System.Threading.Tasks.Task")]
[assembly: SuppressMessage("Performance", "HAA0101:Array allocation for params parameter", Justification = "<Ausstehend>", Scope = "member", Target = "~M:DAB.Discord.DiscordAnnouncementService.OnSlashCommandExecutedAsync(Discord.WebSocket.SocketSlashCommand)~System.Threading.Tasks.Task")]
[assembly: SuppressMessage("Performance", "HAA0101:Array allocation for params parameter", Justification = "<Ausstehend>")]
[assembly: SuppressMessage("Performance", "HAA0601:Value type to reference type conversion causing boxing allocation", Justification = "<Ausstehend>", Scope = "member", Target = "~M:DAB.Discord.HandlerModules.AnnouncementHandlerModule.LoadAudioFileAsync(Discord.IAttachment,Discord.ISlashCommandInteraction)~System.Threading.Tasks.Task")]
[assembly: SuppressMessage("Performance", "HAA0601:Value type to reference type conversion causing boxing allocation", Justification = "<Ausstehend>")]
#endif