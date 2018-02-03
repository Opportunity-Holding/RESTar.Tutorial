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

    public class Program
    {
        public static void Main()
        {
            SuperHero.ClearDemoDatabase();

            var workingDirectory = Application.Current.WorkingDirectory;
            var sqliteProvider = new SQLiteProvider(workingDirectory, "data");
            // SQLite is used to populate the SuperHero table with data

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

            SuperHeroSQLite.LoadDemoDatabase();
        }
    }

    [Database, RESTar(GET, POST, PUT, PATCH, DELETE)]
    public class SuperHero
    {
        public string Title { get; set; }
        public string Gender { get; set; }
        public string Height { get; set; }
        public string Weight { get; set; }
        public string Occupation { get; set; }
        public DateTime InsertedAt { get; }
        public SuperHero() => InsertedAt = DateTime.Now;

        internal static void ClearDemoDatabase() => Db.Transact(() => Db
            .SQL<SuperHero>("SELECT t FROM RESTarTutorial.SuperHero t")
            .ForEach(Db.Delete));
    }

    [RESTar(GET)]
    public class SuperHeroReport : ISelector<SuperHeroReport>
    {
        public long NumberOfSuperHeroes { get; private set; }
        public SuperHero FirstSuperHeroInserted { get; private set; }
        public SuperHero LastSuperHeroInserted { get; private set; }

        public IEnumerable<SuperHeroReport> Select(IRequest<SuperHeroReport> request)
        {
            var superHeroesOrdered = Db
                .SQL<SuperHero>("SELECT t FROM RESTarTutorial.SuperHero t ORDER BY InsertedAt")
                .ToList();
            return new[]
            {
                new SuperHeroReport
                {
                    NumberOfSuperHeroes = Db
                        .SQL<long>("SELECT COUNT(t) FROM RESTarTutorial.SuperHero t")
                        .FirstOrDefault(),
                    FirstSuperHeroInserted = superHeroesOrdered.FirstOrDefault(),
                    LastSuperHeroInserted = superHeroesOrdered.LastOrDefault(),
                }
            };
        }
    }
}