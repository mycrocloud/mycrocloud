FROM golang:1.26-alpine AS builder

ENV CGO_ENABLED=0 \
    GOOS=linux \
    GOARCH=amd64

WORKDIR /app

COPY go.mod go.sum ./
RUN go mod download

COPY . .

RUN go build -o worker .

FROM alpine:3.23

WORKDIR /app

COPY --from=builder /app/worker .

ENTRYPOINT ["./worker"]
