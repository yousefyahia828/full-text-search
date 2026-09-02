# syntax=docker/dockerfile:1

FROM nginx:1.27-alpine

# Static site
COPY index.html /usr/share/nginx/html/index.html
COPY styles.css /usr/share/nginx/html/styles.css
COPY app.js     /usr/share/nginx/html/app.js

# nginx's official entrypoint envsubst's *.template files in this folder
# into /etc/nginx/conf.d/ on container start, so BACKEND_URL can be
# supplied at runtime (e.g. by Aspire) without rebuilding the image.
COPY default.conf.template /etc/nginx/templates/default.conf.template

# Only substitute the variables we actually use in the template.
ENV NGINX_ENVSUBST_OUTPUT_DIR=/etc/nginx/conf.d
ENV NGINX_ENVSUBST_TEMPLATE_DIR=/etc/nginx/templates
ENV NGINX_ENVSUBST_TEMPLATE_SUFFIX=.template

# Default value; override at run time (Aspire will inject the real
# backend endpoint via WithEnvironment).
ENV BACKEND_URL=http://localhost:5000

EXPOSE 8080

HEALTHCHECK --interval=30s --timeout=3s CMD wget -qO- http://localhost:8080/ || exit 1
