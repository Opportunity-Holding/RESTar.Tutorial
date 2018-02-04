using System;
using System.Collections.Generic;
using System.Linq;
using RESTar.Linq;
using RESTar.SQLite;
using static RESTar.Methods;

namespace RESTarTutorial
{
    using RESTar;
    using Starcounter;

    /// <summary>
    /// A simple RESTar application
    /// </summary>
    public class TutorialApp
    {
        public static void Main()
        {
            Db.Transact(() => Db.SQL<Superhero>("SELECT t FROM RESTarTutorial.Superhero t").ForEach(Db.Delete));
            var workingDirectory = Application.Current.WorkingDirectory;
            var sqliteProvider = new SQLiteProvider(workingDirectory, "data");
            // SQLite is used here to populate the Superhero table with data

            RESTarConfig.Init
            (
                port: 8282,
                uri: "/myservice",
                requireApiKey: true,
                configFilePath: workingDirectory + "/Config.xml",
                resourceProviders: new[] {sqliteProvider}
            );
            // The 'port' argument sets the HTTP port on which to register the REST handlers
            // The 'uri' argument sets the root uri of the REST API
            // The 'requireApiKey' parameter is set to 'true'. API keys are required in all incoming requests.
            // The 'configFilePath' points towards the configuration file, which contains API keys. In this case,
            //   this file is located in the project folder.

            SuperheroSQLite.LoadDemoDatabase();
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
                    NumberOfSuperheroes = Db
                        .SQL<long>("SELECT COUNT(t) FROM RESTarTutorial.Superhero t")
                        .FirstOrDefault(),
                    FirstSuperheroInserted = superHeroesOrdered.FirstOrDefault(),
                    LastSuperheroInserted = superHeroesOrdered.LastOrDefault(),
                }
            };
        }
    }
}