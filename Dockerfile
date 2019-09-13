FROM mcr.microsoft.com/dotnet/core/sdk:2.2-alpine3.9

COPY ./src /app/src
COPY ./tests /app/tests
WORKDIR /app/tests/RedisTribute.TestHarness
RUN dotnet build ./