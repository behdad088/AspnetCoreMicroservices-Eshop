# Microservices Architecture with Clean Architecture, CQRS, and Docker

## Build Status (GitHub Actions)
| Image | Status | Image | Status |
| ------------- | ------------- | ------------- | ------------- |
| Basket API | [![Basket API](https://github.com/behdad088/AspnetCoreMicroservices-Eshop/actions/workflows/basket-api.yml/badge.svg)](https://github.com/behdad088/AspnetCoreMicroservices-Eshop/actions?query=workflow:basket-api)| Web MVC CI | [![Web MVC CI](https://github.com/behdad088/AspnetCoreMicroservices-Eshop/actions/workflows/webmvc.yml/badge.svg)](https://github.com/behdad088/AspnetCoreMicroservices-Eshop/actions/workflows/webmvc.yml)
| Discount API | [![Discount API](https://github.com/behdad088/AspnetCoreMicroservices-Eshop/actions/workflows/discount-api.yml/badge.svg)](https://github.com/behdad088/AspnetCoreMicroservices-Eshop/actions?query=workflow:discount-api)|Web Status |[![Web Status CI](https://github.com/behdad088/AspnetCoreMicroservices-Eshop/actions/workflows/webstatus.yml/badge.svg)](https://github.com/behdad088/AspnetCoreMicroservices-Eshop/actions/workflows/webstatus.yml) |
|Catalog API | [![Catalog API](https://github.com/behdad088/AspnetCoreMicroservices-Eshop/actions/workflows/catalog-api.yml/badge.svg)](https://github.com/behdad088/AspnetCoreMicroservices-Eshop/actions?query=workflow:catalog-api)| Web Shopping CI | [![Web Shopping API CI](https://github.com/behdad088/AspnetCoreMicroservices-Eshop/actions/workflows/web-shopping-api.yml/badge.svg)](https://github.com/behdad088/AspnetCoreMicroservices-Eshop/actions/workflows/web-shopping-api.yml)
Ordering API| [![Ordering API CI](https://github.com/behdad088/AspnetCoreMicroservices-Eshop/actions/workflows/ordering-api.yml/badge.svg)](https://github.com/behdad088/AspnetCoreMicroservices-Eshop/actions/workflows/ordering-api.yml) | CodeQL Secutiry Scan | [![CodeQL Secutiry Scan](https://github.com/behdad088/AspnetCoreMicroservices-Eshop/actions/workflows/codeql.yml/badge.svg)](https://github.com/behdad088/AspnetCoreMicroservices-Eshop/actions/workflows/codeql.yml)


## Overview
Welcome to our microservices-based project with a robust architecture that ensures scalability, maintainability, and efficient communication between services. This project embraces Clean Architecture, Domain-Driven Design (DDD), Command Query Responsibility Segregation (CQRS), MediatR, and Docker for containerization and deployment.
![Architecture overview](https://github.com/behdad088/AspnetCoreMicroservices-Eshop/assets/9384578/fa81f770-1778-4477-934b-51fbf0fb6b08)

## Microservices Architecture
Our project is designed as a collection of microservices that operate independently from each other, each with its dedicated database:

**Catalog Service**: Utilizes MongoDB for data storage.<br>
**Basket Service**: Uses Redis for data storage.<br>
**Discount Service**: Relies on Postgres for data storage.<br>
**Order Service**: Uses SQL Server for data storage.<br> <br>
These microservices communicate with each other through both HTTP REST API and gRPC protocols. REST API plays a vital role in facilitating gateway communication, while gRPC enables seamless communication between inner services.

## Health Monitoring
To monitor the health of each microservice, we have implemented two or more health checks:

**Liveness**: Indicates whether the application is live.<br>
**hc**: Checks if all underlying dependencies required by the application are up and running.<br><br>
Each service has its health checks. For instance, in the Order service, we have checks for the SQL Server service and RabbitMQ (RMQ).
![healthcheck](https://github.com/behdad088/AspnetCoreMicroservices-Eshop/assets/9384578/486eb3c1-df21-4601-97bb-bd71337c7207)

## WebStatus Application
To provide an overview of the health status of all registered services, we've developed a web application called WebStatus. This application allows us to monitor the health of the microservices and their dependencies in a centralized manner.

## API Gateway
To ensure the security of our microservices and prevent direct external exposure, we have implemented an API Gateway project. This project uses YARP reverse proxy to simplify the routing of requests to the appropriate microservices. YARP settings are configured in the appsettings.json file. YARP enables us to avoid duplicating requests in the gateway when they are already available in the microservices' REST API by utilizing a reverse proxy. In cases where the required endpoint does not exist in the microservices, the gateway communicates with multiple services via gRPC, aggregates the results, and populates them into the API.

## Asynchronous Communication
We have established asynchronous communication between the Basket and Order microservices. When an order is registered in the Basket service, a message containing the necessary information is sent to the RabbitMQ (RMQ). The Order service has a consumer that listens to the order queue, reads the message from the queue, and processes the order accordingly.

## Containerization and Deployment
For containerization and deployment, we have created a Docker file for each microservice. This allows us to create Docker images and run the services within Docker. The services and their dependencies are managed through the docker-compose.yaml file.

## Continuous Integration (CI)
To streamline the CI process, we have set up CI workflows for each service. These workflows are triggered to build Docker images and push them to the Docker registry upon pull requests and merges to the master branch. Docker tags are used for versioning the images. Pull requests add the branch name as a Docker tag to test the branch independently if needed. Upon merging the branch to the master, two additional tags are created: "build id" and "latest." The "latest" tag represents the most recent image build but may not be the most stable, as it can be overridden by other builds. The "build id" tag serves as the preferable tag for release, ensuring stability and traceability for our releases.

## Clean Architecture Approach
In our Order service, we follow the Clean Architecture design approach, which helps us keep our code organized, maintainable, and easy to understand. The architecture suggests dividing the application into different layers, each with its responsibility:
![order-servicepng](https://github.com/behdad088/AspnetCoreMicroservices-Eshop/assets/9384578/34a68060-6959-4e92-911a-9ed092e2ed98)

Application Layer (Ordering.Application): Contains specific business logic of our order management system. It handles commands and queries, implementing the CQRS pattern using MediatR to separate code for handling write (commands) and read (queries) operations, making our application more efficient and scalable.

Domain Layer (Ordering.Domain): Captures core concepts and behaviors related to orders using Domain-Driven Design (DDD). It models real-world order-related entities, making our code more expressive and closely aligned with actual business requirements.

Infrastructure Layer (Ordering.Infrastructure): Handles all external concerns of our application, such as database access and interactions with external services. Keeping these concerns separate in the infrastructure layer ensures our core business logic remains isolated and clean, and allows us to replace external dependencies with minimal effort if needed.

Presentation Layer (Ordering.API): Serves as the user interface through which people interact with our order management system. It depends only on the Application Layer, decoupling it from the nitty-gritty details of infrastructure concerns, making it easier to maintain and replace without affecting the rest of the application.

By combining Clean Architecture, DDD, CQRS, and MediatR, we achieve a well-organized and maintainable codebase that accurately reflects the real-world domain and its requirements.

## How to Run
To get started with the project, please explore the project repository and its individual microservices. Detailed instructions on how to set up and run the services can be found in their respective folders.
Make sure docker is installed in your system. 
In src folder run the following command

```powershell
docker-compose -f .\docker-compose.yml -f .\docker-compose.override.yml up --build -d
```
After running the above command the containers will be built and run the docker descktop application. 
![containers](https://github.com/behdad088/AspnetCoreMicroservices-Eshop/assets/9384578/3992f159-c038-4a80-a71f-d75f90aeb175)

From the list of the containers you can select any service port (800X) to open the browser. Port 900X is gRPC. After selecting on the port you need to brows /swagger/index.html to open swagger. Blow is an example of Basket.API.

![image](https://github.com/behdad088/AspnetCoreMicroservices-Eshop/assets/9384578/5b36b553-54ca-4608-b5cb-96b851b890eb)

>[!NOTE]
>The project is still ongoing, and we have plans to add identity and logging functionalities. As we progress and introduce new services, the architecture overview will be updated accordingly
