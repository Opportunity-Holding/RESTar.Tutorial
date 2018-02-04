# Tutorial
RESTar is a powerful REST API Framework for Starcounter applications, that is free to use and easy to set up in new or existing applications. Using RESTar will give your applications all sorts of REST super powers, with minimal effort. This tutorial will give a hands-on introduction to RESTar, and how to use it in a simple Starcounter application. For more information, please se the complete [RESTar Specification](https://goo.gl/TIkN7m), which outlines all the features of RESTar.

## Getting started
To get started, install RESTar from NuGet, either by browsing for `RESTar` in the **NuGet Package Manager** or by running the following command in the **Package Manager Console**:

```Install-Package RESTar```

All we need to do then, to enable RESTar in a given application, is to make a call to `RESTar.RESTarConfig.Init()` somewhere in the application code, preferably where it's called once every time the app starts. `Init()` will register the necessary handlers, collect all resources and make them available over a REST API. Here is a simple RESTar application:

```c#
namespace RESTarTutorial
{
    using RESTar;
    public class TutorialApp
    {
        public static void Main()
        {
            RESTarConfig.Init(port: 8282, uri: "/myservice");
            // The 'port' argument decides which HTTP port to register the REST handlers on
            // The 'uri' argument sets the root uri of the REST API
        }
    }
}
```
The application above is not very useful, however, since it doesn't really expose any app data through the REST API. Let's change that. RESTar can take a Starcounter database class and make its content available as a resource in the REST API. To show RESTar which classes to expose, we simply decorate their definitions with the `RESTarAttribute` attribute. Like this:

```c#
namespace RESTarTutorial
{
    using RESTar;
    using Starcounter;
    using static RESTar.Methods;

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
}
```
RESTar will find the `Superhero` database class and register it as available over the REST API. This means that REST clients can send `GET`, `POST`, `PUT`, `PATCH` and `DELETE` requests to `<host>:8282/myservice/superhero` and interact with its content. To make a different set of methods available for a resource, we simply include a different set of methods in the `RESTarAttribute` constructor. RESTar has two supported content types, **JSON** and **Excel**, so the bodies contained within these requests can be of either of these formats. Now let's make a couple of simple local `POST` requests to this API with JSON data (using cURL syntax) (or [Postman](https://github.com/Mopedo/RESTar.Tutorial/blob/master/RESTarTutorial/Postman_data_post.jpg)):

```
curl 'localhost:8282/myservice/superhero' -d '{
    "Name": "Batman (Bruce Wayne)",
    "HasSecretIdentity": true,
    "Gender": "Male",
    "YearIntroduced": 1939
}'
curl 'localhost:8282/myservice/superhero' -d '{
    "Name": "Superman (Clark Kent)",
    "HasSecretIdentity": true,
    "Gender": "Male",
    "YearIntroduced": 1986
}' 
```
And now, let's retrieve this data using a `GET` request ([Postman](https://github.com/Mopedo/RESTar.Tutorial/blob/master/RESTarTutorial/Postman_data_get.jpg)):
```
curl 'localhost:8282/myservice/superhero//limit=2'
Output:
[{
    "Name": "Batman (Bruce Wayne)",
    "HasSecretIdentity": true,
    "Gender": "Male",
    "YearIntroduced": 1939,
    "InsertedAt": "2018-02-04T14:41:50.9009688Z",
    "ObjectNo": 103464
},
{
    "Name": "Aquaman (Arthur Curry)",
    "HasSecretIdentity": false,
    "Gender": "Male",
    "YearIntroduced": 1941,
    "InsertedAt": "2018-02-04T14:41:50.9633607Z",
    "ObjectNo": 103468
}]
```
## Exploring the parameters of `RESTarConfig.Init()`

The `RESTar.RESTarConfig.Init()` method has more parameters than the ones we used above. This is the complete signature:
```c#
static void Init
(
    ushort port = 8282, 
    string uri = "/rest",
    bool viewEnabled = false,
    bool setupMenu = false,
    bool requireApiKey = false,
    bool allowAllOrigins = true,
    string configFilePath = null,
    bool prettyPrint = true,
    ushort daysToSaveErrors = 30,
    LineEndings lineEndings = LineEndings.Windows,
    IEnumerable<ResourceProvider> resourceProviders = null
);
```
For now, let's focus on `requireApiKey`, and `configFilePath`. These are used to control external access to the REST API. 

## Role-based authorization using API keys

In most use cases, we want to apply some form of role-based access control to the registered resources. Let's say only some clients should be allowed to insert and delete `Superhero` entities, while all should be able to read. To implement this, we create an XML file that will work as the configuration that RESTar reads API keys and access rights from. Let's create a new XML file in the project directory and call it "Config.xml" (the name and location can be different). Let's make its content look like this:

```xml
<?xml version="1.0" encoding="UTF-8"?>
<config>
  <ApiKey>
    <Key>a-secure-admin-key</Key>
    <AllowAccess>
      <Resource>RESTar.*</Resource>
      <Resource>RESTar.Admin.*</Resource>
      <Resource>RESTar.Dynamic.*</Resource>
      <Resource>RESTarTutorial.*</Resource>
      <Methods>*</Methods>
    </AllowAccess>
  </ApiKey>  
  <ApiKey>
    <Key>a-secure-user-key</Key>
    <AllowAccess>
      <Resource>RESTarTutorial.*</Resource>
      <Methods>GET</Methods>
    </AllowAccess>
  </ApiKey>
</config>
```
This configuration file specifies two api keys: `a-secure-admin-key` and `a-secure-user-key`. The first can perform all methods on all resources in the `RESTar`, `RESTar.Admin`, `RESTar.Dynamic` and `RESTarTutorial` namespaces, the latter which includes our `Superhero` resource. The second key, however, can only make `GET` calls to resources in the `RESTarTutorial` namespace. To enforce these access rights, we set the `requireApiKey` parameter to `true` in the call to `RESTarConfig.Init()` and provide the file path to the configuration file in the `configFilePath` parameter. Here is the same program as above, but now with role-based access control:

```c#
namespace RESTarTutorial
{
    using RESTar;
    using Starcounter;
    public class TutorialApp
    {
        public static void Main()
        {
            RESTarConfig.Init
            (
                port: 8282,
                uri: "/myservice",
                requireApiKey: true,
                configFilePath: Application.Current.WorkingDirectory + "/Config.xml"
            );
        }
    }
}
```
## Non-starcounter resources
In the example above, we saw a Starcounter database class working as a REST resource through RESTar. Starcounter database classes make for good examples, since most Starcounter developers are familiar with them, but RESTar itself is not limited to these classes. Any public non-static class can work as a RESTar resource class – as long as the developer can define the logic that is needed to support operations like `Select`, `Insert` and `Delete` that are used in REST requests. Say, for example, that we want a REST resource that is simply a transient aggregation of database data, that is generated when requested. To go with the example above, let's say we want a `SuperheroReport` class that we can make `GET` requests to ([Postman](https://github.com/Mopedo/RESTar.Tutorial/blob/master/RESTarTutorial/Postman_report_get.jpg)):
```
curl "localhost:8282/myservice/superheroreport" -H "Authorization: apikey a-secure-user-key"
Output:
[{
    "NumberOfSuperheroes": 167,
    "FirstSuperheroInserted": {
        "Name": "Batman (Bruce Wayne)",
        "HasSecretIdentity": true,
        "Gender": "Male",
        "YearIntroduced": 1939,
        "InsertedAt": "2018-02-04T01:16:27.6341808Z",
        "ObjectNo": 103295
    },
    "LastSuperheroInserted": {
        "Name": "Sersi (Earth-616)",
        "HasSecretIdentity": true,
        "Gender": "Female",
        "YearIntroduced": 1976,
        "InsertedAt": "2018-02-04T01:16:27.9386776Z",
        "ObjectNo": 103461
    }
}]
```
To implement `SuperheroReport`, just like we would with a database resource, we create a new .NET class, and assign the `RESTarAttribute` attribute to it. This time we only need `GET` to be enabled for the resource. Note that the class below is not a Starcounter database class.
```c#
namespace RESTarTutorial
{
    using RESTar;
    using Starcounter;
    using static RESTar.Methods;
    using System.Linq;
    using System.Collections.Generic;

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
```

To define or override the logic that is used when RESTar selects entities of a resource type, we implement the `RESTar.ISelector<T>` interface, and use the resource type as the type parameter `T`. Failure to provide the operations needed for the methods assigned in the `RESTarAttribute` constructor will result in a kind but resolute runtime exception. In the body of this `Select` method above, we provide logic for generating an `IEnumerable<SuperheroReport>` that is then returned to RESTar when evaluating `GET` requests.
 
## Request examples
OK, now we've seen the basics of what RESTar can do – and how to make data sources from a Starcounter application available over the REST API in a secure way. Next, let's look at some more advanced examples of how a client can consume a RESTar API. We will use the same application as above, and imagine that the database is now populated with `Superhero` entities. To try these requests yourself – clone this repository to your local machine and run the `RESTarTutorial` application. The application comes with an SQLite database that will automatically populate Starcounter with `Superhero` entities. If that sounded cool, you should check out [RESTar.SQLite](https://www.nuget.org/packages/RESTar.SQLite) on NuGet next.

**Notes**
For all requests below, API keys are included (if required) in the `Authorization` header. To include an API key, set the following as value to the `Authorization` header: `apikey <api key>` where `<api key>` is substituted with your actual API key. URIs are written as relative to the API root, e.g. `/myservice` as in the application above. When making requests to a local instance of the tutorial app, `/superhero` below should be understood as `localhost:8282/myservice/superhero`.

### GET requests

What it does | Request URI
--- | ---
Get all superheroes | `/superhero`
Get all female heroes | `/superhero/gender=Female`
Get all female heroes with secret identities | `/superhero/gender=Female&secretidentity=true`
Get the first five male heroes | `/superhero/gender=Male/limit=5`
Get male heroes from 6 through 10 | `/superhero/gender=Male/limit=5&offset=5`
Get female heroes, ordered by the year they were introduced | `/superhero/gender=Female/order_asc=yearintroduced`
Get male heroes, ordered by the length of their names | `/superhero/gender=Male/order_asc=name.length`
Get all male heroes' names  | `/superhero/gender=Male/select=name`
Get all years when a superhero was introduced  | `/superhero//select=yearintroduced&distinct=true`
Find Odin's surname | `/superhero//search=odin%20`
