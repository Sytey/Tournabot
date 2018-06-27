﻿using Discord;
using Discord.WebSocket;
using System;
using System.Threading.Tasks;
using Discord.Commands;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using MySql.Data.MySqlClient;

namespace Tournabot
{
    class Program
    {
        private DiscordSocketClient client;
        private CommandService commands;
        private IServiceProvider services;

        public static void Main(string[] args)
            => new Program().MainAsync().GetAwaiter().GetResult();

        public async Task MainAsync()
        {
            client = new DiscordSocketClient();
            client.MessageReceived += HandleCommand;
            client.Log += Log;
            commands = new CommandService();

            services = new ServiceCollection()
                .AddSingleton(this)
                .AddSingleton(client)
                .AddSingleton(commands)
                .AddSingleton<ConfigHandler>()
                .BuildServiceProvider();

            await services.GetService<ConfigHandler>().PopulateConfig();

            await commands.AddModulesAsync(Assembly.GetEntryAssembly(), services);

            await client.LoginAsync(TokenType.Bot, services.GetService<ConfigHandler>().GetToken());
            await client.StartAsync();

            // Block this task until the program is closed.
            await Task.Delay(-1);
        }

        private Task Log(LogMessage msg)
        {
            Console.WriteLine(msg.ToString());
            return Task.CompletedTask;
        }

        public async Task HandleCommand(SocketMessage messageParam)
        {
            var message = messageParam as SocketUserMessage;
            if (message == null) return;
            int argPos = 0;
            if (!(message.HasCharPrefix('.', ref argPos) || message.HasMentionPrefix(client.CurrentUser, ref argPos))) return;
            var context = new SocketCommandContext(client, message);
            var result = await commands.ExecuteAsync(context, argPos, services);
            if (!result.IsSuccess)
            {
                await context.Channel.SendMessageAsync(result.ErrorReason);
            }
        }

        private async void MysqlConnect()
        {
            MySqlConnection conn = null;
            MySqlDataReader rdr = null;
            string myConnectionString = "server=localhost;uid=root;pwd=root;database=boys;";
            try
            {
                conn = new MySqlConnection();
                conn.ConnectionString = myConnectionString;
                conn.Open();
                string stm = "SELECT * FROM test";
                MySqlCommand cmd = new MySqlCommand(stm, conn);
                rdr = cmd.ExecuteReader();

                foreach (SocketGuild serv in client.Guilds)
                {
                    await serv.DefaultChannel.SendMessageAsync("Connected to Boy Database version : " + conn.ServerVersion);
                }
            }
            catch (MySqlException ex)
            {
                foreach (SocketGuild serv in client.Guilds)
                {
                    await serv.DefaultChannel.SendMessageAsync(ex.Message);
                }
            }
            finally
            {
                if (rdr != null)
                {
                    rdr.Close();
                }
                if (conn != null)
                {
                    conn.Close();
                }
            }
        }
    }
}
