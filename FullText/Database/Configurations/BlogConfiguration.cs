using FullText.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NpgsqlTypes;

namespace FullText.Database.Configurations;

internal sealed class BlogConfiguration : IEntityTypeConfiguration<Blog>
{
    public void Configure(EntityTypeBuilder<Blog> builder)
    {
        builder.ToTable("blogs");
        builder.Property(b => b.Id).ValueGeneratedNever();

        builder.Property<NpgsqlTsVector>("SearchVector").HasComputedColumnSql(
            """
            setweight(to_tsvector('english', coalesce("title", '')), 'A') ||
            setweight(to_tsvector('english', coalesce("content", '')), 'B') ||
            setweight(to_tsvector('english', coalesce("excerpt", '')), 'C')
            """,
            stored: true);

        builder.HasIndex("SearchVector").HasMethod("GIN");

        builder.HasData(
        [
            // ---- DDD (4 posts, decreasing relevance) ----
            new Blog
            {
                Id = 1,
                Title = "Domain-Driven Design: Modeling Complex Business Logic",
                Excerpt = "An introduction to DDD building blocks — entities, value objects, and aggregates.",
                Content = """
                    Domain-Driven Design (DDD) is a methodology for tackling complexity in software by
                    focusing on the core domain and domain logic. DDD encourages a rich domain model
                    built from entities, value objects, and aggregates that enforce invariants. Every
                    serious DDD implementation revolves around an aggregate root that controls access
                    to the objects within its consistency boundary.

                    A central concept in Domain-Driven Design is the ubiquitous language — a shared
                    vocabulary between developers and domain experts. DDD also introduces bounded
                    contexts, letting large domain models split into smaller, independently evolvable
                    pieces, each with its own domain language and its own DDD-driven design decisions.
                    """,
                DuoDate = new DateTime(2024, 1, 10, 0, 0, 0, kind: DateTimeKind.Utc)
            },
            new Blog
            {
                Id = 2,
                Title = "Bounded Contexts and the Ubiquitous Language",
                Excerpt = "Why splitting your domain model matters in Domain-Driven Design.",
                Content = """
                    Bounded contexts are one of the most practically useful ideas from Domain-Driven
                    Design. A bounded context defines an explicit boundary within which a particular
                    domain model applies, with its own ubiquitous language shared between developers
                    and domain experts.

                    Applying DDD without bounded contexts often leads to a single bloated model trying
                    to represent every concern of the business at once. Splitting by bounded context
                    keeps each domain model focused, and lets teams evolve their piece of the system
                    independently under DDD principles.
                    """,
                DuoDate = new DateTime(2024, 1, 18, 0, 0, 0, kind: DateTimeKind.Utc)
            },
            new Blog
            {
                Id = 3,
                Title = "Aggregates and Domain Events Explained",
                Excerpt = "How aggregates enforce consistency and emit meaningful domain events.",
                Content = """
                    An aggregate is a cluster of associated objects treated as a single unit for data
                    changes, with a designated aggregate root as the only entry point. This pattern,
                    borrowed from DDD, ensures invariants are enforced consistently rather than scattered
                    across the codebase.

                    Domain events allow an aggregate to signal that something meaningful happened,
                    without coupling directly to whoever reacts to it. This keeps the domain model
                    expressive while staying decoupled from infrastructure concerns.
                    """,
                DuoDate = new DateTime(2024, 1, 25, 0, 0, 0, kind: DateTimeKind.Utc)
            },
            new Blog
            {
                Id = 4,
                Title = "Clean Architecture and Layered .NET Systems",
                Excerpt = "Structuring .NET applications around dependency inversion and clear layers.",
                Content = """
                    Clean Architecture organizes an application into concentric layers, with core
                    business rules at the center and infrastructure concerns pushed to the outer
                    edges. Dependencies always point inward, so the domain layer never depends on
                    databases, frameworks, or UI technology.

                    Many teams pair Clean Architecture with ideas from DDD to shape the innermost
                    layer, though Clean Architecture itself is a broader structural pattern that
                    applies even to simpler, non-domain-heavy applications.
                    """,
                DuoDate = new DateTime(2024, 2, 1, 0, 0, 0, kind: DateTimeKind.Utc)
            },

            // ---- Modular Monolith (4 posts) ----
            new Blog
            {
                Id = 5,
                Title = "Modular Monolith: The Middle Ground Between Monolith and Microservices",
                Excerpt = "How to structure a monolith into independent modules without microservices overhead.",
                Content = """
                    A modular monolith organizes a single deployable application into well-isolated
                    modules, each owning its own data and business logic, communicating only through
                    explicit public contracts. This modular monolith approach gives teams many of the
                    boundary benefits of microservices without the distributed-systems complexity of
                    network calls and deployment orchestration.

                    Each module in a modular monolith can apply its own internal architecture, and
                    modular monolith systems often enforce boundaries through internal visibility
                    modifiers or dedicated architecture tests.
                    """,
                DuoDate = new DateTime(2024, 3, 5, 0, 0, 0, kind: DateTimeKind.Utc)
            },
            new Blog
            {
                Id = 6,
                Title = "Enforcing Module Boundaries in a Modular Monolith",
                Excerpt = "Techniques for keeping modules independent inside a single deployable.",
                Content = """
                    Enforcing module boundaries in a modular monolith usually relies on internal
                    visibility modifiers, architecture tests, or build-time analyzers that fail the
                    build if a module reaches into another module's internals.

                    A well-structured modular monolith treats each module almost like a service,
                    exposing a small public surface area while keeping implementation details private
                    to that module alone.
                    """,
                DuoDate = new DateTime(2024, 3, 12, 0, 0, 0, kind: DateTimeKind.Utc)
            },
            new Blog
            {
                Id = 7,
                Title = "From Monolith to Microservices: When to Make the Jump",
                Excerpt = "Evaluating whether microservices are the right next step for a growing system.",
                Content = """
                    Many teams jump to microservices before their monolith actually needs the split.
                    A modular monolith is often a safer intermediate step, letting a team validate
                    module boundaries before paying the operational cost of distributed deployment.

                    If you find your modules rarely change independently, microservices may add
                    overhead without real benefit — staying a monolith, modular or otherwise, can be
                    the pragmatic choice.
                    """,
                DuoDate = new DateTime(2024, 3, 20, 0, 0, 0, kind: DateTimeKind.Utc)
            },
            new Blog
            {
                Id = 8,
                Title = "Deployment Strategies for .NET Applications",
                Excerpt = "Comparing deployment approaches from single binaries to container orchestration.",
                Content = """
                    .NET applications can be deployed as self-contained executables, framework-dependent
                    binaries, or containers orchestrated by Kubernetes. The right strategy depends on
                    your team's operational maturity and infrastructure.

                    Whether you deploy a modular monolith or a set of independent services, deployment
                    pipeline reliability matters more than the chosen architecture style.
                    """,
                DuoDate = new DateTime(2024, 3, 28, 0, 0, 0, kind: DateTimeKind.Utc)
            },

            // ---- xUnit (4 posts) ----
            new Blog
            {
                Id = 9,
                Title = "Getting Started with xUnit: Writing Your First .NET Tests",
                Excerpt = "A beginner's guide to structuring and running unit tests with xUnit in .NET.",
                Content = """
                    xUnit is one of the most popular testing frameworks for .NET, favored for its
                    extensibility and clean attribute-based syntax. A basic xUnit test class requires no
                    base class — tests are public methods decorated with [Fact], or [Theory] combined
                    with [InlineData] for parameterized cases.

                    xUnit creates a new instance of the test class for every test method, isolating
                    state between tests. Assertions in xUnit are handled through the static Assert
                    class, with methods like Assert.Equal, Assert.Throws, and Assert.Collection.
                    """,
                DuoDate = new DateTime(2024, 4, 2, 0, 0, 0, kind: DateTimeKind.Utc)
            },
            new Blog
            {
                Id = 10,
                Title = "Test-Driven Development with xUnit and Mocking",
                Excerpt = "Practical TDD techniques using xUnit, Moq, and dependency injection.",
                Content = """
                    Test-Driven Development with xUnit follows the red-green-refactor loop: write a
                    failing xUnit test, write the minimal code to pass it, then refactor. xUnit's fast
                    test discovery and parallel execution make this loop quick enough to sustain
                    throughout a working day.

                    Mocking libraries like Moq pair naturally with xUnit's constructor-based fixture
                    pattern, letting you inject fake dependencies into your xUnit test classes without
                    touching real infrastructure.
                    """,
                DuoDate = new DateTime(2024, 4, 10, 0, 0, 0, kind: DateTimeKind.Utc)
            },
            new Blog
            {
                Id = 11,
                Title = "Integration Testing in .NET with Test Containers",
                Excerpt = "Spinning up real dependencies in tests using Testcontainers.",
                Content = """
                    Integration tests validate that your application works correctly against real
                    infrastructure — a real database, a real message broker — rather than mocks.
                    Testcontainers makes this practical by spinning up disposable Docker containers
                    for the duration of a test run.

                    These integration tests are commonly written using xUnit as the runner, combined
                    with collection fixtures to share an expensive container across multiple test
                    classes rather than starting one per test.
                    """,
                DuoDate = new DateTime(2024, 4, 18, 0, 0, 0, kind: DateTimeKind.Utc)
            },
            new Blog
            {
                Id = 12,
                Title = "Writing Maintainable Test Suites",
                Excerpt = "General principles for keeping a growing test suite fast and reliable.",
                Content = """
                    As a test suite grows, maintainability becomes as important as coverage. Favor
                    clear naming, avoid shared mutable state between tests, and keep individual tests
                    focused on a single behavior rather than asserting on many unrelated outcomes.

                    Whatever framework you use — xUnit, NUnit, or MSTest — the same principles of test
                    isolation and clarity apply equally well.
                    """,
                DuoDate = new DateTime(2024, 4, 25, 0, 0, 0, kind: DateTimeKind.Utc)
            },

            // ---- CQRS (4 posts) ----
            new Blog
            {
                Id = 13,
                Title = "CQRS: Separating Reads from Writes",
                Excerpt = "Why splitting command and query responsibilities can simplify complex systems.",
                Content = """
                    Command Query Responsibility Segregation (CQRS) is an architectural pattern that
                    separates the write model (commands) from the read model (queries). Under CQRS,
                    commands mutate state and return no data, while queries return data and never
                    mutate state.

                    Teams adopting CQRS often introduce a mediator to dispatch commands and queries to
                    their respective handlers, keeping the API layer thin. CQRS pairs naturally with
                    event sourcing, though the two patterns are independent.
                    """,
                DuoDate = new DateTime(2024, 5, 2, 0, 0, 0, kind: DateTimeKind.Utc)
            },
            new Blog
            {
                Id = 14,
                Title = "Combining DDD and CQRS in an Event-Sourced System",
                Excerpt = "A practical walkthrough of applying Domain-Driven Design and CQRS together.",
                Content = """
                    When Domain-Driven Design and CQRS are combined, the domain model becomes the
                    source of truth for command validation, while a separate read model serves queries.
                    Aggregates emit domain events as the output of every CQRS command, which an event
                    store persists as the authoritative log.

                    CQRS command handlers load an aggregate by replaying its events, apply the requested
                    behavior, and append new events back to the store — query handlers subscribe to the
                    same stream to build read-optimized projections.
                    """,
                DuoDate = new DateTime(2024, 5, 10, 0, 0, 0, kind: DateTimeKind.Utc)
            },
            new Blog
            {
                Id = 15,
                Title = "Building Read Models for Reporting Dashboards",
                Excerpt = "Designing denormalized read models optimized for fast queries.",
                Content = """
                    Reporting dashboards typically need data shaped very differently from your
                    transactional write model. Building a dedicated, denormalized read model — an
                    approach closely related to CQRS — lets queries stay fast without complicating the
                    write side with reporting concerns.

                    This read model can be refreshed on a schedule or updated incrementally as events
                    arrive, depending on how fresh the reporting data needs to be.
                    """,
                DuoDate = new DateTime(2024, 5, 18, 0, 0, 0, kind: DateTimeKind.Utc)
            },
            new Blog
            {
                Id = 16,
                Title = "API Design Patterns for Scalable Systems",
                Excerpt = "Common patterns for building APIs that scale gracefully under load.",
                Content = """
                    Scalable API design often involves pagination, caching, rate limiting, and clear
                    versioning strategies. Some teams also split their API surface following CQRS-like
                    principles, exposing distinct endpoints for commands versus queries even without a
                    full CQRS implementation underneath.

                    Regardless of the pattern chosen, consistent error handling and predictable
                    response shapes matter more to API consumers than the internal architecture.
                    """,
                DuoDate = new DateTime(2024, 5, 26, 0, 0, 0, kind: DateTimeKind.Utc)
            },

            // ---- Kubernetes (4 posts, decreasing relevance) ----
            new Blog
            {
                Id = 17,
                Title = "Kubernetes Fundamentals: Pods, Deployments, and Services",
                Excerpt = "A tour of core Kubernetes objects and how they fit together.",
                Content = """
                    Kubernetes is a container orchestration platform that schedules, scales, and heals
                    workloads across a cluster of machines. The smallest deployable unit in Kubernetes
                    is the Pod, typically wrapped by a Kubernetes Deployment that manages rolling
                    updates and replica counts.

                    A Kubernetes Service provides a stable network identity for a set of Pods, while
                    Kubernetes ConfigMaps and Secrets externalize configuration so container images
                    stay environment-agnostic across every Kubernetes cluster you run.
                    """,
                DuoDate = new DateTime(2024, 6, 3, 0, 0, 0, kind: DateTimeKind.Utc)
            },
            new Blog
            {
                Id = 18,
                Title = "Scaling Applications with Horizontal Pod Autoscaling",
                Excerpt = "How Kubernetes automatically scales workloads based on load.",
                Content = """
                    Horizontal Pod Autoscaling lets Kubernetes add or remove Pod replicas automatically
                    based on observed CPU, memory, or custom metrics. This keeps a Kubernetes workload
                    responsive during traffic spikes without manual intervention.

                    Combined with Cluster Autoscaler, which adds nodes to the underlying Kubernetes
                    cluster when Pods can't be scheduled, teams can scale both application and
                    infrastructure layers together.
                    """,
                DuoDate = new DateTime(2024, 6, 10, 0, 0, 0, kind: DateTimeKind.Utc)
            },
            new Blog
            {
                Id = 19,
                Title = "Container Orchestration Options Beyond Kubernetes",
                Excerpt = "Comparing simpler alternatives for teams that don't need a full cluster.",
                Content = """
                    Kubernetes is the dominant orchestrator, but it isn't the only option. Docker Swarm
                    offers a much simpler operational model for small teams, and managed platform
                    services like AWS ECS or Azure Container Apps abstract away most of what Kubernetes
                    exposes directly.

                    The right choice depends on team size and operational appetite — Kubernetes gives
                    the most flexibility but demands the most expertise to run well in production.
                    """,
                DuoDate = new DateTime(2024, 6, 17, 0, 0, 0, kind: DateTimeKind.Utc)
            },
            new Blog
            {
                Id = 20,
                Title = "CI/CD Pipelines for .NET Applications",
                Excerpt = "Building automated build, test, and release pipelines for .NET projects.",
                Content = """
                    A solid CI/CD pipeline builds, tests, and packages a .NET application on every
                    commit, then promotes the artifact through staging and production environments
                    with minimal manual steps.

                    The final deployment target might be a set of Kubernetes manifests, a container
                    registry push, or a simple app-service deployment — the pipeline discipline matters
                    more than which target it eventually deploys to.
                    """,
                DuoDate = new DateTime(2024, 6, 24, 0, 0, 0, kind: DateTimeKind.Utc)
            },

            // ---- GraphQL (4 posts, decreasing relevance) ----
            new Blog
            {
                Id = 21,
                Title = "GraphQL vs REST: Designing Flexible APIs",
                Excerpt = "Why GraphQL lets clients request exactly the data they need.",
                Content = """
                    GraphQL is a query language for APIs that lets clients specify exactly which fields
                    they need, avoiding the over-fetching and under-fetching common with fixed REST
                    endpoints. A single GraphQL endpoint typically replaces dozens of REST routes.

                    Every GraphQL API is described by a strongly typed schema, and a GraphQL resolver
                    is responsible for fetching the data behind each field, whether that means calling
                    a database, a cache, or another GraphQL service entirely.
                    """,
                DuoDate = new DateTime(2024, 7, 1, 0, 0, 0, kind: DateTimeKind.Utc)
            },
            new Blog
            {
                Id = 22,
                Title = "Implementing GraphQL Subscriptions in .NET with Hot Chocolate",
                Excerpt = "Adding real-time updates to a .NET GraphQL API.",
                Content = """
                    Hot Chocolate is a popular GraphQL server for .NET that supports queries, mutations,
                    and subscriptions out of the box. GraphQL subscriptions push updates to connected
                    clients over WebSockets whenever the underlying data changes.

                    Setting up a GraphQL subscription usually means wiring an in-memory or Redis-backed
                    pub/sub provider so multiple server instances can broadcast the same event to every
                    subscribed client.
                    """,
                DuoDate = new DateTime(2024, 7, 8, 0, 0, 0, kind: DateTimeKind.Utc)
            },
            new Blog
            {
                Id = 23,
                Title = "API Gateway Patterns for Microservices",
                Excerpt = "Centralizing cross-cutting concerns in front of a microservices architecture.",
                Content = """
                    An API gateway sits in front of a set of microservices, handling authentication,
                    rate limiting, and request routing in one place so individual services don't
                    duplicate that logic. Some gateways expose a GraphQL layer that stitches together
                    responses from several backend services into a single query.

                    Choosing between a REST-style gateway and a GraphQL-based one usually comes down to
                    how varied your client applications' data needs are.
                    """,
                DuoDate = new DateTime(2024, 7, 15, 0, 0, 0, kind: DateTimeKind.Utc)
            },
            new Blog
            {
                Id = 24,
                Title = "Rate Limiting Strategies for Public APIs",
                Excerpt = "Protecting backend services from abuse and traffic spikes.",
                Content = """
                    Public APIs need rate limiting to protect backend resources from abusive or
                    accidental overuse. Common approaches include fixed windows, sliding windows, and
                    token buckets, each with different trade-offs around burst tolerance.

                    These strategies apply whether the API surface is REST, GraphQL, or something else
                    entirely — the limiter sits in front of the request pipeline regardless of the
                    underlying query style.
                    """,
                DuoDate = new DateTime(2024, 7, 22, 0, 0, 0, kind: DateTimeKind.Utc)
            },

            // ---- Event-Driven Architecture (4 posts, decreasing relevance) ----
            new Blog
            {
                Id = 25,
                Title = "Event-Driven Architecture with Message Brokers",
                Excerpt = "Decoupling services by communicating through events instead of direct calls.",
                Content = """
                    Event-driven architecture decouples services by having them publish events to a
                    broker rather than calling each other directly. Producers in an event-driven system
                    don't need to know which consumers, if any, will react to an event.

                    A message broker like RabbitMQ or Kafka sits at the center of most event-driven
                    architectures, durably storing and routing events so consumers can process them
                    independently, at their own pace, without coupling to producer availability.
                    """,
                DuoDate = new DateTime(2024, 7, 29, 0, 0, 0, kind: DateTimeKind.Utc)
            },
            new Blog
            {
                Id = 26,
                Title = "Implementing the Outbox Pattern for Reliable Messaging",
                Excerpt = "Guaranteeing events are published even when a service crashes mid-transaction.",
                Content = """
                    The outbox pattern solves a classic event-driven problem: how to atomically update
                    your database and publish an event without a distributed transaction. A service
                    writes the event to an outbox table in the same local transaction as its data
                    change, then a separate relay process publishes it to the broker.

                    This keeps event-driven systems consistent even if the process crashes between the
                    database write and the network call to the broker.
                    """,
                DuoDate = new DateTime(2024, 8, 5, 0, 0, 0, kind: DateTimeKind.Utc)
            },
            new Blog
            {
                Id = 27,
                Title = "Choosing Between RabbitMQ and Kafka",
                Excerpt = "Comparing two popular message brokers for different workload shapes.",
                Content = """
                    RabbitMQ is a traditional message broker built around queues and flexible routing,
                    while Kafka is a distributed log optimized for high-throughput streaming and replay.
                    Both can anchor an event-driven system, but they suit different access patterns.

                    Kafka's ability to replay a full event history makes it attractive for analytics
                    and event sourcing, while RabbitMQ's routing flexibility often fits simpler
                    task-queue style workloads better.
                    """,
                DuoDate = new DateTime(2024, 8, 12, 0, 0, 0, kind: DateTimeKind.Utc)
            },
            new Blog
            {
                Id = 28,
                Title = "Designing Idempotent APIs",
                Excerpt = "Making sure repeated requests don't cause duplicate side effects.",
                Content = """
                    An idempotent API endpoint produces the same result no matter how many times an
                    identical request is retried, which matters enormously whenever clients or
                    networks might duplicate a call. Idempotency keys let a server recognize and
                    safely ignore a repeated request.

                    This matters just as much for HTTP APIs as it does for event-driven consumers,
                    since message brokers commonly redeliver events at least once.
                    """,
                DuoDate = new DateTime(2024, 8, 19, 0, 0, 0, kind: DateTimeKind.Utc)
            },

            // ---- Caching Strategies (4 posts, decreasing relevance) ----
            new Blog
            {
                Id = 29,
                Title = "Caching Strategies with Redis in .NET",
                Excerpt = "Speeding up .NET applications with an in-memory cache layer.",
                Content = """
                    Redis is an in-memory data store commonly used as a caching layer in front of a
                    slower primary database. Caching frequently read, rarely changed data in Redis can
                    turn expensive queries into sub-millisecond lookups.

                    Common Redis caching strategies include cache-aside, where the application checks
                    the cache before falling back to the database, and write-through caching, where
                    every write updates the cache and the database together.
                    """,
                DuoDate = new DateTime(2024, 8, 26, 0, 0, 0, kind: DateTimeKind.Utc)
            },
            new Blog
            {
                Id = 30,
                Title = "Cache Invalidation Patterns: The Two Hard Problems",
                Excerpt = "Keeping cached data correct as the underlying data changes.",
                Content = """
                    Cache invalidation is famously one of the two hardest problems in computer science.
                    A cache that never expires eventually serves stale data, while one that expires too
                    aggressively loses most of its performance benefit.

                    Time-based expiry, explicit invalidation on write, and event-driven cache busting
                    are the three most common approaches, each trading off simplicity against
                    freshness guarantees.
                    """,
                DuoDate = new DateTime(2024, 9, 2, 0, 0, 0, kind: DateTimeKind.Utc)
            },
            new Blog
            {
                Id = 31,
                Title = "Improving API Latency with Distributed Caching",
                Excerpt = "A latency-focused look at where caching fits in a request path.",
                Content = """
                    API latency budgets are usually dominated by database round trips and downstream
                    service calls, not application logic itself. Introducing a distributed cache in
                    front of the slowest calls is often the single highest-leverage latency fix
                    available to a team.

                    Beyond caching, connection pooling, response compression, and reducing serialization
                    overhead all contribute meaningfully to a faster API.
                    """,
                DuoDate = new DateTime(2024, 9, 9, 0, 0, 0, kind: DateTimeKind.Utc)
            },
            new Blog
            {
                Id = 32,
                Title = "Database Indexing Best Practices for PostgreSQL",
                Excerpt = "Choosing the right indexes to keep queries fast as tables grow.",
                Content = """
                    PostgreSQL indexes speed up lookups at the cost of extra storage and slower writes,
                    so indexing every column is rarely the right call. B-tree indexes cover most
                    equality and range queries, while GIN indexes suit full-text search and array
                    containment lookups.

                    For read-heavy tables, a well-chosen index often beats adding a caching layer
                    entirely, since it fixes the underlying query cost rather than papering over it.
                    """,
                DuoDate = new DateTime(2024, 9, 16, 0, 0, 0, kind: DateTimeKind.Utc)
            }
        ]);
    }
}