FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["framework/JNPF/JNPF.csproj", "framework/JNPF/JNPF/"]
COPY ["src/modularity/oauth/JNPF.OAuth/JNPF.OAuth.csproj", "src/modularity/oauth/JNPF.OAuth/"]
COPY ["src/modularity/system/JNPF.Systems/JNPF.Systems.csproj", "src/modularity/system/JNPF.Systems/"]
COPY ["src/modularity/message/JNPF.Message/JNPF.Message.csproj", "src/modularity/message/JNPF.Message/"]
COPY ["src/modularity/taskscheduler/JNPF.TaskScheduler/JNPF.TaskScheduler.csproj", "src/modularity/taskscheduler/JNPF.TaskScheduler/"]
COPY ["src/modularity/workflow/JNPF.WorkFlow/JNPF.WorkFlow.csproj", "src/modularity/workflow/JNPF.WorkFlow/"]
COPY ["src/modularity/visualdev/JNPF.VisualDev/JNPF.VisualDev.csproj", "src/modularity/visualdev/JNPF.VisualDev/"]
COPY ["src/modularity/codegen/JNPF.CodeGen/JNPF.CodeGen.csproj", "src/modularity/codegen/JNPF.CodeGen/"]
COPY ["src/modularity/visualdata/JNPF.VisualData/JNPF.VisualData.csproj", "src/modularity/visualdata/JNPF.VisualData/"]
COPY ["src/modularity/extend/JNPF.Extend/JNPF.Extend.csproj", "src/modularity/extend/JNPF.Extend/"]
COPY ["src/modularity/app/JNPF.Apps/JNPF.Apps.csproj", "src/modularity/app/JNPF.Apps/"]
COPY ["src/modularity/subdev/JNPF.SubDev/JNPF.SubDev.csproj", "src/modularity/subdev/JNPF.SubDev/"]
COPY ["src/application/JNPF.API.Entry/JNPF.API.Entry.csproj", "src/application/JNPF.API.Entry/"]
COPY . .
WORKDIR "/src/src/application/JNPF.API.Entry"
RUN dotnet build "JNPF.API.Entry.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "JNPF.API.Entry.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

ENTRYPOINT ["dotnet", "JNPF.API.Entry.dll"]