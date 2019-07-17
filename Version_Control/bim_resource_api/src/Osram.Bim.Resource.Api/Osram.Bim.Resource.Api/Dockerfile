FROM microsoft/dotnet:aspnetcore-runtime AS runtime
WORKDIR /app
COPY publish/ /app/

#TODO: Environment variable should be set externally not within the docker image as the same image
# will be used for all the environments.. 
#ENV ASPNETCORE_ENVIRONMENT="Production"

#EXPOSE 5000/tcp
#ENV ASPNETCORE_URLS http://*:5000

#ENTRYPOINT ["dotnet", "DevOpsSample.Api.dll", "--server.urls", "http://*:5000"]
ENTRYPOINT ["dotnet", "Osram.Bim.Resource.Api.dll"]