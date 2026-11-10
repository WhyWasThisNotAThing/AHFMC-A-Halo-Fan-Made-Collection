

create table if not exists public.reports (
  id                bigint generated always as identity primary key,
  reporter_id       uuid not null default auth.uid() references auth.users(id) on delete cascade,
  reported_id       uuid not null,
  reported_username text not null,
  reason            text not null check (char_length(reason) between 1 and 500),
  evidence          text check (char_length(evidence) <= 6000),
  status            text not null default 'open',
  created_at        timestamptz not null default now()
);
alter table public.reports enable row level security;

drop policy if exists "users file reports" on public.reports;
create policy "users file reports" on public.reports
  for insert to authenticated with check (reporter_id = auth.uid());

create or replace function public.limit_reports() returns trigger
language plpgsql security definer set search_path = public as $$
begin
  if (select count(*) from public.reports
        where reporter_id = new.reporter_id and created_at > now() - interval '1 hour') >= 5 then
    raise exception 'You have sent too many reports. Try again later.' using errcode = 'P0001';
  end if;
  return new;
end $$;
drop trigger if exists limit_reports_trg on public.reports;
create trigger limit_reports_trg before insert on public.reports
  for each row execute function public.limit_reports();



alter table public.messages         drop constraint if exists messages_body_len;
alter table public.messages         add  constraint messages_body_len check (char_length(body) between 1 and 500) not valid;

alter table public.dms              drop constraint if exists dms_body_len;
alter table public.dms              add  constraint dms_body_len check (char_length(body) between 1 and 4000) not valid;

alter table public.party_messages   drop constraint if exists party_messages_len;
alter table public.party_messages   add  constraint party_messages_len check (char_length(body) between 1 and 4000 and char_length(sig) <= 300) not valid;

alter table public.key_backups      drop constraint if exists key_backups_len;
alter table public.key_backups      add  constraint key_backups_len check (char_length(blob) <= 4000) not valid;

alter table public.profiles         drop constraint if exists profiles_format;
alter table public.profiles         add  constraint profiles_format check (
    username ~ '^[A-Za-z0-9_]{3,16}$' and char_length(ek) <= 300 and char_length(sk) <= 300) not valid;

alter table public.parties          drop constraint if exists parties_name_format;
alter table public.parties          add  constraint parties_name_format check (name ~ '^[A-Za-z0-9_-]{1,24}$') not valid;

create unique index if not exists profiles_username_lower_uniq on public.profiles (lower(username));

create index if not exists messages_rl       on public.messages       (sender_id, created_at);
create index if not exists dms_rl            on public.dms            (sender_id, created_at);
create index if not exists party_messages_rl on public.party_messages (sender_id, created_at);

create or replace function public.rate_limit_sender() returns trigger
language plpgsql as $$
declare
  cnt int;
  lim int  := tg_argv[0]::int;
  secs int := tg_argv[1]::int;
begin
  execute format(
    'select count(*) from %I.%I where sender_id = $1 and created_at > now() - make_interval(secs => $2)',
    tg_table_schema, tg_table_name)
  into cnt using new.sender_id, secs;
  if cnt >= lim then
    raise exception 'Slow down: too many messages' using errcode = 'P0001';
  end if;
  return new;
end $$;

drop trigger if exists rl_messages       on public.messages;
drop trigger if exists rl_dms            on public.dms;
drop trigger if exists rl_party_messages on public.party_messages;
create trigger rl_messages       before insert on public.messages       for each row execute function public.rate_limit_sender(10, 10);
create trigger rl_dms            before insert on public.dms            for each row execute function public.rate_limit_sender(20, 10);
create trigger rl_party_messages before insert on public.party_messages for each row execute function public.rate_limit_sender(20, 10);



create table if not exists public.lfg_posts (
  id         bigint generated always as identity primary key,
  user_id    uuid not null default auth.uid() references auth.users(id) on delete cascade,
  username   text not null default '',
  game       text not null check (game ~ '^[a-z0-9_-]{1,24}$'),
  activity   text not null default '' check (char_length(activity) <= 30),
  note       text not null default '' check (char_length(note) <= 120),
  needed     smallint not null default 1 check (needed between 1 and 15),
  created_at timestamptz not null default now(),
  expires_at timestamptz not null default now() + interval '2 hours',
  unique (user_id, game)
);
create index if not exists lfg_posts_game_idx on public.lfg_posts (game, created_at desc);
alter table public.lfg_posts enable row level security;

drop policy if exists "signed in users see live posts" on public.lfg_posts;
create policy "signed in users see live posts" on public.lfg_posts
  for select to authenticated using (expires_at > now());

drop policy if exists "users post for themselves" on public.lfg_posts;
create policy "users post for themselves" on public.lfg_posts
  for insert to authenticated with check (user_id = auth.uid());

drop policy if exists "users remove own posts" on public.lfg_posts;
create policy "users remove own posts" on public.lfg_posts
  for delete to authenticated using (user_id = auth.uid());

create or replace function public.lfg_before_insert() returns trigger
language plpgsql security definer set search_path = public as $$
begin
  select username into new.username from public.profiles where user_id = new.user_id;
  if new.username is null then
    raise exception 'Create your chat account first.' using errcode = 'P0001';
  end if;

  new.created_at := now();
  new.expires_at := least(greatest(new.expires_at, now() + interval '5 minutes'), now() + interval '6 hours');

  delete from public.lfg_posts where expires_at < now() - interval '1 day';

  if (select count(*) from public.lfg_posts
        where user_id = new.user_id and expires_at > now()) >= 5 then
    raise exception 'You can have at most 5 live posts. Remove one first.' using errcode = 'P0001';
  end if;
  return new;
end $$;
drop trigger if exists lfg_before_insert_trg on public.lfg_posts;
create trigger lfg_before_insert_trg before insert on public.lfg_posts
  for each row execute function public.lfg_before_insert();



alter table public.lfg_posts drop constraint if exists lfg_posts_needed_check;
alter table public.lfg_posts add constraint lfg_posts_needed_check check (needed between 1 and 128);

create or replace function public.lfg_before_insert() returns trigger
language plpgsql security definer set search_path = public as $$
begin
  select username into new.username from public.profiles where user_id = new.user_id;
  if new.username is null then
    raise exception 'Create your chat account first.' using errcode = 'P0001';
  end if;

  new.created_at := now();
  new.expires_at := least(greatest(new.expires_at, now() + interval '5 minutes'), now() + interval '24 hours');

  delete from public.lfg_posts where expires_at < now() - interval '1 day';

  if (select count(*) from public.lfg_posts
        where user_id = new.user_id and expires_at > now()) >= 5 then
    raise exception 'You can have at most 5 live posts. Remove one first.' using errcode = 'P0001';
  end if;
  return new;
end $$;



drop policy if exists "dm delete own"    on public.dms;
drop policy if exists "lobby delete own" on public.messages;
drop policy if exists "pm delete own"    on public.party_messages;
create policy "dm delete own"    on public.dms            for delete to authenticated using (sender_id = auth.uid());
create policy "lobby delete own" on public.messages       for delete to authenticated using (sender_id = auth.uid());
create policy "pm delete own"    on public.party_messages for delete to authenticated using (sender_id = auth.uid());

create or replace function public.delete_my_account() returns void
language plpgsql security definer set search_path = public, auth as $$
declare me uuid := auth.uid();
begin
  if me is null then
    raise exception 'Not signed in.' using errcode = 'P0001';
  end if;

  delete from public.party_messages where party_id in (select id from public.parties where owner_id = me);
  delete from public.party_keys     where party_id in (select id from public.parties where owner_id = me);
  delete from public.party_invites  where party_id in (select id from public.parties where owner_id = me);
  delete from public.party_members  where party_id in (select id from public.parties where owner_id = me);
  delete from public.parties        where owner_id = me;

  delete from public.party_messages where sender_id = me;
  delete from public.party_keys     where user_id = me;
  delete from public.party_invites  where invitee_id = me;
  delete from public.party_members  where user_id = me;
  delete from public.dms            where sender_id = me or recipient_id = me;
  delete from public.messages       where sender_id = me;
  delete from public.key_backups    where user_id = me;
  if to_regclass('public.reports') is not null then
    execute 'delete from public.reports where reporter_id = $1' using me;
  end if;
  delete from public.profiles       where user_id = me;

  delete from auth.users where id = me;
end $$;

revoke all on function public.delete_my_account() from public, anon;
grant execute on function public.delete_my_account() to authenticated;



create extension if not exists fuzzystrmatch with schema extensions;

create table if not exists public.admins (user_id uuid primary key references auth.users(id) on delete cascade);
alter table public.admins enable row level security;

create or replace function public.is_admin() returns boolean
language sql security definer stable set search_path = public as $$
  select exists (select 1 from public.admins where user_id = auth.uid());
$$;
revoke all on function public.is_admin() from public, anon;
grant execute on function public.is_admin() to authenticated;

create table if not exists public.moderation (
  user_id    uuid primary key references auth.users(id) on delete cascade,
  status     text not null check (status in ('hold','banned','clear')),
  reason     text not null default '' check (char_length(reason) <= 500),
  updated_at timestamptz not null default now()
);
alter table public.moderation enable row level security;

drop policy if exists "see own moderation" on public.moderation;
create policy "see own moderation" on public.moderation
  for select to authenticated using (user_id = auth.uid() or public.is_admin());

alter table public.reports add column if not exists reporter_username text not null default '';

drop policy if exists "admin reads reports" on public.reports;
create policy "admin reads reports" on public.reports
  for select to authenticated using (public.is_admin());

update public.reports r set status = 'duplicate'
 where r.status = 'open'
   and exists (select 1 from public.reports r2
                where r2.reporter_id = r.reporter_id and r2.reported_id = r.reported_id
                  and r2.status = 'open' and r2.id > r.id);
create unique index if not exists reports_one_open_per_pair
  on public.reports (reporter_id, reported_id) where status = 'open';

create or replace function public.report_before_insert() returns trigger
language plpgsql security definer set search_path = public as $$
begin
  select coalesce(username, '') into new.reporter_username from public.profiles where user_id = new.reporter_id;
  if new.reported_id = new.reporter_id then
    raise exception 'You can''t report yourself.' using errcode = 'P0001';
  end if;
  return new;
end $$;
drop trigger if exists report_before_insert_trg on public.reports;
create trigger report_before_insert_trg before insert on public.reports
  for each row execute function public.report_before_insert();

create or replace function public.report_hold_after() returns trigger
language plpgsql security definer set search_path = public as $$
declare
  auto_hold constant boolean := true;
begin
  if auto_hold
     and not exists (select 1 from public.reports r
                      where r.reporter_id = new.reporter_id
                        and r.id < new.id
                        and r.created_at > now() - interval '12 hours')
     and not exists (select 1 from public.admins where user_id = new.reported_id)
     and not exists (select 1 from public.moderation where user_id = new.reported_id and status = 'banned') then
    insert into public.moderation (user_id, status, reason)
      values (new.reported_id, 'hold', 'Report #' || new.id)
      on conflict (user_id) do update set status = 'hold', reason = excluded.reason, updated_at = now();
  end if;
  return null;
end $$;
drop trigger if exists report_hold_after_trg on public.reports;
create trigger report_hold_after_trg after insert on public.reports
  for each row execute function public.report_hold_after();

create or replace function public.mod_set_status(target uuid, new_status text, why text default '') returns void
language plpgsql security definer set search_path = public as $$
begin
  if not public.is_admin() then
    raise exception 'Not allowed.' using errcode = 'P0001';
  end if;
  if new_status not in ('hold','banned','clear') then
    raise exception 'Bad status.' using errcode = 'P0001';
  end if;
  if exists (select 1 from public.admins where user_id = target) then
    raise exception 'You can''t hold or ban an admin.' using errcode = 'P0001';
  end if;

  insert into public.moderation (user_id, status, reason)
    values (target, new_status, left(coalesce(why, ''), 500))
    on conflict (user_id) do update
      set status = excluded.status, reason = excluded.reason, updated_at = now();

  update public.reports
     set status = case new_status when 'banned' then 'actioned' when 'clear' then 'dismissed' else status end
   where reported_id = target and status = 'open' and new_status in ('banned','clear');
end $$;
revoke all on function public.mod_set_status(uuid, text, text) from public, anon;
grant execute on function public.mod_set_status(uuid, text, text) to authenticated;

create table if not exists public.user_devices (
  user_id     uuid not null references auth.users(id) on delete cascade,
  device_hash text not null check (char_length(device_hash) = 64),
  last_seen   timestamptz not null default now(),
  primary key (user_id, device_hash)
);
alter table public.user_devices enable row level security;
create index if not exists user_devices_hash on public.user_devices (device_hash);

create or replace function public.shares_restricted_device(uid uuid) returns boolean
language sql security definer stable set search_path = public as $$
  select exists (
    select 1
    from public.user_devices me
    join public.user_devices other on other.device_hash = me.device_hash and other.user_id <> me.user_id
    join public.moderation m on m.user_id = other.user_id and m.status in ('hold','banned')
    where me.user_id = uid
  );
$$;

create or replace function public.block_restricted_sender() returns trigger
language plpgsql security definer set search_path = public as $$
declare st text; uid uuid := coalesce(to_jsonb(new)->>'sender_id', to_jsonb(new)->>'user_id')::uuid;
begin
  if exists (select 1 from public.admins where user_id = uid) then return new; end if;

  select status into st from public.moderation where user_id = uid;
  if st = 'banned' then
    raise exception 'You are banned. You can only talk to the admin in the appeal chat.' using errcode = 'P0001';
  elsif st = 'hold' then
    raise exception 'Your chats are on hold while a report is looked at. You can read but not send.' using errcode = 'P0001';
  end if;

  if public.shares_restricted_device(uid) then
    raise exception 'This device is linked to an account that is on hold or banned.' using errcode = 'P0001';
  end if;
  return new;
end $$;

drop trigger if exists block_restricted_messages       on public.messages;
drop trigger if exists block_restricted_dms            on public.dms;
drop trigger if exists block_restricted_party_messages on public.party_messages;
create trigger block_restricted_messages       before insert on public.messages       for each row execute function public.block_restricted_sender();
create trigger block_restricted_dms            before insert on public.dms            for each row execute function public.block_restricted_sender();
create trigger block_restricted_party_messages before insert on public.party_messages for each row execute function public.block_restricted_sender();

do $$ begin
  if to_regclass('public.lfg_posts') is not null then
    drop trigger if exists block_restricted_lfg on public.lfg_posts;
    create trigger block_restricted_lfg before insert on public.lfg_posts
      for each row execute function public.block_restricted_sender();
  end if;
end $$;

create or replace function public.register_device(h text) returns void
language plpgsql security definer set search_path = public as $$
begin
  if auth.uid() is null then raise exception 'Not signed in.' using errcode = 'P0001'; end if;
  if h is null or char_length(h) <> 64 then raise exception 'Bad device id.' using errcode = 'P0001'; end if;

  insert into public.user_devices (user_id, device_hash) values (auth.uid(), h)
    on conflict (user_id, device_hash) do update set last_seen = now();

  if not exists (select 1 from public.admins where user_id = auth.uid())
     and public.shares_restricted_device(auth.uid()) then
    raise exception 'This device is linked to an account that is on hold or banned, so other accounts can''t be used on it.' using errcode = 'P0001';
  end if;
end $$;
revoke all on function public.register_device(text) from public, anon;
grant execute on function public.register_device(text) to authenticated;

alter table public.profiles add column if not exists device_hash text;

create or replace function public.profiles_device_check() returns trigger
language plpgsql security definer set search_path = public as $$
begin
  if new.device_hash is null or char_length(new.device_hash) <> 64 then
    raise exception 'Update the app to create an account.' using errcode = 'P0001';
  end if;
  if exists (
    select 1 from public.user_devices d
    join public.moderation m on m.user_id = d.user_id and m.status in ('hold','banned')
    where d.device_hash = new.device_hash and d.user_id <> new.user_id
  ) then
    raise exception 'New accounts can''t be created on this device right now.' using errcode = 'P0001';
  end if;
  insert into public.user_devices (user_id, device_hash) values (new.user_id, new.device_hash)
    on conflict (user_id, device_hash) do nothing;
  return new;
end $$;
drop trigger if exists profiles_device_check_trg on public.profiles;
create trigger profiles_device_check_trg before insert on public.profiles
  for each row execute function public.profiles_device_check();

create table if not exists public.appeal_messages (
  id         bigint generated always as identity primary key,
  user_id    uuid not null references auth.users(id) on delete cascade,
  sender_id  uuid not null default auth.uid() references auth.users(id) on delete cascade,
  body       text not null check (char_length(body) between 1 and 1000),
  created_at timestamptz not null default now()
);
create index if not exists appeal_messages_user on public.appeal_messages (user_id, id);
alter table public.appeal_messages enable row level security;

drop policy if exists "appeal read" on public.appeal_messages;
create policy "appeal read" on public.appeal_messages
  for select to authenticated using (user_id = auth.uid() or public.is_admin());

drop policy if exists "appeal write" on public.appeal_messages;
create policy "appeal write" on public.appeal_messages
  for insert to authenticated with check (
    sender_id = auth.uid() and (
      public.is_admin()
      or (user_id = auth.uid()
          and exists (select 1 from public.moderation where user_id = auth.uid() and status = 'banned'))
    )
  );

drop trigger if exists rl_appeal_messages on public.appeal_messages;
create trigger rl_appeal_messages before insert on public.appeal_messages
  for each row execute function public.rate_limit_sender(10, 60);

create or replace function public.username_too_close(candidate text) returns boolean
language sql security definer stable set search_path = public, extensions as $$
  select exists (
    select 1 from public.profiles
    where extensions.levenshtein(lower(username), lower(candidate)) < 3
  );
$$;
revoke all on function public.username_too_close(text) from public;
grant execute on function public.username_too_close(text) to anon, authenticated;

create or replace function public.profiles_name_distance() returns trigger
language plpgsql security definer set search_path = public, extensions as $$
declare clash text;
begin
  select username into clash from public.profiles
   where user_id <> new.user_id
     and extensions.levenshtein(lower(username), lower(new.username)) < 3
   limit 1;
  if clash is not null then
    raise exception 'That name is too close to the existing name "%". Names must differ by at least 3 characters.', clash
      using errcode = 'P0001';
  end if;
  return new;
end $$;
drop trigger if exists profiles_name_distance_trg on public.profiles;
create trigger profiles_name_distance_trg before insert on public.profiles
  for each row execute function public.profiles_name_distance();

create or replace function public.delete_my_account() returns void
language plpgsql security definer set search_path = public, auth as $$
declare me uuid := auth.uid();
begin
  if me is null then
    raise exception 'Not signed in.' using errcode = 'P0001';
  end if;
  if exists (select 1 from public.moderation where user_id = me and status in ('hold','banned'))
     or exists (select 1 from public.reports where reported_id = me and status = 'open') then
    raise exception 'You can''t delete your account while a report about you is being looked at, or while it is on hold or banned.' using errcode = 'P0001';
  end if;

  delete from public.party_messages where party_id in (select id from public.parties where owner_id = me);
  delete from public.party_keys     where party_id in (select id from public.parties where owner_id = me);
  delete from public.party_invites  where party_id in (select id from public.parties where owner_id = me);
  delete from public.party_members  where party_id in (select id from public.parties where owner_id = me);
  delete from public.parties        where owner_id = me;

  delete from public.party_messages where sender_id = me;
  delete from public.party_keys     where user_id = me;
  delete from public.party_invites  where invitee_id = me;
  delete from public.party_members  where user_id = me;
  delete from public.dms            where sender_id = me or recipient_id = me;
  delete from public.messages       where sender_id = me;
  delete from public.key_backups    where user_id = me;
  if to_regclass('public.reports') is not null then
    execute 'delete from public.reports where reporter_id = $1' using me;
  end if;
  delete from public.profiles       where user_id = me;

  delete from auth.users where id = me;
end $$;
revoke all on function public.delete_my_account() from public, anon;
grant execute on function public.delete_my_account() to authenticated;

do $$ begin
  begin alter publication supabase_realtime add table public.appeal_messages; exception when duplicate_object then null; end;
  begin alter publication supabase_realtime add table public.reports;         exception when duplicate_object then null; end;
end $$;



create or replace function public.report_hold_after() returns trigger
language plpgsql security definer set search_path = public as $$
declare
  auto_hold constant boolean := true;
begin
  if auto_hold
     and not exists (select 1 from public.reports r
                      where r.reporter_id = new.reporter_id
                        and r.id < new.id
                        and r.created_at > now() - interval '12 hours')
     and not exists (select 1 from public.admins where user_id = new.reported_id)
     and not exists (select 1 from public.moderation where user_id = new.reported_id and status = 'banned') then
    insert into public.moderation (user_id, status, reason)
      values (new.reported_id, 'hold', 'Report #' || new.id)
      on conflict (user_id) do update set status = 'hold', reason = excluded.reason, updated_at = now();
  end if;
  return null;
end $$;

create or replace function public.delete_my_account() returns void
language plpgsql security definer set search_path = public, auth as $$
declare me uuid := auth.uid();
begin
  if me is null then
    raise exception 'Not signed in.' using errcode = 'P0001';
  end if;
  if exists (select 1 from public.moderation where user_id = me and status in ('hold','banned'))
     or exists (select 1 from public.reports where reported_id = me and status = 'open') then
    raise exception 'You can''t delete your account while a report about you is being looked at, or while it is on hold or banned.' using errcode = 'P0001';
  end if;

  delete from public.party_messages where party_id in (select id from public.parties where owner_id = me);
  delete from public.party_keys     where party_id in (select id from public.parties where owner_id = me);
  delete from public.party_invites  where party_id in (select id from public.parties where owner_id = me);
  delete from public.party_members  where party_id in (select id from public.parties where owner_id = me);
  delete from public.parties        where owner_id = me;

  delete from public.party_messages where sender_id = me;
  delete from public.party_keys     where user_id = me;
  delete from public.party_invites  where invitee_id = me;
  delete from public.party_members  where user_id = me;
  delete from public.dms            where sender_id = me or recipient_id = me;
  delete from public.messages       where sender_id = me;
  delete from public.key_backups    where user_id = me;
  if to_regclass('public.reports') is not null then
    execute 'delete from public.reports where reporter_id = $1' using me;
  end if;
  delete from public.profiles       where user_id = me;

  delete from auth.users where id = me;
end $$;
revoke all on function public.delete_my_account() from public, anon;
grant execute on function public.delete_my_account() to authenticated;



create or replace function public.limit_parties() returns trigger
language plpgsql security definer set search_path = public as $$
begin
  if (select count(*) from public.parties where owner_id = new.owner_id) >= 20 then
    raise exception 'You can own at most 20 parties.' using errcode = 'P0001';
  end if;
  return new;
end $$;
drop trigger if exists limit_parties_trg on public.parties;
create trigger limit_parties_trg before insert on public.parties
  for each row execute function public.limit_parties();

create or replace function public.limit_members() returns trigger
language plpgsql security definer set search_path = public as $$
begin
  if (select count(*) from public.party_members where party_id = new.party_id) >= 50 then
    raise exception 'This party is full (50 members).' using errcode = 'P0001';
  end if;
  return new;
end $$;
drop trigger if exists limit_members_trg on public.party_members;
create trigger limit_members_trg before insert on public.party_members
  for each row execute function public.limit_members();

create or replace function public.limit_invites() returns trigger
language plpgsql security definer set search_path = public as $$
begin
  if (select count(*) from public.party_invites where party_id = new.party_id) >= 50 then
    raise exception 'Too many pending invites for this party.' using errcode = 'P0001';
  end if;
  if (select count(*) from public.party_invites where invitee_id = new.invitee_id) >= 20 then
    raise exception 'That user has too many pending invites.' using errcode = 'P0001';
  end if;
  return new;
end $$;
drop trigger if exists limit_invites_trg on public.party_invites;
create trigger limit_invites_trg before insert on public.party_invites
  for each row execute function public.limit_invites();



-- Party finder: every post gets its own party, and other players join with one click.
-- The poster's app adds each joiner (the party chat is end-to-end encrypted, so only a
-- member can hand out the key) and lowers "needed" by one.

alter table public.lfg_posts add column if not exists party_id uuid;
alter table public.lfg_posts add column if not exists joined   smallint not null default 0;

alter table public.lfg_posts drop constraint if exists lfg_posts_needed_check;
alter table public.lfg_posts add constraint lfg_posts_needed_check check (needed between 0 and 128);

drop policy if exists "users update own posts" on public.lfg_posts;
create policy "users update own posts" on public.lfg_posts
  for update to authenticated using (user_id = auth.uid()) with check (user_id = auth.uid());

create or replace function public.lfg_before_update() returns trigger
language plpgsql security definer set search_path = public as $$
begin
  if (to_jsonb(new) - 'needed' - 'joined' - 'note' - 'activity' - 'expires_at')
       is distinct from (to_jsonb(old) - 'needed' - 'joined' - 'note' - 'activity' - 'expires_at') then
    raise exception 'Only the player counts, note, activity and time can be changed.' using errcode = 'P0001';
  end if;
  new.needed     := least(greatest(new.needed, 0), 128);
  new.expires_at := least(greatest(new.expires_at, now() + interval '5 minutes'), now() + interval '24 hours');
  return new;
end $$;
drop trigger if exists lfg_before_update_trg on public.lfg_posts;
create trigger lfg_before_update_trg before update on public.lfg_posts
  for each row execute function public.lfg_before_update();

create table if not exists public.lfg_joins (
  post_id    bigint not null references public.lfg_posts(id) on delete cascade,
  user_id    uuid   not null default auth.uid() references auth.users(id) on delete cascade,
  username   text   not null default '',
  party_id   uuid,
  status     text   not null default 'pending' check (status in ('pending', 'accepted', 'full')),
  created_at timestamptz not null default now(),
  primary key (post_id, user_id)
);
alter table public.lfg_joins enable row level security;

drop policy if exists "see own or incoming joins" on public.lfg_joins;
create policy "see own or incoming joins" on public.lfg_joins
  for select to authenticated using (
    user_id = auth.uid()
    or exists (select 1 from public.lfg_posts p where p.id = lfg_joins.post_id and p.user_id = auth.uid()));

drop policy if exists "users ask to join" on public.lfg_joins;
create policy "users ask to join" on public.lfg_joins
  for insert to authenticated with check (user_id = auth.uid());

drop policy if exists "poster answers joins" on public.lfg_joins;
create policy "poster answers joins" on public.lfg_joins
  for update to authenticated
  using      (exists (select 1 from public.lfg_posts p where p.id = lfg_joins.post_id and p.user_id = auth.uid()))
  with check (exists (select 1 from public.lfg_posts p where p.id = lfg_joins.post_id and p.user_id = auth.uid()));

drop policy if exists "remove own or incoming joins" on public.lfg_joins;
create policy "remove own or incoming joins" on public.lfg_joins
  for delete to authenticated using (
    user_id = auth.uid()
    or exists (select 1 from public.lfg_posts p where p.id = lfg_joins.post_id and p.user_id = auth.uid()));

create or replace function public.lfg_joins_before_insert() returns trigger
language plpgsql security definer set search_path = public as $$
declare p public.lfg_posts;
begin
  select * into p from public.lfg_posts where id = new.post_id;
  if p.id is null or p.expires_at <= now() then
    raise exception 'That post is no longer live.' using errcode = 'P0001';
  end if;
  if p.party_id is null then
    raise exception 'That post has no party to join. Message the player instead.' using errcode = 'P0001';
  end if;
  if p.user_id = new.user_id then
    raise exception 'That is your own post.' using errcode = 'P0001';
  end if;
  if p.needed <= 0 then
    raise exception 'That party is already full.' using errcode = 'P0001';
  end if;

  select username into new.username from public.profiles where user_id = new.user_id;
  if new.username is null then
    raise exception 'Create your chat account first.' using errcode = 'P0001';
  end if;

  if (select count(*) from public.lfg_joins
        where user_id = new.user_id and created_at > now() - interval '1 hour') >= 10 then
    raise exception 'You have asked to join too many parties. Try again later.' using errcode = 'P0001';
  end if;

  new.party_id   := p.party_id;
  new.status     := 'pending';
  new.created_at := now();
  return new;
end $$;
drop trigger if exists lfg_joins_before_insert_trg on public.lfg_joins;
create trigger lfg_joins_before_insert_trg before insert on public.lfg_joins
  for each row execute function public.lfg_joins_before_insert();

create or replace function public.lfg_joins_before_update() returns trigger
language plpgsql security definer set search_path = public as $$
begin
  if (to_jsonb(new) - 'status') is distinct from (to_jsonb(old) - 'status') then
    raise exception 'Only the status can be changed.' using errcode = 'P0001';
  end if;
  return new;
end $$;
drop trigger if exists lfg_joins_before_update_trg on public.lfg_joins;
create trigger lfg_joins_before_update_trg before update on public.lfg_joins
  for each row execute function public.lfg_joins_before_update();

drop trigger if exists block_restricted_lfg_joins on public.lfg_joins;
create trigger block_restricted_lfg_joins before insert on public.lfg_joins
  for each row execute function public.block_restricted_sender();



drop trigger  if exists limit_accounts_trg on public.profiles;
drop function if exists public.limit_accounts();
drop table    if exists public.account_limits;

create extension if not exists pg_cron;

select cron.schedule('purge-auth-audit', '17 * * * *',
  $$ delete from auth.audit_log_entries where created_at < now() - interval '1 hour' $$);

select cron.schedule('scrub-session-ips', '19 * * * *',
  $$ update auth.sessions set ip = null, user_agent = null where ip is not null or user_agent is not null $$);

