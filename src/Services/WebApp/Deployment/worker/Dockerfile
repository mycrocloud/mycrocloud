FROM golang:1.23.2-alpine AS builder

RUN apk add --no-cache git

WORKDIR /app

COPY go.mod go.sum ./
RUN go mod download

COPY . .

RUN CGO_ENABLED=0 GOOS=linux go build -ldflags="-s -w" -o worker .

FROM alpine:3.20

RUN adduser -D -g '' appuser

WORKDIR /app

COPY --from=builder /app/worker .

USER appuser

ENTRYPOINT ["./worker"]
