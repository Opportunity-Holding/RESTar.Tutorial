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
            var projectFolder = Application.Current.WorkingDirectory;
            RESTarConfig.Init
            (
                port: 8282,
                uri: "/api",
                requireApiKey: true,
                configFilePath: projectFolder + "/Config.xml",
                resourceProviders: new[] {new SQLiteProvider(projectFolder, "data")}
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
            new Request<SuperheroSQLite>()
                .WithConditions("Year", Operators.NOT_EQUALS, null)
                .GET()
                .ForEach(hero => Db.Transact(() => new Superhero
                {
                    Name = hero.Name,
                    YearIntroduced = hero.Year != 0 ? hero.Year : default(int?),
                    HasSecretIdentity = hero.Id == "Secret Identity",
                    Gender = hero.Sex == "Male Characters" ? "Male" : hero.Sex == "Female Characters" ? "Female" : "Other",
                }));
        }
    }

    [SQLite(CustomTableName = "Heroes"), RESTarInternal(GET)]
    public class SuperheroSQLite : SQLiteTable
    {
        [Column] public string Name { get; set; }
        [Column] public string Id { get; set; }
        [Column] public string Sex { get; set; }
        [Column] public int Year { get; set; }
    }

    #endregion
}