# Blog Reader (static Vanilla JS site)

A tiny static site (HTML/CSS/JS, no build step) that lists and searches blogs
via the two API endpoints from your `FullText` project (`/blogs` and
`/blogs/search`). Served by nginx and packaged as a Docker image so it can be
added to .NET Aspire as a container resource.

## Files

- `index.html` / `styles.css` / `app.js` — the static site.
- `default.conf.template` — nginx config; proxies `/api/*` to your backend
  API (`BACKEND_URL`), and serves the static files for everything else.
- `Dockerfile` — builds an `nginx:alpine`-based image containing the site.

The front end calls `/api/blogs` and `/api/blogs/search` (same-origin), and
nginx forwards those to `BACKEND_URL` — so no CORS setup is needed and the
backend URL doesn't need to be baked into the JS at build time.

## Run it standalone

```bash
docker build -t blog-reader .
docker run --rm -p 8080:8080 -e BACKEND_URL=http://host.docker.internal:5000 blog-reader
```

Then open http://localhost:8080.

## Add it to your Aspire AppHost

In `AppHost.cs` (or `Program.cs` of your AppHost project), reference the
folder containing this Dockerfile as a container resource and point it at
your API project's endpoint:

```csharp
var api = builder.AddProject<Projects.FullText_Api>("blogs-api");

builder.AddDockerfile("blog-reader", "../blog-reader")
    .WithHttpEndpoint(port: 8080, targetPort: 8080)
    .WithEnvironment("BACKEND_URL", api.GetEndpoint("http"))
    .WithReference(api);
```

Notes:
- `AddDockerfile(name, contextPath)` comes from the
  `Aspire.Hosting.Docker` building blocks that ship with the Aspire SDK —
  point `contextPath` at this project's folder (wherever you place it
  relative to the AppHost project).
- `api.GetEndpoint("http")` resolves to the running API's actual URL at
  launch time, which Aspire injects into the container as `BACKEND_URL`,
  matching the `${BACKEND_URL}` placeholder in `default.conf.template`.
- If your API project already defines its endpoint name differently (e.g.
  `"https"`), adjust `GetEndpoint(...)` to match.
