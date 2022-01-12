# Webstep

# Installation guide

1. Download zip and unpack solution to folder
2. Open cmd and cd to ```cd <path-to-containing-folder>\Webstep\TaskApi```
3. Run command: ```dotnet build```
4. After build run command ```dotnet run --urls=https://localhost:5001;http://localhost:8080```
5. Now that the Api for the application is up and running we will run the web application
6. Open a new cmd and cd to ```cd <path-to-containing-folder>\Webstep\TaskApplication```
7. Run command: ```dotnet build```
8. After build run command ```dotnet run --urls=https://localhost:5021;http://localhost:5020```
9. Now the application is running! Go to https://localhost:5021 to start using the application
