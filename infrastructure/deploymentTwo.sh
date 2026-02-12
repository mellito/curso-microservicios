#!/bin/bash

cd ..

if [ -d "microservicios" ]; then
    rm -rf microservicios
    echo "Había una carpeta de despliegue anterior, se ha eliminado"
fi

mkdir -p microservicios
cd microservicios

# Crea todos los proyectos de microservicios
projectsList=("GetAdults" "GetChildren" "GetAdultById" "GetChildById" "AddMember" "PickAge" "AddChild" "AddAdult")
for project in "${projectsList[@]}"; do
    dotnet new webapi -n "$project"
    cd $project
    echo "FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build 
WORKDIR /src
COPY $project.csproj .
RUN dotnet restore
COPY . .

RUN dotnet build \"$project.csproj\" -c Release -o /app/build

RUN dotnet publish -c release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY --from=build /app .
ENTRYPOINT [\"dotnet\", \"$project.dll\"]
" > Dockerfile
cd ..
done

