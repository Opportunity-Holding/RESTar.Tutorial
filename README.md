# Tutorial
RESTar is a powerful REST API Framework for Starcounter applications, that is free to use and easy to set up in new or existing applications. Using RESTar in your Starcounter projects will give your applications all sorts of REST super powers, with minimal effort. This tutorial will give a basic introduction to RESTar, and how to use it in a simple Starcounter application. For more information, please se the complete [RESTar Specification](https://goo.gl/TIkN7m), which outlines all the features of RESTar.

## Getting started
To get started, install RESTar from NuGet, either by browsing for `RESTar` in the **NuGet Package Manager** or by running the following command in the **Package Manager Console**:

```Install-Package RESTar```

All we need to do then, to enable RESTar in a given application, is to make a call to `RESTar.RESTarConfig.Init()` somewhere in the application code, preferably where it's called once every time the app starts. `Init()` will register the necessary handlers, collect all resources and make them available over a REST API. Here is a simple RESTar application:

```c#
namespace RESTarTutorial
{
    using RESTar;
    public class Program
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
The application above is not very useful, however, since it doesn't really expose any app data. Let's change that. To register a Starcounter database class with RESTar, so that RESTar can expose it for operations over the REST API, we simply decorate it's class definition with the `RESTarAttribute` attribute.

```c#
namespace RESTarTutorial
{
    using RESTar;
    using Starcounter;
    using static RESTar.Methods;

    [Database, RESTar(GET, POST, PUT, PATCH, DELETE)]
    public class SuperHero
    {
        public string RegularName { get; set; }
        public string SuperHeroName { get; set; }
        public string OriginStory { get; set; }
        public string AliasOrName => SuperHeroName ?? RegularName;
        public DateTime InsertedAt { get; }
        public SuperHero() => InsertedAt = DateTime.Now;
    }
}
```
RESTar will find the `SuperHero` class and register it as available over the REST API. This means that REST clients can send `GET`, `POST`, `PUT`, `PATCH` and `DELETE` requests to `<host>:8282/myservice/superhero`. To make a different set of methods available for a resource, we simply include a different set of methods in the `RESTarAttribute` constructor. RESTar has two supported content types, **JSON** and **Excel**, so the bodies contained within this requests can be of either of these formats. Now let's make a couple of simple local `POST` requests to this API with JSON data (using cURL syntax):

```
curl 'localhost:8282/myservice/superhero' -d '{
    "RegularName": "Selina Kyle",
    "SuperHeroName": "Catwoman"
}'
curl 'localhost:8282/myservice/superhero' -d '{
    "RegularName": "Bruce Wayne",
    "SuperHeroName": "Batman"
}' 
```
And now, let's retrieve this data using a `GET` request:

```
curl 'localhost:8282/myservice/superhero'
Output:
[{
    "RegularName": "Selina Kyle",
    "SuperHeroName": "Catwoman"
},{
    "RegularName": "Bruce Wayne",
    "SuperHeroName": "Batman"
}]
```
## Exploring the parameters of `RESTarConfig.Init()`

The `RESTar.RESTarConfig.Init()` method has more parameters than the ones we used above. There is the complete signature:
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

In most use cases, we want to apply some form of role-based access control to the registered resources. Let's say only some clients should be allowed to insert and delete `SuperHero` entities, while all should be able to read. To implement this, we create an XML file that will work as the configuration that RESTar reads API keys and access rights from. Let's create a new XML file in the project directory and call it "Config.xml" (the name and location can be different). Let's make its content look like this:

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
This configuration file specifies two api keys: `a-secure-admin-key` and `a-secure-user-key`. The first can perform all methods on all resources in the `RESTar`, `RESTar.Admin`, `RESTar.Dynamic` and `RESTarTutorial` namespaces, the latter which includes our `SuperHero` resource. The second key, however, can only make `GET` calls to resources in the `RESTarTutorial` namespace. To enforce these access rights, we set the `requireApiKey` parameter to `true` in the call to `RESTarConfig.Init()` and provide the file path to the configuration file in the `configFilePath` parameter. Here is the same program as above, but now with role-based access control:

```c#
namespace RESTarTutorial
{
    using RESTar;
    using Starcounter;
    public class Program
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
In the example above, we saw a Starcounter database class working as a REST resource through RESTar. Starcounter database classes make for good examples, since most Starcounter developers are familiar with them, but RESTar itself is not limited to these classes. Any public non-static class can work as a RESTar resource class – as long as the developer can define the logic that is needed to support operations like `Select`, `Insert` and `Delete` that are used in REST requests. Say, for example, that we want a REST resource that is simply a transient aggregation of database data, that is generated when requested. To go with the example above, let's say we want a `SuperHeroReport` class that we can make `GET` requests to. And since we just set up API keys, we need to include one of the keys in the `Authorization` header. We want to be able to do something like this:
```
curl "localhost:8282/myservice/superheroreport" -H "Authorization: apikey a-secure-user-key"
Output:
[{
    "NumberOfSuperHeroes": 245,
    "FirstSuperHeroInserted": {
        "RegularName": "Selina Kyle",
        "SuperHeroName": "Catwoman"
     },
     "LastSuperHeroInserted": {
        "RegularName": "Matthew Murdock",
        "SuperHeroName": "Daredevil"
    },
    "LongestOriginStoryLength": 4123
}]
```
We can all see the benefit of this resource, right?

Implementing it is simple. Just like we would with a database class, we create a new .NET class, and assign the `RESTarAttribute` attribute to it. This time we only need `GET` to be enabled for the resource. Note that the class below is not a Starcounter database class.

```c#
namespace RESTarTutorial
{
    using RESTar;
    using Starcounter;
    using static RESTar.Methods;
    using System.Linq;
    using System.Collections.Generic;

    [RESTar(GET)]
    public class SuperHeroReport : ISelector<SuperHeroReport>
    {
        public long NumberOfSuperHeroes { get; private set; }
        public SuperHero FirstSuperHeroInserted { get; private set; }
        public SuperHero LastSuperHeroInserted { get; private set; }
        public int LongestOriginStoryLength { get; private set; }

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
                    LongestOriginStoryLength = superHeroesOrdered
                        .Select(h => h.OriginStory.Length)
                        .OrderByDescending(h => h)
                        .FirstOrDefault()
                }
            };
        }
    }
}
```

To define or override the logic that is used when RESTar selects entities of a resource type, we implement the `RESTar.ISelector<T>` interface, and use the resource type as the type parameter `T`. Failure to provide the operations needed for the methods assigned in the `RESTarAttribute` constructor will result in a kind but resolute runtime exception. In the body of this `Select` method above, we provide logic for generating an `IEnumerable<SuperHeroReport>` that is then returned to RESTar when evaluating `GET` requests.
 
## Making some fancy requests
OK, now we've seen the basics of what RESTar can do – and how to make data sources from a Starcounter application available over the REST API in a secure way. Next, let's look at some more advanced examples of how a client can consume a RESTar API. We will use the same application as above, and imagine that the database is now populated with `SuperHero` entities. To try these requests yourself – first clone this repository to your local machine and run the `RESTarTutorial` application.
