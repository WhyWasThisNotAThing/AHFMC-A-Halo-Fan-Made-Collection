drop trigger  if exists limit_accounts_trg on public.profiles;
drop function if exists public.limit_accounts();
drop table    if exists public.account_limits;

create extension if not exists pg_cron;

select cron.schedule('purge-auth-audit', '17 * * * *',
  $$ delete from auth.audit_log_entries where created_at < now() - interval '1 hour' $$);

select cron.schedule('scrub-session-ips', '19 * * * *',
  $$ update auth.sessions set ip = null, user_agent = null where ip is not null or user_agent is not null $$);

