-- primary table, insert will happen to this table and a trigger will redirect it to the appropriate table
-- child tables will also have checks on them to help with query planning.
CREATE TABLE dblocalizer_resources
(
    culturecode TEXT,
    resourcekey TEXT,
    resourcevalue TEXT,
    resourcepage TEXT
);

-- Get a specific value pased on page, culture and key
CREATE OR REPLACE FUNCTION dblocalizer_get_by_type_and_culture (
    _resource_page text,
    _culture_code text,
    _resource_key text
) RETURNS SETOF dblocalizer_resources
AS $$
begin

  return query select * from dblocalizer_resources
    where resourcepage = _resource_page AND
      culturecode = _culture_code AND
      resourcekey = _resource_key;

end;
$$
LANGUAGE plpgsql;

-- Get all resources for a page based on a specific culture.
CREATE OR REPLACE FUNCTION dblocalizer_resources_by_culture(
    _resource_page text,
    _culture_code text
) RETURNS SETOF dblocalizer_resources
AS $$
  begin

  return query select * from dblocalizer_resources
    where resourcepage = _resource_page AND
      culturecode = _culture_code;

end;
$$
LANGUAGE plpgsql;

-- This is used in the DbFunctions class right now, it can be used to get a list of all resources regardless of culture
-- so that you could make a page to modify the localizations instead of heading to the DB every time.
CREATE OR REPLACE FUNCTION dblocalizer_resources_by_page_all_cultures(
    _resource_page text
) RETURNS SETOF dblocalizer_resources
AS $$
  begin

  return query select * from dblocalizer_resources
    where resourcepage = _resource_page;

end;
$$
LANGUAGE plpgsql;

-- get a list of all resources filterable by page, regardless culture.
-- could be used to create a front end for editing the localizations
create function dblocalizer_get_all_resources (
    _page text = null,
    _limit int = null,
    _offset int = null
) returns setof dblocalizer_resources
as $$
begin

    if _limit = 0 then _limit := null; end if;

  return query select * from dblocalizer_resources where
    (_page is null or resourcepage = _page)
  limit _limit
  offset _offset;

end;
$$
language plpgsql;

-- get a list of unique resource pages
-- could be used to create a front end for editing the localizations
create function dblocalizer_get_all_unique_pages()
returns setof text
as $$
begin

    return query select DISTINCT resourcepage from dblocalizer_resources;

end;
$$
  LANGUAGE plpgsql;

-- This is just to save a new resource to the database, all 4 values are required
-- TODO: add exceptioning to the method
CREATE OR REPLACE FUNCTION dblocalizer_save_resource(
    _resource_page text,
    _culture_code text,
    _resource_key text,
    _resource_value text
) RETURNS bool
AS $$
  begin

  insert into dblocalizer_resources
    (resourcepage, culturecode, resourcekey, resourcevalue)
  VALUES
    (_resource_page, _culture_code, _resource_key, _resource_value);

    return true;

end;
$$
LANGUAGE plpgsql;

-- function to see if we can use the upsert method or if we have to fallback.
create function dblocalizer_supports_upsert ()
returns bool
as $$
declare
  _version int;
begin
  -- upsert support was added in postgres 9.5 anything lower than that should return false;

  execute 'show server_version_num;' into _version;
  return (_version >= 90500);

end;
$$
language plpgsql;

-- insert function for postgres versions that do not support upserting.
create function dblocalizer_legacy_insert (
  _resource_table text,
  _resource_page text,
  _culture_code text,
  _resource_key text,
  _resource_value text
) returns BOOL
as $$
begin

  if exists(select * from dblocalizer_resources where 
    resourcepage = _resource_page and 
    resourcekey = _resource_key and 
    culturecode = _culture_code)
  THEN

    update dblocalizer_resources set
      resourcevalue = _resource_value
    where
      resourcepage = _resource_page and
      resourcekey = _resource_key and
      culturecode = _culture_code;

  ELSE -- this is a new resource lets do a new insert

    execute format('insert into %s (resourcepage, culturecode, resourcekey, resourcevalue) '||
                  'values (%s, %s, %s, %s)', 
                  quote_ident(_resource_table),
                  quote_literal(_resource_page), 
                  quote_literal(_culture_code),
                  quote_literal(_resource_key),
                  quote_literal(_resource_value));

  END IF;

end;
$$
language plpgsql;

-- trigger function to intercept when a resource is added to the main resources table and redirect to a child
-- if all resources are going to be stored in one table then this is not necessary.
CREATE OR REPLACE FUNCTION dblocalizer_resources_insert_trigger()
  RETURNS trigger
AS
$$
declare
  _table_name text = 'dblocalizer_resources_';
  _page_name text;
begin

  _page_name := replace(replace(replace(replace(NEW.resourcepage, '\', '_'), '.Master', ''), '.ascx', ''), '.aspx', '');
  _table_name := _table_name || lower(_page_name);

  -- create a new table for this data if we dont have one yet.
  if not (select exists(select * from information_schema.tables where table_schema = 'public' and table_name = _table_name))
  then

    -- create the table with inheriting from resources and
    execute format('CREATE TABLE %s (check (resourcepage = ''%s'')) inherits (dblocalizer_resources);',
                   quote_ident(_table_name), NEW.resourcepage, quote_ident(_table_name));

    execute format('CREATE UNIQUE INDEX ON %s (resourcepage, culturecode, resourcekey);', quote_ident(_table_name));
    execute format('CREATE INDEX ON %s (culturecode);', quote_ident(_table_name));
    execute format('CREATE INDEX ON %s (resourcekey);', quote_ident(_table_name));
    execute format('CREATE INDEX ON %s (resourcevalue);', quote_ident(_table_name));

  END IF;

  -- upsert the data into the correct table.
  IF localize_supports_upsert()
  THEN
    execute format('insert into %s (resourcepage, culturecode, resourcekey, resourcevalue) '||
                  'values (%s, %s, %s, %s)'||
                  'on conflict (resourcepage, culturecode, resourcekey)'||
                  'do update set resourcevalue = %s', _table_name,
                 quote_literal(new.resourcepage), quote_literal(new.culturecode),
                 quote_literal(new.resourcekey), quote_literal(new.resourcevalue),
                 quote_literal(new.resourcevalue));
  ELSE
    perform localize_legacy_insert(_table_name, NEW.resourcepage, NEW.culturecode, NEW.resourcekey, NEW.resourcevalue);
  END IF;

  return null;

end;
$$
LANGUAGE plpgsql VOLATILE;

-- add the trigger to the resources table, skip this if storing all resources in one table.
CREATE TRIGGER insert_resource_trigger
  BEFORE INSERT
  ON public.dblocalizer_resources
  FOR EACH ROW
  EXECUTE PROCEDURE public.dblocalizer_resources_insert_trigger();
