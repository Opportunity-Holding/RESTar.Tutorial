using RESTar;
using RESTar.Linq;
using RESTar.SQLite;
using Starcounter;

namespace RESTarTutorial
{
    /// <summary>
    /// This class defines the mapping from an included SQLite 3 database containing 
    /// the data that is to be populated to the Superhero Starcounter tabke. This 
    /// resource cannot be queried directly in this application. See the RESTar.SQLite 
    /// NuGet package for more information about how to integrate SQLite with RESTar.
    /// 
    /// Database is a subset of https://github.com/fivethirtyeight/data/tree/master/comic-characters
    /// - which is, in turn, taken from Marvel and DC Comics respective sites.
    /// </summary>
    [SQLite(CustomTableName = "Heroes"), RESTar(Methods.GET)]
    public class SuperheroSQLite : SQLiteTable
    {
        [Column] public string Name { get; set; }
        [Column] public string Id { get; set; }
        [Column] public string Sex { get; set; }
        [Column] public int Year { get; set; }

        public static void LoadDemoDatabase() => new Request<SuperheroSQLite>()
            .WithConditions(nameof(Year), Operators.NOT_EQUALS, null)
            .GET()
            .ForEach(hero => Db.Transact(() => new Superhero
            {
                HasSecretIdentity = hero.Id == "Secret Identity",
                Name = hero.Name,
                Gender = hero.Sex == "Male Characters"
                    ? "Male"
                    : hero.Sex == "Female Characters"
                        ? "Female"
                        : "Other",
                YearIntroduced = hero.Year != 0 ? hero.Year : default(int?)
            }));
    }
}