FROM node:26-alpine AS build
WORKDIR /app

COPY package.json package-lock.json ./
RUN npm ci

COPY . .

ARG APP_VERSION=""
ENV APP_VERSION=${APP_VERSION}
ARG APP_COMMIT=""
ENV APP_COMMIT=${APP_COMMIT}

RUN npm run build

FROM nginx:alpine AS final

COPY nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/dist/ticket-booking-web/browser /usr/share/nginx/html

EXPOSE 80
