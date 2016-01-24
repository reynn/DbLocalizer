-- primary table, insert will happen to this table and a trigger will redirect it to the appropriate table
-- child tables will also have checks on them to help with query planning.
CREATE TABLE resources
(
    culturecode TEXT,
    resourcekey TEXT,
    resourcevalue TEXT,
    resourcepage TEXT
);

-- partitioned tables, these will hold the actual data, can have as many or none of these if desired.
CREATE TABLE resources_about (check (resourcepage = 'About.aspx')) inherits (resources);
CREATE TABLE resources_default (check (resourcepage = 'Default.aspx')) inherits (resources);

-- 
CREATE UNIQUE INDEX ON resources_about (resourcepage, culturecode, resourcekey);
CREATE INDEX ON resources_about (culturecode);
CREATE INDEX ON resources_about (resourcekey);
CREATE INDEX ON resources_about (resourcevalue);

CREATE UNIQUE INDEX ON resources_default (resourcepage, culturecode, resourcekey);
CREATE INDEX ON resources_default (culturecode);
CREATE INDEX ON resources_default (resourcekey);
CREATE INDEX ON resources_default (resourcevalue);

-- Get a specific value pased on page, culture and key
CREATE OR REPLACE FUNCTION localize_get_by_type_and_culture (
    _resource_page text, 
    _culture_code text, 
    _resource_key text
) RETURNS SETOF resources
AS $$
begin

  return query select * from resources
    where resourcepage = _resource_page AND
      culturecode = _culture_code AND
      resourcekey = _resource_key;

end;
$$
LANGUAGE plpgsql;

-- Get all resources for a page based on a specific culture.
CREATE OR REPLACE FUNCTION localize_resources_by_culture(
    _resource_page text, 
    _culture_code text
) RETURNS SETOF resources
AS $$
  begin

  return query select * from resources
    where resourcepage = _resource_page AND
      culturecode = _culture_code;

end;
$$
LANGUAGE plpgsql;

-- This is used in the DbFunctions class right now, it can be used to get a list of all resources regardless of culture
-- so that you could make a page to modify the localizations instead of heading to the DB every time.
CREATE OR REPLACE FUNCTION localize_resources_by_page_all_cultures(
    _resource_page text
) RETURNS SETOF resources
AS $$
  begin

  return query select * from resources 
    where resourcepage = _resource_page;

end;
$$
LANGUAGE plpgsql;


-- This is just to save a new resource to the database, all 4 values are required
-- TODO: add exceptioning to the method
CREATE OR REPLACE FUNCTION localize_save_resource(
    _resource_page text, 
    _culture_code text, 
    _resource_key text, 
    _resource_value text
) RETURNS bool
AS $$
  begin

  insert into resources
    (resourcepage, culturecode, resourcekey, resourcevalue)
  VALUES
    (_resource_page, _culture_code, _resource_key, _resource_value);
    
    return true;

end;
$$
LANGUAGE plpgsql;

-- trigger function to intercept when a resource is added to the main resources table and redirect to a child
-- if all resources are going to be stored in one table then this is not necessary, will look into a way to make
-- this more dynamic if possible.
CREATE OR REPLACE FUNCTION public.resources_insert_trigger()
  RETURNS trigger
AS
$BODY$
  begin

  if NEW.resourcepage ilike 'about.aspx'
  THEN

    insert into resources_about values (NEW.*)
    on conflict (resourcepage, culturecode, resourcekey) 
    do update set resourcevalue = NEW.resourcevalue;

  elseif NEW.resourcepage ilike 'default.aspx'
  THEN

    insert into resources_default values (NEW.*)
    on conflict (resourcepage, culturecode, resourcekey) 
    do update set resourcevalue = NEW.resourcevalue;

  END IF;

  return null;

end;
$BODY$
LANGUAGE plpgsql VOLATILE;

-- add the trigger to the resources table, skip this if storing all resources in one table.
CREATE TRIGGER insert_resource_trigger
  BEFORE INSERT
  ON public.resources
  FOR EACH ROW
  EXECUTE PROCEDURE public.resources_insert_trigger();