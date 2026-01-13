using FluentMigrator;
using FluentMigrator.Expressions;
using FluentMigrator.Infrastructure;

namespace Monoling0.NotificationService.Persistence.Migrations;

#pragma warning disable SA1649 // File name must match first type name

[Migration(1768127583, "InitialMigration")]
public class InitialMigration : IMigration
{
    public void GetUpExpressions(IMigrationContext context)
    {
        context.Expressions.Add(new ExecuteSqlStatementExpression
        {
            SqlStatement = """
                           create schema if not exists notification;
                           
                           create table if not exists notification.inbox_events
                           (
                               event_id        varchar(64) primary key,
                               topic           varchar(255) not null,
                               partition       int not null,
                               offset          bigint not null,
                               received_at     timestamptz not null,
                               processed_at    timestamptz null,
                               status          smallint not null,
                               attempt_count   int not null default 0,
                               last_attempt_at timestamptz null,
                               last_error      text null
                           );
                           
                           create index if not exists ix_inbox_events_topic_partition_offset
                               on notification.inbox_events(topic, partition, offset);
                           
                           create table if not exists notification.email_outbox
                           (
                               outbox_id        bigserial primary key,
                               kind             varchar(128) not null,
                               recipient_user_id bigint null,
                               to_email         varchar(320) not null,
                               subject          varchar(512) not null,
                               body             text not null,
                               status           smallint not null,
                               attempt_count    int not null default 0,
                               created_at       timestamptz not null,
                               next_attempt_at  timestamptz not null,
                               last_attempt_at  timestamptz null,
                               sent_at          timestamptz null,
                               last_error       text null
                           );
                           
                           create index if not exists ix_email_outbox_status_next_attempt
                               on notification.email_outbox(status, next_attempt_at);
                           
                           create table if not exists notification.pending_email_requests
                           (
                               correlation_id varchar(128) primary key,
                               user_id        bigint not null,
                               purpose        varchar(128) not null,
                               payload_json   jsonb not null,
                               status         smallint not null,
                               created_at     timestamptz not null,
                               expires_at     timestamptz not null,
                               completed_at   timestamptz null,
                               resolved_email varchar(320) null,
                               error          text null
                           );
                           
                           create index if not exists ix_pending_email_requests_status_expires
                               on notification.pending_email_requests(status, expires_at);
                           
                           create table if not exists notification.users_email_cache
                           (
                               user_id    bigint primary key,
                               email      varchar(320) not null,
                               updated_at timestamptz not null
                           );
                           
                           create table if not exists notification.followers_cache
                           (
                               followee_id bigint not null,
                               follower_id bigint not null,
                               updated_at  timestamptz not null,
                               primary key (followee_id, follower_id)
                           );
                           
                           create index if not exists ix_followers_cache_followee
                               on notification.followers_cache(followee_id);
                           
                           create table if not exists notification.courses_cache
                           (
                               course_id    bigint primary key,
                               title        varchar(512) not null,
                               description  text null,
                               cefr_level   varchar(16) null,
                               language     varchar(64) null,
                               published_at timestamptz null,
                               updated_at   timestamptz not null
                           );
                           """,
        });
    }

    public void GetDownExpressions(IMigrationContext context)
    {
        context.Expressions.Add(new ExecuteSqlStatementExpression
        {
            SqlStatement = """
                           drop table if exists notification.courses_cache;
                           drop table if exists notification.followers_cache;
                           drop table if exists notification.users_email_cache;
                           drop table if exists notification.pending_email_requests;
                           drop table if exists notification.email_outbox;
                           drop table if exists notification.inbox_events;
                           
                           drop schema if exists notification;
                           """,
        });
    }

    public string ConnectionString => throw new NotSupportedException();
}

#pragma warning restore SA1649 // File name must match first type name
