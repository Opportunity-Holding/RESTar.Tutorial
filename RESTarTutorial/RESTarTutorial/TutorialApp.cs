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

    public class TutorialApp
    {
        public static void Main()
        {
            Superhero.ClearDemoDatabase();
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
            // The 'port' argument decides which HTTP port to register the REST handlers on
            // The 'uri' argument sets the root uri of the REST API
            // The 'requireApiKey' parameter is set to 'true'. API keys are required in all incoming requests
            // The 'configFilePath' points towards the configuration file, which includes API keys

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

        internal static void ClearDemoDatabase() => Db.Transact(() => Db
            .SQL<Superhero>("SELECT t FROM RESTarTutorial.Superhero t")
            .ForEach(Db.Delete));
    }

    [RESTar(GET)]
    public class SuperheroReport : ISelector<SuperheroReport>
    {
        public long NumberOfSuperheroes { get; private set; }
        public Superhero FirstSuperheroInserted { get; private set; }
        public Superhero LastSuperheroInserted { get; private set; }

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