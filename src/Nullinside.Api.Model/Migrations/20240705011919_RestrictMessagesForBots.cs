﻿using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nullinside.Api.Model.Migrations
{
    /// <inheritdoc />
    public partial class RestrictMessagesForBots : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
          migrationBuilder.Sql("""
                               ALTER VIEW nullinside.BansWithMessagesInChat AS
                               SELECT DISTINCT(OuterC.Id), OuterC.Channel, OuterC.TwitchUsername, OuterC.Message, OuterC.`Timestamp`
                               FROM nullinside.TwitchUserChatLogs as OuterC
                               JOIN (
                               	SELECT c.Id, c.Channel, c.TwitchId, c.TwitchUsername, c.Reason, c.`Timestamp`
                               	FROM nullinside.TwitchUserBannedOutsideOfBotLogs as c
                               	JOIN (
                               		SELECT Channel, TwitchUsername, COUNT(1) AS MessageCount
                               		FROM nullinside.TwitchUserChatLogs
                               		GROUP BY Channel, TwitchUsername
                               		HAVING MessageCount <= 3
                               	) AS Logs ON c.Channel = Logs.Channel AND c.TwitchUsername = Logs.TwitchUsername
                               ) AS OuterLogs ON OuterC.Channel = OuterLogs.Channel AND OuterC.TwitchUsername = OuterLogs.TwitchUsername;
                               """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
          migrationBuilder.Sql("""
                               ALTER VIEW nullinside.BansWithMessagesInChat AS
                               SELECT DISTINCT(OuterC.Id), OuterC.Channel, OuterC.TwitchUsername, OuterC.Message, OuterC.`Timestamp`
                               FROM nullinside.TwitchUserChatLogs as OuterC
                               JOIN (
                               	SELECT c.Id, c.Channel, c.TwitchId, c.TwitchUsername, c.Reason, c.`Timestamp`
                               	FROM nullinside.TwitchUserBannedOutsideOfBotLogs as c
                               	JOIN (
                               		SELECT Channel, TwitchUsername, COUNT(1) AS MessageCount
                               		FROM nullinside.TwitchUserChatLogs
                               		GROUP BY Channel, TwitchUsername
                               	) AS Logs ON c.Channel = Logs.Channel AND c.TwitchUsername = Logs.TwitchUsername
                               ) AS OuterLogs ON OuterC.Channel = OuterLogs.Channel AND OuterC.TwitchUsername = OuterLogs.TwitchUsername;
                               """);
        }
    }
}
