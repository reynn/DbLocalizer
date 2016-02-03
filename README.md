# DbLocalizer for ASP.NET and PostgreSQL
Resource provider for .net applications that allows localizations from a database, this particular version is geared towards ASP.NET using a PostgreSQL database.

# Requirements
Visual Studio 2013+ (preferrably 2015)
PostgreSQL 9.0+ (preferrably 9.5 for upsert support)
Web Application that uses localizations or a desire to implement it without the need for messy RESX files.

# Installation
if installing from NuGet most things are already configured for you, you will have to go into the web.config and modify the new localizationConnString to point to your database (does not have to be in database as your main application)
using the source you will have to add a connection string called localizationConnString (or modify the code to point to the connection string you want), then add

```
<globalization culture="en-US" uiCulture="en-US" resourceProviderFactoryType="DbLocalizer.DbResourceProviderFactory" />
```

# Usage
easiest option to get your existing localizations from your RESX files is to run a console app that calls

`new DbImporter("--connectionstring--").ImportProject("-- base directory --");`

connectionstring can be left blank if you have it already specified in the app.config file. This will go through all directories looking for resx files, and attempt to import them into the database, that should get you up and running.
