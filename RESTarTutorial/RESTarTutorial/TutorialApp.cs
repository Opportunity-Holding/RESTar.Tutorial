using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using Newtonsoft.Json.Linq;
using RESTar;
using Starcounter;
using RESTar.Linq;
using RESTar.Requests;
using RESTar.Resources;
using RESTar.Resources.Operations;
using RESTar.SQLite;
using RESTar.WebSockets;
using static System.StringComparison;
using static RESTar.Method;

// ReSharper disable InheritdocConsiderUsage

namespace RESTarTutorial
{
    #region Tutorial 1

    /// <summary>
    /// A simple RESTar application
    /// </summary>
    public class TutorialApp
    {
        public static void Main()
        {
            var projectFolder = Application.Current.WorkingDirectory;
            RESTarConfig.Init
            (
                port: 8282,
                uri: "/api",
                requireApiKey: true,
                configFilePath: projectFolder + "/Config.xml",
                entityResourceProviders: new[] {new SQLiteProvider($"{projectFolder}\\data.sqlite")}
            );

            // The 'port' argument sets the HTTP port on which to register the REST handlers
            // The 'uri' argument sets the root uri of the REST API
            // The 'requireApiKey' parameter is set to 'true'. API keys are required in all incoming requests.
            // The 'configFilePath' points towards the configuration file, which contains API keys. In this case,
            //   this file is located in the project folder.
            // The 'resourceProviders' parameter is used for SQLite integration (see the ExampleDatabase class below)

            ExampleDatabase.Setup();
        }
    }

    [Database, RESTar(GET, POST, PUT, PATCH, DELETE)]
    public class Superhero
    {
        public string Name { get; set; }
        public bool HasSecretIdentity { get; set; }
        public string Gender { get; set; }
        public int? YearIntroduced { get; set; }
        public DateTime InsertedAt { get; }
        public Superhero() => InsertedAt = DateTime.Now;
    }

    [RESTar(GET)]
    public class SuperheroReport : ISelector<SuperheroReport>
    {
        public long NumberOfSuperheroes { get; private set; }
        public Superhero FirstSuperheroInserted { get; private set; }
        public Superhero LastSuperheroInserted { get; private set; }

        /// <summary>
        /// This method returns an IEnumerable of the resource type. RESTar will call this
        /// on GET requests and send the results back to the client as e.g. JSON.
        /// </summary>
        public IEnumerable<SuperheroReport> Select(IRequest<SuperheroReport> request)
        {
            var superHeroesOrdered = Db
                .SQL<Superhero>("SELECT t FROM RESTarTutorial.Superhero t")
                .OrderBy(h => h.InsertedAt)
                .ToList();
            return new[]
            {
                new SuperheroReport
                {
                    NumberOfSuperheroes = superHeroesOrdered.Count,
                    FirstSuperheroInserted = superHeroesOrdered.FirstOrDefault(),
                    LastSuperheroInserted = superHeroesOrdered.LastOrDefault()
                }
            };
        }
    }

    #endregion

    #region Tutorial 2

    /// <summary>
    /// RESTar will generate an instance of this class when a client makes a GET request to /chatbot
    /// with a WebSocket handshake.
    /// </summary>
    [RESTar]
    public class Chatbot : ITerminal
    {
        /// <summary>
        /// Each time this class is instantiated, an IWebSocket instance will be assigned to the
        /// WebSocket property. This object holds the WebSocket connection to the connected client.
        /// We can, for example, send text to the client by making a call to WebSocket.SendText().
        /// </summary>
        public IWebSocket WebSocket { private get; set; }

        /// <summary>
        /// This method is called when the WebSocket is opened towards this Chatbot instance. A perfect
        /// time to send a welcome message.
        /// </summary>
        public void Open() => WebSocket.SendText(
            "> Hi, I'm a chatbot! Type anything, and I'll try my best to answer. I like to tell jokes... " +
            "(type QUIT to return to the shell)"
        );

        /// <summary>
        /// Here we inform RESTar that instances of Chatbot can handle text input
        /// </summary>
        public bool SupportsTextInput { get; } = true;

        /// <summary>
        /// ... but not binary input
        /// </summary>
        public bool SupportsBinaryInput { get; } = false;

        /// <summary>
        /// This method defines the logic that is run when an incoming text message is received over the
        /// WebSocket that is assigned to this terminal.
        /// </summary>
        public void HandleTextInput(string input)
        {
            if (string.Equals(input, "quit", OrdinalIgnoreCase))
            {
                WebSocket.DirectToShell();
                return;
            }

            var response = GetChatbotResponse(input);
            WebSocket.SendText(response);
        }

        internal static string GetChatbotResponse(string input)
        {
            return ChatbotAPI.GetResponse(input).result?.fulfillment?.speech ?? "I have no response to that. Sorry...";
        }

        /// <summary>
        /// We still need to implement this method, but it is never called, since SupportsBinaryInput is
        /// set to false.
        /// </summary>
        public void HandleBinaryInput(byte[] input) => throw new NotImplementedException();

        #region DialogFlow API

        /// <summary>
        /// A simple API for a pre-defined DialogFlow chatbot.
        /// </summary>
        private static class ChatbotAPI
        {
            private const string AccessToken = "6d7be132f63e48bab18531ec41364673";

            private static readonly AuthenticationHeaderValue Authorization =
                new AuthenticationHeaderValue("Bearer", AccessToken);

            private static readonly HttpClient HttpClient = new HttpClient();
            private static readonly string SessionId = Guid.NewGuid().ToString();

            /// <summary>
            /// Sends the input to the chatbot service API, and returns the text response
            /// </summary>
            internal static dynamic GetResponse(string input)
            {
                var uri = $"https://api.dialogflow.com/v1/query?v=20170712&query={WebUtility.UrlEncode(input)}" +
                          $"&lang=en&sessionId={SessionId}&timezone={TimeZone.CurrentTimeZone}";
                var message = new HttpRequestMessage(HttpMethod.Get, uri) {Headers = {Authorization = Authorization}};
                var response = HttpClient.SendAsync(message).Result.Content.ReadAsStringAsync().Result;
                return JObject.Parse(response);
            }
        }

        #endregion

        /// <summary>
        /// If the terminal resource has additional resources tied to an instance, this is were we release
        /// them.
        /// </summary>
        public void Dispose() { }
    }

    /// <summary>
    /// "ChatRoom" is an appropriate name for the resource from the client's perspective, even though
    /// each instance of this resource will work more like a chat participant.
    /// </summary>
    [RESTar]
    public class ChatRoom : ITerminal
    {
        /// <summary>
        /// This collection holds all ChatRoom instances
        /// </summary>
        private static readonly TerminalSet<ChatRoom> Terminals = new TerminalSet<ChatRoom>();

        private string _name;

        /// <summary>
        /// The name of the connected chat room participant. To change this, we can write
        /// #terminal {"Name": "new name"} while in the chat room.
        /// </summary>
        public string Name
        {
            get => _name;
            set
            {
                var name = GetUniqueName(value);
                if (Initiated)
                    SendToAll($"# {_name} has changed name to \"{name}\"");
                _name = name;
            }
        }

        /// <summary>
        /// A read-only list of all chat room participants (names).
        /// </summary>
        public string[] Members => Terminals.Select(t => t.Name).ToArray();

        /// <summary>
        /// The number of connected participants.
        /// </summary>
        public int NumberOfMembers => Terminals.Count;

        /// <summary>
        /// Used internally to track if the participant is initiated. Invisible in the API.
        /// </summary>
        private bool Initiated;

        public void Open()
        {
            Name = GetUniqueName(Name);
            SendToAll($"# {Name} has joined the chat room.");
            Terminals.Add(this);
            WebSocket.SendText(
                $"# Welcome to the chat room! Your name is \"{Name}\" (type QUIT to return to the shell)");
            Initiated = true;
        }

        /// <summary>
        /// Creates a unique name for a participant, or deal with edge cases like a participant naming
        /// themselves nothing or "Chatbot".
        /// </summary>
        private static string GetUniqueName(string Name)
        {
            if (string.IsNullOrWhiteSpace(Name) || string.Equals(Name, "chatbot", OrdinalIgnoreCase))
                Name = "Chatter";
            if (!Terminals.Any(c => string.Equals(c.Name, Name, OrdinalIgnoreCase)))
                return Name;
            var modifier = 2;
            var tempName = $"{Name} {modifier}";
            while (Terminals.Any(c => string.Equals(c.Name, tempName, OrdinalIgnoreCase)))
                tempName = $"{Name} {modifier++}";
            return tempName;
        }

        public void Dispose()
        {
            Terminals.Remove(this);
            SendToAll($"# {Name} left the chat room.");
        }

        public void HandleTextInput(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return;
            if (string.Equals(input, "quit", OrdinalIgnoreCase))
            {
                WebSocket.DirectToShell();
                return;
            }

            SendToAll($"> {Name}: {input}");
            if (input.Length > 5 && input.StartsWith("@bot ", OrdinalIgnoreCase))
            {
                var message = input.Split("@bot ")[1];
                var response = Chatbot.GetChatbotResponse(message);
                SendToAll($"> Chatbot: {response}");
            }
        }

        private static void SendToAll(string message)
        {
            foreach (var terminal in Terminals)
                terminal.WebSocket.SendText(message);
        }

        public IWebSocket WebSocket { private get; set; }
        public bool SupportsTextInput { get; } = true;
        public bool SupportsBinaryInput { get; } = false;
        public void HandleBinaryInput(byte[] input) => throw new NotImplementedException();
    }

    #endregion

    #region Demo database

    /// <summary>
    /// Database is a subset of https://github.com/fivethirtyeight/data/tree/master/comic-characters
    /// - which is, in turn, taken from Marvel and DC Comics respective sites.
    /// </summary>
    internal static class ExampleDatabase
    {
        internal static void Setup()
        {
            // First we delete all Superheroes from the database. Then we get the content from an included SQLite
            // database and build the Starcounter database from it. For more information on how to integrate SQLite
            // with RESTar, see the 'RESTar.SQLite' package on NuGet.

            Db.Transact(() => Db
                .SQL<Superhero>("SELECT t FROM RESTarTutorial.Superhero t")
                .ForEach(Db.Delete));

            // The Root context has access to all resources, and can only be used internally
            using (var request = Context.Root.CreateRequest<SuperheroSQLite>())
            {
                request.Conditions.Add("Year", Operators.NOT_EQUALS, null);
                using (var superheroes = request.EvaluateToEntities())
                {
                    foreach (var hero in superheroes)
                    {
                        // Create a Starcounter Superhero instance from a SQLite row
                        Db.TransactAsync(() => new Superhero
                        {
                            Name = hero.Name,
                            YearIntroduced = hero.Year != 0 ? hero.Year : default(int?),
                            HasSecretIdentity = hero.Id == "Secret Identity",
                            Gender = hero.Sex == "Male Characters" ? "Male" : hero.Sex == "Female Characters" ? "Female" : "Other",
                        });
                    }
                }
            }
        }
    }

    [SQLite(CustomTableName = "Heroes"), RESTarInternal(GET)]
    public class SuperheroSQLite : SQLiteTable
    {
        public string Name { get; set; }
        public string Id { get; set; }
        public string Sex { get; set; }
        public int Year { get; set; }
    }

    #endregion
}