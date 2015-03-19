# Elmah.Io.ElasticSearch
Elmah.Io.ElasticSearch is an Elasticsearch storage backend for ELMAH.

[![install from nuget](http://img.shields.io/nuget/v/Elmah.ElasticSearch.svg?style=flat-square)](https://www.nuget.org/packages/Elmah.ElasticSearch)[![downloads](http://img.shields.io/nuget/dt/Elmah.ElasticSearch.svg?style=flat-square)](https://www.nuget.org/packages/Elmah.ElasticSearch)    

Builds:    
[![teamcity](http://img.shields.io/teamcity/http/teamcity.codebetter.com/e/bt1123.svg?style=flat-square)](http://teamcity.codebetter.com/viewType.html?buildTypeId=bt1123)

#Release 1.1 is live! [Release Notes](https://github.com/elmahio/Elmah.Io.ElasticSearch/releases/tags/1.1)


## How
Elmah.Io.ElasticSearch is configured pretty much like every other storage implementation for Elmah. To get started, add the following to your web.config:

    <connectionStrings>
        <add name="ElmahIoElasticSearch" connectionString="http://localhost:9200/elmah"/>
    </connectionStrings>

    <elmah>
        <errorLog type="Elmah.Io.ElasticSearch.ElasticSearchErrorLog, Elmah.Io.ElasticSearch"
        connectionStringName="ElmahIoElasticSearch" />
    </elmah>

Replace the connection string URL with your Elasticsearch URL and add the elmah config section as explained on the [offical ELMAH site](https://code.google.com/p/elmah/).

##Configuration


The Elasticsearch index name can be specified one of two ways, let's say you want the index name to be elmahCurrent:

1: Put it in the connection string *(preferred)*
```
<connectionStrings>
    <add name="ElmahIoElasticSearch" connectionString="http://localhost:9200/elmahCurrent"/>
</connectionStrings>
```
2: Leave it off the connection string and put it in the elmah specification
```
<connectionStrings>
    <add name="ElmahIoElasticSearch" connectionString="http://localhost:9200"/>
</connectionStrings>

<errorLog
    type="Elmah.Io.ElasticSearch.ElasticSearchErrorLog, Elmah.Io.ElasticSearch"
    connectionStringName="ElmahIoElasticSearch"
    defaultIndex="elmahCurrent" />
```

You can optionally specify the following fields that will be written to search:

1. Application Name
2. Environment Name
3. Customer Name

Specify these in the <errorLog> attribute:
```
  <elmah>
    <errorLog 
        type="Elmah.Io.ElasticSearch.ElasticSearchErrorLog, Elmah.Io.ElasticSearch, Version=1.0.0.0, Culture=neutral" 
        connectionStringName="ElmahElasticSearch" 
        applicationName="ElmahElasticSearchSampleWebsite"
        environmentName="development"
        customerName="sample customer"
        />
    <security allowRemoteAccess="false" />
  </elmah>
```

##Retreiving raw data
To get the data from Elastic, run the following:
```
GET elmah/error/_search
{
  "query": {
    "match_all": {}
  }
}
```

Sample Result:
```
{
   "took": 4,
   "timed_out": false,
   "_shards": {
      "total": 5,
      "successful": 5,
      "failed": 0
   },
   "hits": {
      "total": 1,
      "max_score": 1,
      "hits": [
         {
            "_index": "elmah",
            "_type": "error",
            "_id": "AUwupDKg5dFjuaEj8ID-",
            "_score": 1,
            "_source": {
               "errorXml": "<some xml/>",
               "applicationName": "ElmahElasticSearchSampleWebsite",
               "hostName": "VB-133",
               "type": "System.Web.HttpException",
               "source": "System.Web.Mvc",
               "message": "The controller for path '/asdfdsaf' was not found or does not implement IController.",
               "detail": "System.Web.HttpException (0x80004005): The controller for path '/asdfdsaf' was not found or does not implement IController.",
               "user": "",
               "@timestamp": "2015-03-18T15:47:35.0322548-05:00",
               "statusCode": 404,
               "webHostHtmlMessage": "",
               "environmentName": "development",
               "customerName": "sample customer"
            }
         }
      ]
   }
}
```
## Respect

[![Build by TeamCity](http://www.jetbrains.com/img/banners/Codebetter300x250.png)](http://www.jetbrains.com/teamcity/)
