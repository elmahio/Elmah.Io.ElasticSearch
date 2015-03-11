# Elmah.Io.ElasticSearch
Elmah.Io.ElasticSearch is a ElasticSearch storage backend for ELMAH.

## How
Elmah.Io.ElasticSearch is configured pretty much like every other storage implementation for Elmah. To get started, add the following to your web.config:

    <connectionStrings>
        <add name="ElmahIoElasticSearch" connectionString="http://localhost:9200/elmah"/>
    </connectionStrings>

    <elmah>
        <errorLog type="Elmah.Io.ElasticSearch.ElasticSearchErrorLog, Elmah.Io.ElasticSearch" connectionStringName="ElmahIoElasticSearch" />
    </elmah>

Replace the connection string URL with your ElasticSearch URL and add the elmah config section as explained on the [offical ELMAH site](https://code.google.com/p/elmah/).

That's it, dudes!

The Elastic index name can be specified one of two ways, let's say you want the index name to be elmahCurrent:

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

## Respect

[![Build by TeamCity](http://www.jetbrains.com/img/banners/Codebetter300x250.png)](http://www.jetbrains.com/teamcity/)
