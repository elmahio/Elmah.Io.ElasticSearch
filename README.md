# Elmah.Io.ElasticSearch
Elmah.Io.ElasticSearch is a ElasticSearch storage backend for ELMAH.

## How
Elmah.Io.ElasticSearch is configured pretty much like every other storage implementation for Elmah. To get started, add the following to your web.config:

    <connectionStrings>
        <add name="ElmahIoElasticSearch" connectionString="http://localhost:9200"/>
    </connectionStrings>

    <elmah>
        <errorLog type="Elmah.Io.ElasticSearch.ElasticSearchErrorLog, Elmah.Io.ElasticSearch" connectionStringName="ElmahIoElasticSearch" />
    </elmah>

Replace the connection string URL with your ElasticSearch URL and add the elmah config section as explained on the [offical ELMAH site](https://code.google.com/p/elmah/).

That's it, dudes!

If you don't like the default generated index named _elmah_, you should override it by specifying a default index name:

    <errorLog
        type="Elmah.Io.ElasticSearch.ElasticSearchErrorLog, Elmah.Io.ElasticSearch"
        connectionStringName="ElmahIoElasticSearch"
        defaultIndex="myindex" />

## Respect

[![Build by TeamCity](http://www.jetbrains.com/img/banners/Codebetter300x250.png)](http://www.jetbrains.com/teamcity/)