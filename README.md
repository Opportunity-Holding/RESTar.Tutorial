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
For now, let's focus on `requireApiKey`, `allowAllOrigins`, and `configFilePath`. These are used to control external access to the REST API. 

## Role-based authorization using API keys

In most use cases, we want to apply some form of role-based access control to the registered resources. Let's say only some clients should be allowed to insert and delete `SuperHero` entities, while all should be able to read. To implement this, we create an XML file that will work as the configuration that RESTar reads API keys and access rights from. It can look like this:



