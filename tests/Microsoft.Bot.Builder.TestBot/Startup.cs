﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.AI.Luis;
using Microsoft.Bot.Builder.BotFramework;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.BotBuilderSamples.CognitiveModels;
using Microsoft.BotBuilderSamples.Dialogs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.BotBuilderSamples
{
    public class Startup
    {
        public Startup(IHostingEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
                .AddEnvironmentVariables();

            Configuration = builder.Build();
        }

        /// <summary>
        /// Gets the configuration that represents a set of key/value application configuration properties.
        /// </summary>
        /// <value>
        /// The <see cref="IConfiguration"/> that represents a set of key/value application configuration properties.
        /// </value>
        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_1);

            // Create the credential provider to be used with the Bot Framework Adapter.
            services.AddSingleton<ICredentialProvider, ConfigurationCredentialProvider>();

            // Create the debug middleware
            services.AddSingleton<InspectionMiddleware>();

            // Create the Bot Framework Adapter with error handling enabled.
            services.AddSingleton<IBotFrameworkHttpAdapter, AdapterWithErrorHandler>();

            // Create the storage we'll be using for User and Conversation state. (Memory is great for testing purposes.)
            services.AddSingleton<IStorage, MemoryStorage>();

            // Create the App state. (Used by the DebugMiddleware.)
            services.AddSingleton<InspectionState>();

            // Create the User state. (Used in this bot's Dialog implementation.)
            services.AddSingleton<UserState>();

            // Create the Conversation state. (Used by the Dialog system itself.)
            services.AddSingleton<ConversationState>();

            // Register intents and dialog mapping
            var intentAndDialogsMap = new IntentDialogMap
            {
                { FlightBooking.Intent.BookFlight, new BookingDialog() },
            };
            services.AddSingleton(intentAndDialogsMap);

            // Register LUIS recognizer
            var luisApplication = new LuisApplication(
                Configuration["LuisAppId"],
                Configuration["LuisAPIKey"],
                "https://" + Configuration["LuisAPIHostName"]);

            var recognizer = new LuisRecognizer(luisApplication, null, false, null);
            services.AddSingleton(recognizer);

            // The Dialog that will be run by the bot.
            services.AddSingleton<MainDialog>();

            // Create the bot as a transient. In this case the ASP Controller is expecting an IBot.
            services.AddTransient<IBot, DialogAndWelcomeBot<MainDialog>>();

            // We can also run the inspection at a different endpoint. Just uncomment these lines.
            // services.AddSingleton<DebugAdapter>();
            // services.AddTransient<DebugBot>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseHsts();
            }

            app.UseDefaultFiles();
            app.UseStaticFiles();

            // app.UseHttpsRedirection();
            app.UseMvc();
        }
    }
}
