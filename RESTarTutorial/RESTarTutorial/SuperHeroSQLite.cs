using RESTar;
using RESTar.Linq;
using RESTar.SQLite;
using Starcounter;

namespace RESTarTutorial
{
    /// <summary>
    /// This class outlines the O/RM mapping from an included SQLite 3 database containing 
    /// the data that is to be populated to Starcounter. This resource cannot be queried 
    /// directly in this application. See the RESTar.SQLite NuGet package for more information 
    /// about how to integrate SQLite with RESTar.
    /// 
    /// Database taken from https://github.com/RGamberini/Superhero-Database
    /// </summary>
    [SQLite(CustomTableName = "Heros"), RESTar(Methods.GET)]
    public class SuperHeroSQLite : SQLiteTable
    {
        [Column] public string Title { get; set; }
        [Column] public string Gender { get; set; }
        [Column] public string Height { get; set; }
        [Column] public string Weight { get; set; }
        [Column] public string Occupation { get; set; }

        public static void LoadDemoDatabase() => new Request<SuperHeroSQLite>()
            .GET()
            .ForEach(hero => Db.Transact(() => new SuperHero
            {
                Title = hero.Title,
                Gender = hero.Gender,
                Height = hero.Height,
                Weight = hero.Weight,
                Occupation = hero.Occupation
            }));
    }
}